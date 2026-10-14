using AVAUltima4.Rules;

namespace AVAUltima4.World;

public enum SiteKind { Town, Village, Castle, Shrine, Dungeon }

/// <summary>A place on the map you can go into: a town, the castle, a shrine or a dungeon.</summary>
public sealed record Site(string Id, string Name, SiteKind Kind, int X, int Y, Virtue? Virtue = null);

/// <summary>
/// The land of Merovale: an island continent, grown from a fixed seed so that it is the same for
/// everyone (and the clues people give, which name places by their map coordinates, stay true).
/// The Isle of the Undermere lies off the coast and can be reached only by ship.
/// </summary>
public sealed class Overworld
{
    public const int Size = 96;

    private readonly Tile[,] _tiles = new Tile[Size, Size];
    public List<Site> Sites { get; } = [];
    /// <summary>The moongates: one for each phase of the pale moon.</summary>
    public List<(int X, int Y)> Gates { get; } = [];
    public (int X, int Y) ShipStart { get; private set; }
    /// <summary>Where the Brass Horn lies on the sea bed (searched for from a ship).</summary>
    public (int X, int Y) Horn { get; private set; }
    /// <summary>Where the Everflame Taper is buried in the marsh.</summary>
    public (int X, int Y) Taper { get; private set; }
    public int Seed { get; private set; }

    private static readonly Lazy<Overworld> Shared = new(Build);
    public static Overworld Instance => Shared.Value;

    public Tile this[int x, int y] => x < 0 || y < 0 || x >= Size || y >= Size ? Tile.Deep : _tiles[x, y];

    public Site? SiteAt(int x, int y) => Sites.FirstOrDefault(s => s.X == x && s.Y == y);
    public Site Site(string id) => Sites.First(s => s.Id == id);

    /// <summary>Where a town, castle or village is: its id, name, kind and roughly where on the map (as fractions).</summary>
    private static readonly (string Id, string Name, SiteKind Kind, double Fx, double Fy, Virtue? V)[] Places =
    [
        ("brightholm", "Castle Brightholm", SiteKind.Castle, 0.50, 0.47, null),
        ("glassmere", "Glassmere", SiteKind.Town, 0.27, 0.24, Virtue.Candor),
        ("hearthwell", "Hearthwell", SiteKind.Town, 0.70, 0.78, Virtue.Mercy),
        ("ironcrag", "Ironcrag", SiteKind.Town, 0.73, 0.22, Virtue.Daring),
        ("evenholt", "Evenholt", SiteKind.Town, 0.27, 0.62, Virtue.Fairness),
        ("tallowford", "Tallowford", SiteKind.Town, 0.50, 0.74, Virtue.Selflessness),
        ("oathgate", "Oathgate", SiteKind.Town, 0.80, 0.50, Virtue.Integrity),
        ("starfall", "Starfall", SiteKind.Town, 0.18, 0.42, Virtue.Reverence),
        ("meekbrook", "Meekbrook", SiteKind.Village, 0.48, 0.20, Virtue.Modesty),
    ];

    private static readonly (string Id, string Name, double Fx, double Fy)[] Dungeons =
    [
        ("hollowroot", "Hollowroot", 0.36, 0.40),
        ("mirrormine", "the Mirrored Mine", 0.62, 0.30),
        ("cindervault", "the Cinder Vaults", 0.34, 0.80),
        ("saltdeep", "Saltdeep", 0.84, 0.66),
    ];

    public static Overworld Build()
    {
        for (int seed = 7; ; seed++)
        {
            var w = new Overworld();
            if (w.TryGenerate(seed)) return w;
        }
    }

    // ───────────────────────────── noise ─────────────────────────────

