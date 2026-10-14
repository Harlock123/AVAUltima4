using AVAUltima4.Rules;

namespace AVAUltima4.World;

public enum TownTile : byte
{
    Grass, Path, Floor, Flowers, Door,
    Wall, Counter, Tree, Oak, Water, Fountain, Bed, Table, Shelf, Grave, Statue, Well, Chest, Throne, Fence,
}

/// <summary>Someone standing in a town: a person with things to say, a shopkeeper behind a counter, or a sheep.</summary>
public sealed class TownNpc
{
    public NpcDef? Def { get; init; }
    public required string Name { get; init; }
    public required string Sprite { get; init; }
    public string? Shop { get; init; }
    public bool Sheep { get; init; }
    public int X { get; set; }
    public int Y { get; set; }
    /// <summary>The part of town they keep to.</summary>
    public (int X0, int Y0, int X1, int Y1) Home { get; init; }
    /// <summary>Shopkeepers stand still behind their counters.</summary>
    public bool Still => Shop is not null || Def?.Kind == NpcKind.Queen;
}

/// <summary>
/// A town, laid out from its <see cref="TownDef"/>: a wall with a gate in the south, a square with a
/// fountain in the middle, and seven lots around it, each a shop, a house, a garden or the like.
/// </summary>
public sealed class TownMap
{
    public const int Size = 32;
    public const int GateX = 15, GateY = 30;

    private readonly TownTile[,] _tiles = new TownTile[Size, Size];
    public TownDef Def { get; }
    public string Id => Def.Id;
    public string Name => Def.Name;
    public List<TownNpc> Npcs { get; } = [];
    public (int X, int Y) Rune { get; private set; } = (-1, -1);
    public (int X, int Y) Tome { get; private set; } = (-1, -1);
    /// <summary>Where a lost purse lies (only in one town).</summary>
    public (int X0, int Y0, int X1, int Y1) PurseArea { get; private set; } = (-1, -1, -1, -1);
    public List<(int X, int Y)> Chests { get; } = [];

    public TownTile this[int x, int y] => x < 0 || y < 0 || x >= Size || y >= Size ? TownTile.Grass : _tiles[x, y];

    public static bool Walkable(TownTile t) => t <= TownTile.Door;

    public bool Inside(int x, int y) => x >= 0 && y >= 0 && x < Size && y < Size;

    /// <summary>The lots' corners: top-left, top-middle, top-right, middle-left, middle-right, bottom-left, bottom-right.</summary>
    public static readonly (int Col, int Row)[] LotCells = [(0, 0), (1, 0), (2, 0), (0, 1), (2, 1), (0, 2), (2, 2)];

    public static (int X0, int Y0, int X1, int Y1) LotRect(int lot)
    {
        var (c, r) = LotCells[lot];
        int x0 = 3 + c * 9, y0 = 3 + r * 9;
        return (x0, y0, x0 + 7, y0 + 6);
    }

    private static readonly Dictionary<string, TownMap> Cache = [];

