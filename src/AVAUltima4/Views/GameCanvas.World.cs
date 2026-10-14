using Avalonia;
using Avalonia.Media;
using AVAUltima4.Game;
using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Views;

public sealed partial class GameCanvas
{
    private int WaveFrame => (int)(Engine.Clock * 2) % 8;

    private static string GroundKind(Tile t) => t switch
    {
        Tile.Deep => "deep",
        Tile.Water => "water",
        Tile.Shallows => "shallows",
        Tile.Sand => "sand",
        Tile.Brush => "brush",
        Tile.Forest => "forest",
        Tile.Hills => "hills",
        Tile.Mountains => "mountains",
        Tile.Swamp => "swamp",
        _ => "grass",
    };

    private static string? SiteSprite(Tile t) => t switch
    {
        Tile.Town => "town",
        Tile.Village => "village",
        Tile.Castle => "castle",
        Tile.Shrine => "shrine",
        Tile.Dungeon => "dungeon",
        _ => null,
    };

    /// <summary>The open land, eleven squares across, with the party in the middle.</summary>
    private void DrawLand(DrawingContext dc)
    {
        var land = GameEngine.Land;
        var w = Engine.World;
        var gate = Engine.OpenGate;
        for (int dy = -5; dy <= 5; dy++)
        for (int dx = -5; dx <= 5; dx++)
        {
            int mx = w.X + dx, my = w.Y + dy;
            double x = View.X + (dx + 5) * T, y = View.Y + (dy + 5) * T;
            var tile = land[mx, my];
            bool water = Terrain.IsWater(tile);
            Image(dc, Sprites.GroundBitmap(GroundKind(tile), water ? (WaveFrame + mx) % 8 : (mx * 7 + my * 13) % 3), x, y, T);
            if (SiteSprite(tile) is { } site) Sprite(dc, site, 0xFFFFFFFF, x, y, T);
            if ((mx, my) == gate) DrawMoongate(dc, x, y);
            if (w.HasShip && !w.Aboard && (mx, my) == (w.ShipX, w.ShipY)) Sprite(dc, "ship", 0xFFFFFFFF, x, y, T);
        }
        foreach (var r in Engine.Roamers)
        {
            int dx = r.X - w.X, dy = r.Y - w.Y;
            if (Math.Abs(dx) > 5 || Math.Abs(dy) > 5) continue;
            double x = View.X + (dx + 5) * T, y = View.Y + (dy + 5) * T;
            Sprite(dc, r.Sprite, Sprites.Parse(r.Tint), x, y, T, flip: dx > 0);
            if (r.Group.Count > 1) Shadowed(dc, r.Group.Count.ToString(), x + T - 6, y + T - 24, 22, Ink, 2);
        }
        double cx = View.X + 5 * T, cy = View.Y + 5 * T;
        if (w.Aboard) Sprite(dc, "ship", 0xFFFFFFFF, cx, cy, T, flip: w.Facing == Dir.West);
        else if (w.Party.Count > 0) Sprite(dc, ClassSprite(w.Leader.Class), MemberTint(0), cx, cy, T);
    }

    private void DrawMoongate(DrawingContext dc, double x, double y)
    {
        double pulse = 0.7 + 0.3 * Math.Sin(Engine.Clock * 5);
        var c = new Point(x + T / 2, y + T / 2);
        for (int i = 4; i >= 1; i--)
        {
            uint a = (uint)(pulse * (40 + (4 - i) * 45)) << 24;
            dc.DrawEllipse(Brush(a | 0x0080B0FF), null, c, T * 0.12 * i, T * 0.11 * i * 1.4);
        }
        dc.DrawEllipse(null, Pen(0xFFE0F0FF, 2), c, T * 0.3, T * 0.42);
    }

    /// <summary>The ground a town square shows, and the thing standing on it, if any.</summary>
    private static (string Ground, string? Thing) TownLook(TownTile t) => t switch
    {
        TownTile.Grass => ("grass", null),
        TownTile.Path => ("path", null),
        TownTile.Floor => ("floor", null),
        TownTile.Flowers => ("grass", "flowers"),
        TownTile.Door => ("floor", "door"),
        TownTile.Wall => ("wall", null),
        TownTile.Counter => ("floor", "counter"),
        TownTile.Tree => ("grass", "tree"),
        TownTile.Oak => ("grass", "oak"),
        TownTile.Water => ("water", null),
        TownTile.Fountain => ("path", "fountain"),
        TownTile.Bed => ("floor", "bed"),
        TownTile.Table => ("floor", "table"),
        TownTile.Shelf => ("floor", "shelf"),
        TownTile.Grave => ("grass", "grave"),
        TownTile.Statue => ("grass", "statue"),
        TownTile.Well => ("grass", "well"),
        TownTile.Chest => ("floor", "chest"),
        TownTile.Throne => ("floor", "throne"),
        TownTile.Fence => ("grass", "fence"),
        _ => ("grass", null),
    };

    public static uint NpcTint(TownNpc n)
    {
        if (n.Def?.Joins is { } cls) return ClassTint(cls);
        uint h = (uint)n.Name.Aggregate(7, (a, ch) => a * 31 + ch);
        uint[] folk = [0xFF8A4A3A, 0xFF3A6A8A, 0xFF6A7A3A, 0xFF8A6A2A, 0xFF5A4A8A, 0xFF8A3A6A];
        return n.Sprite switch
        {
            "queen" => 0xFF7A3AB0,
            "guard" => 0xFF3050A0,
            "sage" => 0xFF4A5AA0,
            "noble" => 0xFFA03030,
            "merchant" => 0xFF2A7040,
            "beggar" => 0xFF7A6A50,
            "child" => 0xFFE08030,
            "fighter" => ClassTint(ClassKind.Fighter),
            "mage" => ClassTint(ClassKind.Mage),
            "druid" => ClassTint(ClassKind.Druid),
            "paladin" => ClassTint(ClassKind.Paladin),
            "shepherd" => ClassTint(ClassKind.Shepherd),
            "ranger" => ClassTint(ClassKind.Ranger),
            "tinker" => ClassTint(ClassKind.Tinker),
            "bard" => ClassTint(ClassKind.Bard),
            _ => folk[h % folk.Length],
        };
    }