    private static double Lattice(int seed, int x, int y)
    {
        uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519u);
        h = (h ^ (h >> 13)) * 1274126177u;
        h ^= h >> 16;
        return (h & 0xFFFFFF) / (double)0xFFFFFF;
    }

    private static double Smooth(double t) => t * t * (3 - 2 * t);

    private static double Value(int seed, double x, double y)
    {
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
        double fx = Smooth(x - x0), fy = Smooth(y - y0);
        double a = Lattice(seed, x0, y0), b = Lattice(seed, x0 + 1, y0), c = Lattice(seed, x0, y0 + 1), d = Lattice(seed, x0 + 1, y0 + 1);
        return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy;
    }

    private static double Fbm(int seed, double x, double y, int octaves)
    {
        double sum = 0, amp = 1, norm = 0, f = 1;
        for (int i = 0; i < octaves; i++)
        {
            sum += Value(seed + i * 101, x * f, y * f) * amp;
            norm += amp;
            amp *= 0.5;
            f *= 2;
        }
        return sum / norm;
    }

    // ───────────────────────────── generation ─────────────────────────────

    private bool TryGenerate(int seed)
    {
        Seed = seed;
        Sites.Clear();
        Gates.Clear();
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            double nx = x / (double)Size - 0.5, ny = y / (double)Size - 0.5;
            double d = Math.Sqrt(nx * nx + ny * ny) * 2; // 0 in the middle, 1 at the edges' middles
            double e = Fbm(seed, x / 20.0, y / 20.0, 5) * 0.92 + (1 - d * 1.3) * 0.40 - 0.03;
            double m = Fbm(seed + 777, x / 18.0, y / 18.0, 4);
            _tiles[x, y] = e switch
            {
                < 0.36 => Tile.Deep,
                < 0.43 => Tile.Water,
                < 0.46 => Tile.Shallows,
                < 0.48 => m > 0.6 ? Tile.Swamp : Tile.Sand,
                > 0.775 => Tile.Mountains,
                > 0.70 => Tile.Hills,
                _ => m switch
                {
                    > 0.64 when e < 0.56 => Tile.Swamp,
                    > 0.55 => Tile.Forest,
                    > 0.47 => Tile.Brush,
                    _ => Tile.Grass,
                },
            };
            if (x == 0 || y == 0 || x == Size - 1 || y == Size - 1) _tiles[x, y] = Tile.Deep;
        }

        var main = Region((x, y) => Terrain.Walkable(this[x, y]), Size / 2, Size / 2);
        if (main.Count < 2600) return false;

        foreach (var p in Places)
        {
            var spot = Nearest(p.Fx, p.Fy, (x, y) => main.Contains((x, y)) && this[x, y] is Tile.Grass or Tile.Brush && Clear(x, y, 7)
                && (p.Id != "hearthwell" || Coastal(x, y)));
            if (spot is null) return false;
            Place(p.Id, p.Name, p.Kind, spot.Value, p.V);
        }
        // A shrine near each virtue's town, a little way out into the country.
        foreach (var v in Virtues.All)
        {
            var town = Site(v.Town);
            var spot = Nearest(town.X / (double)Size, town.Y / (double)Size,
                (x, y) => main.Contains((x, y)) && Dist(x, y, town.X, town.Y) is >= 5 and <= 11 && this[x, y] is Tile.Grass or Tile.Brush or Tile.Sand && Clear(x, y, 4),
                farFirst: false, minDist: 5);
            if (spot is null) return false;
            Place("shrine-" + v.Virtue.ToString().ToLowerInvariant(), $"the Shrine of {v.Virtue}", SiteKind.Shrine, spot.Value, v.Virtue);
        }
        foreach (var dgn in Dungeons)
        {
            var spot = Nearest(dgn.Fx, dgn.Fy, (x, y) => main.Contains((x, y)) && this[x, y] is Tile.Hills or Tile.Brush or Tile.Grass or Tile.Forest
                && Clear(x, y, 6) && Neighbours(x, y).Any(n => this[n.X, n.Y] is Tile.Hills or Tile.Mountains));
            if (spot is null) return false;
            Place(dgn.Id, dgn.Name, SiteKind.Dungeon, spot.Value);
        }

        // The ocean: open water joined to the edge of the map.
        var ocean = Region((x, y) => Terrain.Sailable(this[x, y]), 0, 0);
        var port = Site("hearthwell");
        var dock = Neighbours(port.X, port.Y).Where(n => ocean.Contains(n)).OrderBy(n => this[n.X, n.Y] == Tile.Shallows ? 1 : 0).FirstOrDefault();
        if (dock == default) return false;
        ShipStart = dock;

        // The Isle of the Undermere: raised out of the open sea, well clear of the coast.
        var isle = Nearest(0.88, 0.90, (x, y) => ocean.Contains((x, y)) && AllWater(x, y, 7), farFirst: false);
        if (isle is null) return false;
        var (ix, iy) = isle.Value;
        for (int dy = -5; dy <= 5; dy++)
        for (int dx = -5; dx <= 5; dx++)
        {
            double r = Math.Sqrt(dx * dx + dy * dy);
            if (r <= 2.3) _tiles[ix + dx, iy + dy] = r < 1 ? Tile.Hills : Tile.Grass;
            else if (r <= 3.3) _tiles[ix + dx, iy + dy] = (dx + dy) % 3 == 0 ? Tile.Sand : Tile.Brush;
            else if (r <= 4.4) _tiles[ix + dx, iy + dy] = Tile.Shallows;
        }
        Place("undermere", "the Undermere", SiteKind.Dungeon, (ix, iy));
        ocean = Region((x, y) => Terrain.Sailable(this[x, y]), 0, 0);
        if (!ocean.Contains(ShipStart) || !Neighbours(ix + 3, iy).Concat(Neighbours(ix - 3, iy)).Any(n => ocean.Contains(n))) return false;

        // The horn on the sea bed, far from any shore; the taper in a marsh.
        var horn = Nearest(0.12, 0.88, (x, y) => ocean.Contains((x, y)) && this[x, y] == Tile.Deep && AllWater(x, y, 4), farFirst: false);
        if (horn is null) return false;
        Horn = horn.Value;
        var taper = Nearest(0.62, 0.58, (x, y) => main.Contains((x, y)) && this[x, y] == Tile.Swamp && Clear(x, y, 4), farFirst: false);
        if (taper is null)
        {
            // No marsh where one is wanted: make a little one.
            var at = Nearest(0.62, 0.58, (x, y) => main.Contains((x, y)) && this[x, y] is Tile.Grass or Tile.Brush && Clear(x, y, 5), farFirst: false);
            if (at is null) return false;
            foreach (var n in Neighbours(at.Value.X, at.Value.Y).Append(at.Value))
                if (this[n.X, n.Y] is Tile.Grass or Tile.Brush) _tiles[n.X, n.Y] = Tile.Swamp;
            taper = at;
        }
        Taper = taper.Value;

        // Eight moongates, in a ring around the castle.
        var castle = Site("brightholm");
        for (int i = 0; i < 8; i++)
        {
            double ang = i * Math.PI / 4 - Math.PI / 2;
            double fx = (castle.X + Math.Cos(ang) * 30) / Size, fy = (castle.Y + Math.Sin(ang) * 30) / Size;
            var g = Nearest(fx, fy, (x, y) => main.Contains((x, y)) && this[x, y] is Tile.Grass or Tile.Brush && Clear(x, y, 3) && !Gates.Any(p => Dist(p.X, p.Y, x, y) < 10), farFirst: false);
            if (g is null) return false;
            Gates.Add(g.Value);
        }
        // Every site can be walked to from the castle (but the isle, which takes a ship).
        var reach = Region((x, y) => Terrain.Walkable(this[x, y]), castle.X, castle.Y);
        return Sites.Where(s => s.Id != "undermere").All(s => reach.Contains((s.X, s.Y))) && Gates.All(reach.Contains);
    }

    private void Place(string id, string name, SiteKind kind, (int X, int Y) at, Virtue? v = null)
    {
        Sites.Add(new Site(id, name, kind, at.X, at.Y, v));
        _tiles[at.X, at.Y] = kind switch
        {
            SiteKind.Town => Tile.Town,
            SiteKind.Village => Tile.Village,
            SiteKind.Castle => Tile.Castle,
            SiteKind.Shrine => Tile.Shrine,
            _ => Tile.Dungeon,
        };
    }

    public static int Dist(int x0, int y0, int x1, int y1) => Math.Max(Math.Abs(x0 - x1), Math.Abs(y0 - y1));

    /// <summary>No site within a distance.</summary>
    private bool Clear(int x, int y, int distance) => Sites.All(s => Dist(s.X, s.Y, x, y) >= distance) && Gates.All(g => Dist(g.X, g.Y, x, y) >= 2);

    private bool Coastal(int x, int y) => Neighbours(x, y).Any(n => Terrain.Sailable(this[n.X, n.Y]));

    private bool AllWater(int x, int y, int r)
    {
        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
            if (!Terrain.IsWater(this[x + dx, y + dy]) || x + dx <= 0 || y + dy <= 0 || x + dx >= Size - 1 || y + dy >= Size - 1) return false;
        return true;
    }

    public static IEnumerable<(int X, int Y)> Neighbours(int x, int y)
    {
        yield return (x, y - 1);
        yield return (x + 1, y);
        yield return (x, y + 1);
        yield return (x - 1, y);
    }

    private (int X, int Y)? Nearest(double fx, double fy, Func<int, int, bool> ok, bool farFirst = false, int minDist = 0)
    {
        int tx = (int)(fx * Size), ty = (int)(fy * Size);
        (int, int)? best = null;
        double bestD = double.MaxValue;
        for (int y = 1; y < Size - 1; y++)
        for (int x = 1; x < Size - 1; x++)
        {
            double d = Math.Sqrt((x - tx) * (x - tx) + (y - ty) * (y - ty));
            if (d >= bestD || d < minDist || !ok(x, y)) continue;
            bestD = d;
            best = (x, y);
        }
        return best;
    }

    /// <summary>Every square joined to a start square through squares that pass a test (four ways).</summary>
    public HashSet<(int X, int Y)> Region(Func<int, int, bool> pass, int sx, int sy)
    {
        var seen = new HashSet<(int, int)>();
        if (!pass(sx, sy))
        {
            // Start from the nearest square that passes.
            var near = Nearest(sx / (double)Size, sy / (double)Size, pass);
            if (near is null) return seen;
            (sx, sy) = near.Value;
        }
        var q = new Queue<(int, int)>();
        q.Enqueue((sx, sy));
        seen.Add((sx, sy));
        while (q.Count > 0)
        {
            var (x, y) = q.Dequeue();
            foreach (var n in Neighbours(x, y))
            {
                if (n.X < 0 || n.Y < 0 || n.X >= Size || n.Y >= Size || seen.Contains(n) || !pass(n.X, n.Y)) continue;
                seen.Add(n);
                q.Enqueue(n);
            }
        }
        return seen;
    }

    /// <summary>The map as letters, for looking at in a test.</summary>
    public string Dump()
    {
        var sb = new System.Text.StringBuilder();
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                char c = this[x, y] switch
                {
                    Tile.Deep => ' ', Tile.Water => '~', Tile.Shallows => '-', Tile.Sand => ':', Tile.Grass => '.', Tile.Brush => ',',
                    Tile.Forest => 'f', Tile.Hills => 'h', Tile.Mountains => 'M', Tile.Swamp => 's', Tile.Town => 'T', Tile.Village => 'V',
                    Tile.Castle => 'C', Tile.Shrine => '+', Tile.Dungeon => 'D', _ => '?',
                };
                if (Gates.Contains((x, y))) c = 'O';
                if ((x, y) == ShipStart) c = 'B';
                sb.Append(c);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