    public static TownMap Get(string id)
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue(id, out var t)) Cache[id] = t = new TownMap(People.Town(id));
            return t;
        }
    }

    /// <summary>A fresh copy, with everyone back where they started.</summary>
    public static TownMap Fresh(string id) => new(People.Town(id));

    public TownMap(TownDef def)
    {
        Def = def;
        var rng = new Rng((ulong)def.Id.Aggregate(17, (h, ch) => h * 31 + ch));
        // The ground: grass outside the walls, streets inside.
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
            _tiles[x, y] = x is 0 or Size - 1 || y is 0 or Size - 1 ? TownTile.Grass : TownTile.Path;
        for (int i = 1; i < Size - 1; i++)
        {
            _tiles[i, 1] = _tiles[i, Size - 2] = _tiles[1, i] = _tiles[Size - 2, i] = TownTile.Wall;
        }
        _tiles[GateX, GateY] = _tiles[GateX + 1, GateY] = TownTile.Path;
        // Trees outside the wall.
        for (int i = 0; i < Size; i += 3)
        {
            if (i < 12 || i > 19) _tiles[i, Size - 1] = TownTile.Tree;
            _tiles[i, 0] = TownTile.Tree;
        }

        BuildSquare();
        BuildGreen(rng);
        bool hall = def.Lots.Take(3).All(l => l == "hall");
        if (hall) BuildThroneHall();
        for (int lot = 0; lot < 7; lot++)
        {
            if (hall && lot < 3) continue;
            BuildLot(lot, def.Lots[lot], rng);
        }
        PlacePeople(rng);
    }

    private void Fill(int x0, int y0, int x1, int y1, TownTile t)
    {
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
            _tiles[x, y] = t;
    }

    private void Set(int x, int y, TownTile t) => _tiles[x, y] = t;

    /// <summary>The town square: a fountain, flower beds and four trees.</summary>
    private void BuildSquare()
    {
        Fill(12, 12, 19, 18, TownTile.Grass);
        Fill(15, 12, 16, 18, TownTile.Path);
        Fill(12, 15, 19, 15, TownTile.Path);
        Set(15, 15, TownTile.Fountain);
        Set(16, 15, TownTile.Fountain);
        foreach (var (x, y) in new[] { (12, 12), (19, 12), (12, 18), (19, 18) }) Set(x, y, TownTile.Tree);
        foreach (var (x, y) in new[] { (13, 13), (18, 13), (13, 17), (18, 17) }) Set(x, y, TownTile.Flowers);
    }

    /// <summary>The green inside the gate: a road up the middle, grass and trees either side.</summary>
    private void BuildGreen(Rng rng)
    {
        Fill(12, 21, 19, 27, TownTile.Grass);
        Fill(15, 21, 16, 29, TownTile.Path);
        for (int i = 0; i < 6; i++)
        {
            int x = rng.Next(2) == 0 ? 12 + rng.Next(2) : 18 + rng.Next(2), y = 21 + rng.Next(7);
            Set(x, y, rng.Chance(60) ? TownTile.Tree : TownTile.Flowers);
        }
    }

    private void BuildThroneHall()
    {
        Fill(3, 3, 28, 9, TownTile.Wall);
        Fill(4, 4, 27, 8, TownTile.Floor);
        Set(15, 9, TownTile.Door);
        Set(16, 9, TownTile.Door);
        Set(15, 4, TownTile.Throne);
        Set(16, 4, TownTile.Throne);
        for (int x = 6; x <= 25; x += 4)
        {
            if (x is 14 or 18) continue;
            Set(x, 5, TownTile.Statue);
            Set(x, 7, TownTile.Statue);
        }
        Set(5, 4, TownTile.Shelf);
        Set(6, 4, TownTile.Shelf);
        Set(25, 4, TownTile.Table);
        Set(26, 4, TownTile.Table);
    }

    /// <summary>Which side of a lot faces the square: its door goes there.</summary>
    private static Dir DoorSide(int lot)
    {
        var (c, r) = LotCells[lot];
        return r == 0 ? Dir.South : r == 2 ? Dir.North : c == 0 ? Dir.East : Dir.West;
    }

    private static (int X, int Y) DoorAt((int X0, int Y0, int X1, int Y1) r, Dir side) => side switch
    {
        Dir.South => ((r.X0 + r.X1 + 1) / 2, r.Y1),
        Dir.North => ((r.X0 + r.X1 + 1) / 2, r.Y0),
        Dir.East => (r.X1, (r.Y0 + r.Y1) / 2),
        _ => (r.X0, (r.Y0 + r.Y1) / 2),
    };

    private readonly Dictionary<int, (int X, int Y)> _keeperSpot = [];
    private readonly Dictionary<int, (int X0, int Y0, int X1, int Y1)> _inside = [];

    private void BuildLot(int lot, string kind, Rng rng)
    {
        var r = LotRect(lot);
        var side = DoorSide(lot);
        bool open = kind is "garden" or "graves" or "pasture";
        if (open)
        {
            Fill(r.X0, r.Y0, r.X1, r.Y1, TownTile.Grass);
            _inside[lot] = r;
            BuildOpenLot(lot, kind, r, rng);
            return;
        }
        Fill(r.X0, r.Y0, r.X1, r.Y1, TownTile.Wall);
        Fill(r.X0 + 1, r.Y0 + 1, r.X1 - 1, r.Y1 - 1, TownTile.Floor);
        var door = DoorAt(r, side);
        Set(door.X, door.Y, TownTile.Door);
        var inner = (r.X0 + 1, r.Y0 + 1, r.X1 - 1, r.Y1 - 1);
        _inside[lot] = inner;
        var back = side.Back();
        bool shop = kind is "weapons" or "armour" or "food" or "reagents" or "healer" or "inn" or "guild" or "ship";
        if (shop)
        {
            // A counter across the room, two rows from the back wall, with the keeper behind it.
            var (bx, by) = back.Step();
            if (by != 0)
            {
                int keeperRow = by < 0 ? r.Y0 + 1 : r.Y1 - 1, counterRow = keeperRow - by;
                Fill(r.X0 + 1, counterRow, r.X1 - 1, counterRow, TownTile.Counter);
                _keeperSpot[lot] = (door.X, keeperRow);
                Set(r.X0 + 1, keeperRow, TownTile.Shelf);
                Set(r.X1 - 1, keeperRow, TownTile.Shelf);
            }
            else
            {
                int keeperCol = bx < 0 ? r.X0 + 1 : r.X1 - 1, counterCol = keeperCol - bx;
                Fill(counterCol, r.Y0 + 1, counterCol, r.Y1 - 1, TownTile.Counter);
                _keeperSpot[lot] = (keeperCol, door.Y);
                Set(keeperCol, r.Y0 + 1, TownTile.Shelf);
                Set(keeperCol, r.Y1 - 1, TownTile.Shelf);
            }
            if (kind == "inn")
            {
                // Beds along the front wall, either side of the door.
                var front = FrontCorners(r, side);
                Set(front.A.X, front.A.Y, TownTile.Bed);
                Set(front.B.X, front.B.Y, TownTile.Bed);
            }
            return;
        }
        switch (kind)
        {
            case "library":
            {
                for (int x = r.X0 + 1; x <= r.X1 - 1; x++)
                    if (x != door.X) Set(x, r.Y0 + 1, TownTile.Shelf);
                Set(r.X0 + 1, r.Y0 + 2, TownTile.Shelf);
                Set(r.X1 - 1, r.Y0 + 2, TownTile.Shelf);
                Set(r.X0 + 3, r.Y0 + 3, TownTile.Table);
                Set(r.X0 + 4, r.Y0 + 3, TownTile.Table);
                Tome = (r.X0 + 1, r.Y0 + 1);
                break;
            }
            case "hall" or "observatory":
            {
                Fill(r.X0 + 2, r.Y0 + 2, r.X1 - 2, r.Y0 + 2, TownTile.Table);
                if (kind == "observatory") Set(r.X0 + 1, r.Y0 + 1, TownTile.Statue);
                break;
            }
            default: // a house
            {
                var (a, b) = BackCorners(r, side);
                Set(a.X, a.Y, TownTile.Bed);
                Set(b.X, b.Y, TownTile.Chest);
                Chests.Add(b);
                int tx = (r.X0 + r.X1) / 2, ty = (r.Y0 + r.Y1) / 2;
                if (this[tx, ty] == TownTile.Floor && (tx, ty) != door) Set(tx, ty, TownTile.Table);
                break;
            }
        }
    }

    private static ((int X, int Y) A, (int X, int Y) B) BackCorners((int X0, int Y0, int X1, int Y1) r, Dir side) => side switch
    {
        Dir.South => ((r.X0 + 1, r.Y0 + 1), (r.X1 - 1, r.Y0 + 1)),
        Dir.North => ((r.X0 + 1, r.Y1 - 1), (r.X1 - 1, r.Y1 - 1)),
        Dir.East => ((r.X0 + 1, r.Y0 + 1), (r.X0 + 1, r.Y1 - 1)),
        _ => ((r.X1 - 1, r.Y0 + 1), (r.X1 - 1, r.Y1 - 1)),
    };

    private static ((int X, int Y) A, (int X, int Y) B) FrontCorners((int X0, int Y0, int X1, int Y1) r, Dir side) => side switch
    {
        Dir.South => ((r.X0 + 1, r.Y1 - 1), (r.X1 - 1, r.Y1 - 1)),
        Dir.North => ((r.X0 + 1, r.Y0 + 1), (r.X1 - 1, r.Y0 + 1)),
        Dir.East => ((r.X1 - 1, r.Y0 + 1), (r.X1 - 1, r.Y1 - 1)),
        _ => ((r.X0 + 1, r.Y0 + 1), (r.X0 + 1, r.Y1 - 1)),
    };

    private void BuildOpenLot(int lot, string kind, (int X0, int Y0, int X1, int Y1) r, Rng rng)
    {
        int cx = (r.X0 + r.X1) / 2, cy = (r.Y0 + r.Y1) / 2;
        switch (kind)
        {
            case "garden":
                for (int x = r.X0; x <= r.X1; x++)
                {
                    if (x % 2 == 0) Set(x, r.Y0, TownTile.Tree);
                    if (x % 2 == 1) Set(x, r.Y1, TownTile.Tree);
                }
                for (int i = 0; i < 5; i++) Set(r.X0 + 1 + rng.Next(6), r.Y0 + 1 + rng.Next(5), TownTile.Flowers);
                break;
            case "graves":
                for (int y = r.Y0 + 1; y <= r.Y1 - 1; y += 2)
                for (int x = r.X0 + 1; x <= r.X1 - 1; x += 2)
                    Set(x, y, TownTile.Grave);
                Set(r.X0, r.Y0, TownTile.Tree);
                Set(r.X1, r.Y1, TownTile.Tree);
                break;
            case "pasture":
                for (int x = r.X0; x <= r.X1; x++)
                {
                    Set(x, r.Y0, TownTile.Fence);
                    Set(x, r.Y1, TownTile.Fence);
                }
                for (int y = r.Y0; y <= r.Y1; y++)
                {
                    Set(r.X0, y, TownTile.Fence);
                    Set(r.X1, y, TownTile.Fence);
                }
                var gate = DoorAt(r, DoorSide(lot));
                Set(gate.X, gate.Y, TownTile.Grass);
                for (int i = 0; i < 3; i++)
                {
                    var spot = (r.X0 + 1 + rng.Next(6), r.Y0 + 1 + rng.Next(5));
                    Npcs.Add(new TownNpc { Name = "Sheep", Sprite = "sheep", Sheep = true, X = spot.Item1, Y = spot.Item2, Home = (r.X0 + 1, r.Y0 + 1, r.X1 - 1, r.Y1 - 1) });
                }
                break;
        }
        if (Def.RuneLot == lot)
        {
            // The rune's hiding place: something you search beside.
            var obj = Def.RuneObject switch
            {
                "statue" => TownTile.Statue,
                "well" => TownTile.Well,
                "grave" => TownTile.Grave,
                "fountain" => TownTile.Fountain,
                _ => TownTile.Oak,
            };
            if (Def.RuneObject == "grave")
            {
                // The rune grave stands apart, at the end of the rows.
                cx = r.X1 - 1;
                cy = r.Y1 - 1;
                Set(cx, cy - 1, TownTile.Grass);
            }
            Fill(cx - 1, cy - 1, cx + 1, cy + 1, kind == "graves" ? this[cx, cy] : TownTile.Grass);
            for (int y = cy - 1; y <= cy + 1; y++)
            for (int x = cx - 1; x <= cx + 1; x++)
                if (this[x, y] == TownTile.Grave && (x, y) != (cx, cy)) Set(x, y, TownTile.Grass);
            Set(cx, cy, obj);
            Rune = (cx, cy);
        }
        if (Def.Id == "hearthwell" && kind == "garden") PurseArea = (r.X0 + 1, r.Y0 + 1, r.X1 - 1, r.Y1 - 1);
    }

    private void PlacePeople(Rng rng)
    {
        var square = (12, 12, 19, 18);
        // Shopkeepers behind their counters.
        for (int lot = 0; lot < 7; lot++)
            if (_keeperSpot.TryGetValue(lot, out var spot) && Def.Keepers.TryGetValue(Def.Lots[lot], out var keeper))
                Npcs.Add(new TownNpc { Name = keeper, Sprite = "merchant", Shop = Def.Lots[lot], X = spot.X, Y = spot.Y, Home = (spot.X, spot.Y, spot.X, spot.Y) });
        foreach (var def in People.In(Def.Id))
        {
            (int X0, int Y0, int X1, int Y1) home = def.Lot < 0 ? square : def.Kind == NpcKind.Queen ? (15, 5, 16, 5)
                : def.Lot < 3 && Def.Lots[0] == "hall" ? (4, 5, 27, 8) : _inside[def.Lot];
            var at = FreeSpot(home, rng);
            if (def.Kind == NpcKind.Queen) at = (15, 5);
            Npcs.Add(new TownNpc { Def = def, Name = def.Name, Sprite = def.Sprite, X = at.X, Y = at.Y, Home = home });
        }
        // Two guards at the gate.
        foreach (int gx in new[] { 13, 18 })
            Npcs.Add(new TownNpc { Name = "Guard", Sprite = "guard", X = gx, Y = 28, Home = (gx - 1, 26, gx + 1, 29) });
    }

    private (int X, int Y) FreeSpot((int X0, int Y0, int X1, int Y1) r, Rng rng)
    {
        for (int tries = 0; tries < 200; tries++)
        {
            int x = rng.Range(r.X0, r.X1), y = rng.Range(r.Y0, r.Y1);
            if (Walkable(this[x, y]) && this[x, y] != TownTile.Door && NpcAt(x, y) is null) return (x, y);
        }
        return (r.X0, r.Y0);
    }

    public TownNpc? NpcAt(int x, int y) => Npcs.FirstOrDefault(n => n.X == x && n.Y == y);

    /// <summary>The townsfolk take a step or stand still, keeping to their own part of town and out of the party's way.</summary>
    public void Wander(Rng rng, int px, int py)
    {
        foreach (var n in Npcs)
        {
            if (n.Still || !rng.Chance(n.Sheep ? 30 : 45)) continue;
            var (dx, dy) = ((Dir)rng.Next(4)).Step();
            int nx = n.X + dx, ny = n.Y + dy;
            if (nx < n.Home.X0 || nx > n.Home.X1 || ny < n.Home.Y0 || ny > n.Home.Y1) continue;
            if (!Walkable(this[nx, ny]) || this[nx, ny] == TownTile.Door || (nx, ny) == (px, py) || NpcAt(nx, ny) is not null) continue;
            n.X = nx;
            n.Y = ny;
        }
    }

    /// <summary>A shopkeeper across a counter from a square, looking one way.</summary>
    public TownNpc? KeeperAcross(int x, int y, Dir d)
    {
        var (dx, dy) = d.Step();
        if (this[x + dx, y + dy] != TownTile.Counter) return null;
        return NpcAt(x + 2 * dx, y + 2 * dy) is { Shop: not null } k ? k : null;
    }

    public string Dump()
    {
        var sb = new System.Text.StringBuilder();
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                char c = this[x, y] switch
                {
                    TownTile.Grass => '.', TownTile.Path => ' ', TownTile.Floor => '_', TownTile.Flowers => '*', TownTile.Door => 'D',
                    TownTile.Wall => '#', TownTile.Counter => '=', TownTile.Tree => 'T', TownTile.Oak => 'O', TownTile.Water => '~',
                    TownTile.Fountain => 'F', TownTile.Bed => 'b', TownTile.Table => 't', TownTile.Shelf => 'S', TownTile.Grave => '+',
                    TownTile.Statue => '&', TownTile.Well => 'W', TownTile.Chest => 'c', TownTile.Throne => 'K', TownTile.Fence => '|', _ => '?',
                };
                if (NpcAt(x, y) is { } n) c = n.Shop is not null ? '$' : n.Sheep ? 's' : '@';
                if ((x, y) == Rune) c = 'R';
                sb.Append(c);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