    /// <summary>A town, eleven squares across, around the party.</summary>
    private void DrawTown(DrawingContext dc)
    {
        var t = Engine.Town;
        if (t is null) return;
        var w = Engine.World;
        for (int dy = -5; dy <= 5; dy++)
        for (int dx = -5; dx <= 5; dx++)
        {
            int mx = w.X + dx, my = w.Y + dy;
            double x = View.X + (dx + 5) * T, y = View.Y + (dy + 5) * T;
            var (ground, thing) = t.Inside(mx, my) ? TownLook(t[mx, my]) : ("grass", (mx + my) % 3 == 0 ? "tree" : null);
            Image(dc, Sprites.GroundBitmap(ground, ground == "water" ? (WaveFrame + mx) % 8 : 0), x, y, T);
            if (thing is not null) Sprite(dc, thing, thing == "bed" ? 0xFF9A3040 : 0xFFFFFFFF, x, y, T);
        }
        foreach (var n in t.Npcs)
        {
            int dx = n.X - w.X, dy = n.Y - w.Y;
            if (Math.Abs(dx) > 5 || Math.Abs(dy) > 5) continue;
            Sprite(dc, n.Sprite, n.Sheep ? 0xFFFFFFFF : NpcTint(n), View.X + (dx + 5) * T, View.Y + (dy + 5) * T, T);
        }
        Sprite(dc, ClassSprite(w.Leader.Class), MemberTint(0), View.X + 5 * T, View.Y + 5 * T, T);
    }

    // ───────────────────────────── the seeing-gem ─────────────────────────────

    private static readonly Dictionary<Tile, uint> MapColours = new()
    {
        [Tile.Deep] = 0xFF14305C, [Tile.Water] = 0xFF22549A, [Tile.Shallows] = 0xFF4A88C0, [Tile.Sand] = 0xFFD0B878,
        [Tile.Grass] = 0xFF4E9A3A, [Tile.Brush] = 0xFF3E8030, [Tile.Forest] = 0xFF285A24, [Tile.Hills] = 0xFF7A9A50,
        [Tile.Mountains] = 0xFF9A948A, [Tile.Swamp] = 0xFF4A5A30,
    };

    private void DrawGem(DrawingContext dc, Rect r)
    {
        dc.FillRectangle(Brush(0xFF000000), r);
        var w = Engine.World;
        if (w.Where == Where.Dungeon && Engine.Dungeon is { } lvl)
        {
            double cs = Math.Floor((r.Height - 40) / DungeonLevel.Size);
            double ox = r.X + (r.Width - cs * DungeonLevel.Size) / 2, oy = r.Y + 20;
            for (int y = 0; y < DungeonLevel.Size; y++)
            for (int x = 0; x < DungeonLevel.Size; x++)
            {
                var cell = lvl[x, y];
                uint col = cell switch
                {
                    Cell.Wall => 0xFF3A3448, Cell.Up => 0xFF60C060, Cell.Down => 0xFFC08040, Cell.Altar => 0xFFFFD860,
                    Cell.Fountain => 0xFF4080E0, Cell.Seal => 0xFFA040C0, _ => 0xFF8A8478,
                };
                dc.FillRectangle(Brush(col), new Rect(ox + x * cs, oy + y * cs, cs - 1, cs - 1));
            }
            dc.DrawEllipse(Brush(0xFFFF4040), Pen(0xFFFFFFFF, 2), new Point(ox + (w.X + 0.5) * cs, oy + (w.Y + 0.5) * cs), cs / 3, cs / 3);
            return;
        }
        var land = GameEngine.Land;
        double s = Math.Floor((r.Height - 20) / Overworld.Size);
        double mx0 = r.X + (r.Width - s * Overworld.Size) / 2, my0 = r.Y + 10;
        for (int y = 0; y < Overworld.Size; y++)
        for (int x = 0; x < Overworld.Size; x++)
        {
            var tile = land[x, y];
            uint col = MapColours.GetValueOrDefault(tile, 0xFFFFFFFF);
            dc.FillRectangle(Brush(col), new Rect(mx0 + x * s, my0 + y * s, s, s));
        }
        foreach (var site in land.Sites)
        {
            uint col = site.Kind switch { SiteKind.Shrine => 0xFFFFFFFF, SiteKind.Dungeon => 0xFF202020, SiteKind.Castle => 0xFFFFD860, _ => 0xFFE04040 };
            dc.FillRectangle(Brush(col), new Rect(mx0 + site.X * s - 1, my0 + site.Y * s - 1, s + 2, s + 2));
        }
        foreach (var g in land.Gates) dc.DrawEllipse(null, Pen(0xFF80C0FF, 1.5), new Point(mx0 + (g.X + 0.5) * s, my0 + (g.Y + 0.5) * s), s, s);
        double pulse = 0.5 + 0.5 * Math.Sin(Engine.Clock * 6);
        dc.DrawEllipse(Brush(Sprites.Mix(0xFFFF2020, 0xFFFFFFFF, pulse)), null, new Point(mx0 + (w.X + 0.5) * s, my0 + (w.Y + 0.5) * s), s * 1.3, s * 1.3);
    }
}
