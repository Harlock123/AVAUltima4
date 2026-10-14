using AVAUltima4.Rules;

namespace AVAUltima4.World;

public enum Cell : byte { Wall, Floor, Up, Down, Altar, Fountain, Chest, Trap, Seal, Codex }

/// <summary>
/// A dungeon: its levels, how dangerous it is, which stones rest on its altars, and the arms that
/// can't be bought anywhere, each lying in the first chest of some level.
/// </summary>
public sealed record DungeonDef(string Id, string Name, int Tier, Dictionary<int, Virtue> Stones, string Wall, string Floor, Dictionary<int, string>? Treasure = null);

/// <summary>
/// One level of a dungeon: a maze of passages with a room or two, a ladder up (to the level above,
/// or out), a ladder down (or, in the Undermere, a seal), and things to find. Levels are grown from
/// fixed seeds, and each ladder down lands on the ladder up of the level below.
/// </summary>
public sealed class DungeonLevel
{
    public const int Size = 15;

    private readonly Cell[,] _cells = new Cell[Size, Size];
    public string Dungeon { get; }
    public int Level { get; }
    public (int X, int Y) Up { get; private set; }
    public (int X, int Y) Down { get; private set; } = (-1, -1);
    public (int X, int Y) Altar { get; private set; } = (-1, -1);

    public Cell this[int x, int y] => x < 0 || y < 0 || x >= Size || y >= Size ? Cell.Wall : _cells[x, y];

    /// <summary>The level's chests, in reading order: the first holds the level's treasure, if it has one.</summary>
    public IEnumerable<(int X, int Y)> Chests()
    {
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
            if (_cells[x, y] == Cell.Chest) yield return (x, y);
    }

    public static bool Open(Cell c) => c != Cell.Wall;

    public DungeonLevel(string dungeon, int level, (int X, int Y) up, bool last, bool altar, bool seal, bool codex)
    {
        Dungeon = dungeon;
        Level = level;
        var rng = new Rng((ulong)(dungeon.Aggregate(5381, (h, ch) => h * 33 + ch) * 97 + level * 7919));
        Carve(rng);
        Up = up;
        _cells[up.X, up.Y] = Cell.Up;

        // Rooms: two open squares of three by three, centred on maze cells away from the ladder.
        var rooms = new List<(int X, int Y)>();
        for (int tries = 0; tries < 200 && rooms.Count < 2; tries++)
        {
            int cx = 3 + 2 * rng.Next(5), cy = 3 + 2 * rng.Next(5);
            if (Math.Max(Math.Abs(cx - up.X), Math.Abs(cy - up.Y)) < 3 || rooms.Any(r => Math.Max(Math.Abs(r.X - cx), Math.Abs(r.Y - cy)) < 4)) continue;
            rooms.Add((cx, cy));
            for (int y = cy - 1; y <= cy + 1; y++)
            for (int x = cx - 1; x <= cx + 1; x++)
                _cells[x, y] = Cell.Floor;
        }

        var dist = Distances(up);
        if (!last || seal)
        {
            var far = dist.Where(p => p.Key.X % 2 == 1 && p.Key.Y % 2 == 1 && !rooms.Contains(p.Key)).OrderByDescending(p => p.Value).First().Key;
            Down = far;
            _cells[far.X, far.Y] = seal ? Cell.Seal : Cell.Down;
        }
        // In the Undermere's last level the seal guards the Codex itself: once opened, it is the Codex's chamber.
        if (codex) _cells[Down.X, Down.Y] = Cell.Seal;
        if (altar)
        {
            var room = rooms.Count > 0 ? rooms.OrderByDescending(r => dist.GetValueOrDefault(r)).First() : dist.OrderByDescending(p => p.Value).Skip(1).First().Key;
            Altar = room;
            _cells[room.X, room.Y] = Cell.Altar;
        }
        // A fountain, a few chests and a couple of hidden traps, in the passages.
        var spots = dist.Keys.Where(p => _cells[p.X, p.Y] == Cell.Floor && p != up && dist[p] > 2).OrderBy(_ => rng.Next(1000)).ToList();
        int i = 0;
        if (rng.Chance(70) && i < spots.Count) { var p = spots[i++]; _cells[p.X, p.Y] = Cell.Fountain; }
        for (int c = 0; c < 2 && i < spots.Count; c++) { var p = spots[i++]; _cells[p.X, p.Y] = Cell.Chest; }
        for (int t = 0; t < 2 && i < spots.Count; t++) { var p = spots[i++]; _cells[p.X, p.Y] = Cell.Trap; }
    }

    /// <summary>A maze of passages on the odd squares, with half its dead ends knocked through so there are loops.</summary>
    private void Carve(Rng rng)
    {
        var stack = new Stack<(int X, int Y)>();
        _cells[1, 1] = Cell.Floor;
        stack.Push((1, 1));
        while (stack.Count > 0)
        {
            var (x, y) = stack.Peek();
            var next = Enum.GetValues<Dir>().Select(d => d.Step()).Select(s => (X: x + 2 * s.X, Y: y + 2 * s.Y, MX: x + s.X, MY: y + s.Y))
                .Where(n => n.X > 0 && n.Y > 0 && n.X < Size - 1 && n.Y < Size - 1 && _cells[n.X, n.Y] == Cell.Wall).ToList();
            if (next.Count == 0)
            {
                stack.Pop();
                continue;
            }
            var pick = next[rng.Next(next.Count)];
            _cells[pick.MX, pick.MY] = Cell.Floor;
            _cells[pick.X, pick.Y] = Cell.Floor;
            stack.Push((pick.X, pick.Y));
        }
        for (int y = 1; y < Size - 1; y += 2)
        for (int x = 1; x < Size - 1; x += 2)
        {
            var walls = Enum.GetValues<Dir>().Select(d => d.Step()).Where(s => _cells[x + s.X, y + s.Y] == Cell.Wall
                && x + 2 * s.X > 0 && y + 2 * s.Y > 0 && x + 2 * s.X < Size - 1 && y + 2 * s.Y < Size - 1).ToList();
            if (walls.Count == 3 && rng.Chance(50))
            {
                var s = walls[rng.Next(walls.Count)];
                _cells[x + s.X, y + s.Y] = Cell.Floor;
            }
        }
    }

    /// <summary>Steps from a square to every square you can walk to.</summary>
    public Dictionary<(int X, int Y), int> Distances((int X, int Y) from)
    {
        var dist = new Dictionary<(int, int), int> { [from] = 0 };
        var q = new Queue<(int, int)>();
        q.Enqueue(from);
        while (q.Count > 0)
        {
            var (x, y) = q.Dequeue();
            foreach (var d in Enum.GetValues<Dir>())
            {
                var (dx, dy) = d.Step();
                var n = (x + dx, y + dy);
                if (!Open(this[n.Item1, n.Item2]) || dist.ContainsKey(n)) continue;
                dist[n] = dist[(x, y)] + 1;
                q.Enqueue(n);
            }
        }
        return dist;
    }
}

public static class Dungeons
{
    public const int Levels = 4;

    public static readonly IReadOnlyList<DungeonDef> All =
    [
        new("hollowroot", "Hollowroot", 2, new() { [2] = Virtue.Mercy, [4] = Virtue.Fairness }, "#5a4a3a", "#3a3028"),
        new("mirrormine", "the Mirrored Mine", 3, new() { [2] = Virtue.Candor, [4] = Virtue.Modesty }, "#6a7080", "#34383e", new() { [3] = "runechain" }),
        new("cindervault", "the Cinder Vaults", 3, new() { [2] = Virtue.Daring, [4] = Virtue.Selflessness }, "#7a3a2a", "#3a2420", new() { [4] = "starblade" }),
        new("saltdeep", "Saltdeep", 4, new() { [2] = Virtue.Integrity, [4] = Virtue.Reverence }, "#8a8a7a", "#3a3c38", new() { [3] = "starplate" }),
        new("undermere", "the Undermere", 5, [], "#3a3050", "#1c1828"),
    ];

    /// <summary>Which two stones each seal of the Undermere asks for, level by level.</summary>
    public static (Virtue A, Virtue B) SealStones(int level) => ((Virtue)((level - 1) * 2), (Virtue)((level - 1) * 2 + 1));

    public static DungeonDef Get(string id) => All.First(d => d.Id == id);

    private static readonly Dictionary<(string, int), DungeonLevel> Cache = [];

    public static DungeonLevel Level(string id, int level)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue((id, level), out var l)) return l;
            var def = Get(id);
            var up = level == 1 ? (1, 1) : Level(id, level - 1).Down;
            bool under = id == "undermere";
            l = new DungeonLevel(id, level, up, last: level == Levels, altar: def.Stones.ContainsKey(level), seal: under, codex: under && level == Levels);
            Cache[(id, level)] = l;
            return l;
        }
    }
}
