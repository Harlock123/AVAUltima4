using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AVAUltima4.Game;
using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Views;

public sealed partial class GameCanvas
{
    // The layout: an eleven-by-eleven view of the world on the left, the party and messages on the right.
    public const double T = 64;
    private static readonly Rect View = new(16, 16, 11 * T, 11 * T);
    private static readonly Rect Roster = new(736, 16, 272, 330);
    private static readonly Rect Status = new(736, 354, 272, 102);
    private static readonly Rect Messages = new(736, 464, 272, 288);
    private static readonly Rect Bar = new(16, 726, 704, 26);

    private static readonly Typeface Retro = new(new FontFamily("avares://AVAUltima4/Assets/Fonts#VT323"));
    private static readonly Dictionary<uint, IBrush> BrushCache = [];
    private static readonly Dictionary<(uint, double), Pen> PenCache = [];

    public static IBrush Brush(uint argb)
    {
        if (!BrushCache.TryGetValue(argb, out var b))
            BrushCache[argb] = b = new SolidColorBrush(Color.FromUInt32(argb)).ToImmutable();
        return b;
    }

    private static Pen Pen(uint argb, double width = 1)
    {
        if (!PenCache.TryGetValue((argb, width), out var p))
            PenCache[(argb, width)] = p = new Pen(Brush(argb), width);
        return p;
    }

    private const uint Ground = 0xFF08080E, PanelFill = 0xFF12121C, Frame = 0xFF5A6EA8, Ink = 0xFFE8E4D8, Gold = 0xFFFFD860,
        Dim = 0xFF8A8A9A, Good = 0xFF60D080, Warn = 0xFFE0C040, Bad = 0xFFE05040, Blue = 0xFF80B0FF;

    private static FormattedText Fmt(string text, double size, uint colour) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Retro, size, Brush(colour));

    private static double Measure(string text, double size) => Fmt(text, size, Ink).Width;

    /// <summary>Text with its top-left corner at a point (or centred, or right-aligned, on it).</summary>
    private static void Text(DrawingContext dc, string text, double x, double y, double size, uint colour, int align = 0)
    {
        var ft = Fmt(text, size, colour);
        double left = align == 0 ? x : align == 1 ? x - ft.Width / 2 : x - ft.Width;
        dc.DrawText(ft, new Point(left, y));
    }

    private static void Shadowed(DrawingContext dc, string text, double x, double y, double size, uint colour, int align = 0)
    {
        Text(dc, text, x + 2, y + 2, size, 0xC0000000, align);
        Text(dc, text, x, y, size, colour, align);
    }

    private static readonly Dictionary<(string, double, double), List<string>> WrapCache = [];

    /// <summary>Splits text into lines that fit a width.</summary>
    private static List<string> Wrap(string text, double width, double size)
    {
        if (WrapCache.TryGetValue((text, width, size), out var cached)) return cached;
        var lines = new List<string>();
        var line = "";
        foreach (var word in text.Split(' '))
        {
            string tryLine = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && Measure(tryLine, size) > width)
            {
                lines.Add(line);
                line = word;
            }
            else line = tryLine;
        }
        if (line.Length > 0) lines.Add(line);
        if (WrapCache.Count > 3000) WrapCache.Clear();
        WrapCache[(text, width, size)] = lines;
        return lines;
    }

    private static void Box(DrawingContext dc, Rect r, uint fill = PanelFill, uint border = Frame)
    {
        dc.FillRectangle(Brush(fill), r);
        dc.DrawRectangle(Pen(border, 2), r.Deflate(1));
        dc.DrawRectangle(Pen(Sprites.Shade(border, 0.5), 1), r.Deflate(4));
    }

    private static void Image(DrawingContext dc, Bitmap b, double x, double y, double size, double alpha = 1)
    {
        if (alpha < 1)
            using (dc.PushOpacity(alpha))
                dc.DrawImage(b, new Rect(0, 0, 16, 16), new Rect(x, y, size, size));
        else dc.DrawImage(b, new Rect(0, 0, 16, 16), new Rect(x, y, size, size));
    }

    /// <summary>A sprite in a tint, drawn at a size.</summary>
    private static void Sprite(DrawingContext dc, string name, uint tint, double x, double y, double size, double alpha = 1, bool flip = false)
    {
        if (!Sprites.All.ContainsKey(name)) return;
        Image(dc, Sprites.Get(name, tint, flip), x, y, size, alpha);
    }

    /// <summary>The colour a class's sprite is drawn in: the colour of its virtue's stone (softened for the darkest).</summary>
    public static uint ClassTint(ClassKind k)
    {
        var v = Virtues.All.First(i => i.Class == k);
        return v.Virtue switch { Virtue.Modesty => 0xFF707080, Virtue.Reverence => 0xFFD8D8E0, _ => v.StoneColour };
    }

    private static string ClassSprite(ClassKind k) => k.ToString().ToLowerInvariant();

    private uint MemberTint(int i) => Sprites.PartyTints[i % Sprites.PartyTints.Length];

    private void DrawScreen(DrawingContext dc)
    {
        dc.FillRectangle(Brush(Ground), new Rect(0, 0, ScreenWidth, ScreenHeight));
        switch (Engine.Mode)
        {
            case Mode.Title: DrawTitle(dc); break;
            case Mode.Ending: DrawEnding(dc); break;
            case Mode.Battle: DrawBattleScreen(dc); break;
            default: DrawPlay(dc); break;
        }
        if (Engine.InDemo)
        {
            double pulse = 0.6 + 0.4 * Math.Sin(Engine.Clock * 4);
            var r = new Rect(168, 330, 400, 56);
            dc.FillRectangle(Brush(0xC0000000), r);
            Text(dc, "DEMO - PRESS ANY KEY", 368, 342, 34, Sprites.Shade(Gold, pulse), 1);
        }
    }

    // ───────────────────────────── playing ─────────────────────────────

    private void DrawPlay(DrawingContext dc)
    {
        var top = Engine.Top;
        Box(dc, View.Inflate(4), 0xFF000000);
        using (dc.PushClip(View))
        {
            if (top is { Kind: PanelKind.Gem }) DrawGem(dc, View);
            else if (top is { Kind: PanelKind.Shrine }) DrawShrine(dc, View);
            else if (top is { Kind: PanelKind.Codex }) DrawCodexArt(dc, View);
            else
            {
                switch (Engine.World.Where)
                {
                    case Where.World: DrawLand(dc); break;
                    case Where.Town: DrawTown(dc); break;
                    default: Draw3D(dc, View); break;
                }
            }
            if (Engine.Bumped > 0) dc.FillRectangle(Brush(0x50FF2020), View);
            if (Engine.Flash > 0) dc.FillRectangle(Brush((uint)(Math.Clamp(Engine.Flash, 0, 1) * 200) << 24 | 0x00FFFFFF), View);
        }
        DrawBar(dc);
        DrawRoster(dc, Engine.Battle);
        DrawStatus(dc);
        DrawMessages(dc, Engine.Log);
        if (top is not null && top.Kind != PanelKind.Gem) DrawPanelFor(dc, top);
        else if (top is { Kind: PanelKind.Gem }) DrawPanel(dc, new Rect(View.X + 200, View.Bottom - 70, 304, 60), top);
        if (Engine.Pending is { } pending && top is null)
            Shadowed(dc, pending == "talk" ? "Talk - which way?" : "Fire - which way?", View.X + View.Width / 2, View.Y + 12, 30, Gold, 1);
    }

    private void DrawBar(DrawingContext dc)
    {
        var w = Engine.World;
        string where = w.Where switch
        {
            Where.Town => Engine.Town?.Name ?? "",
            Where.Dungeon => $"{Dungeons.Get(w.Place).Name}, level {w.Level}, facing {w.Facing}",
            _ => w.Aboard ? $"At sea  {w.X},{w.Y}" : $"Merovale  {w.X},{w.Y}  {Terrain.Name(GameEngine.Land[w.X, w.Y])}",
        };
        Text(dc, where, Bar.X + 4, Bar.Y, 24, Ink);
        Text(dc, "H help   Esc menu", Bar.Right - 4, Bar.Y, 22, Dim, 2);
    }

    private void DrawRoster(DrawingContext dc, Combat.Battle? battle)
    {
        var r = Roster;
        Box(dc, r);
        var party = Engine.World.Party;
        var current = battle?.Current?.Pc;
        for (int i = 0; i < party.Count; i++)
        {
            var c = party[i];
            double y = r.Y + 8 + i * 40;
            if (c == current) dc.FillRectangle(Brush(0xFF2A2A48), new Rect(r.X + 5, y - 2, r.Width - 10, 40));
            Sprite(dc, ClassSprite(c.Class), MemberTint(i), r.X + 8, y + 2, 32, c.Alive ? 1 : 0.35);
            uint nameCol = !c.Alive ? Dim : Sprites.Shade(MemberTint(i), 1.3);
            Text(dc, c.Name, r.X + 46, y - 2, 22, nameCol);
            Text(dc, $"L{c.Level} {c.Title}", r.Right - 10, y - 2, 20, Dim, 2);
            double frac = c.MaxHp == 0 ? 0 : (double)Math.Max(0, c.Hp) / c.MaxHp;
            uint hpCol = !c.Alive ? Dim : c.Status == Rules.Status.Poisoned ? 0xFF80E040 : frac > 0.5 ? Good : frac > 0.25 ? Warn : Bad;
            string state = !c.Alive ? "dead" : c.Status == Rules.Status.Poisoned ? "poisoned" : "";
            Text(dc, $"HP {Math.Max(0, c.Hp)}/{c.MaxHp}" + (c.MaxMp > 0 ? $"  MP {c.Mp}" : "") + (state.Length > 0 ? "  " + state : ""), r.X + 46, y + 16, 20, hpCol);
        }
    }

    private void DrawStatus(DrawingContext dc)
    {
        var r = Status;
        Box(dc, r);
        var w = Engine.World;
        Text(dc, $"Gold {w.Gold}", r.X + 12, r.Y + 6, 24, Gold);
        Text(dc, $"Food {w.Food}", r.Right - 12, r.Y + 6, 24, w.Food < 30 ? Bad : Ink, 2);
        if (w.Where == Where.Dungeon) Text(dc, w.Light > 0 ? $"Light {w.Light}" : "Dark", r.X + 12, r.Y + 32, 22, w.Light > 0 ? Warn : Dim);
        else Text(dc, $"Turn {w.Turn}", r.X + 12, r.Y + 32, 22, Dim);
        Text(dc, $"Torches {w.Torches}", r.Right - 12, r.Y + 32, 22, Dim, 2);
        // The two moons, drawn in their phases.
        DrawMoon(dc, r.X + 28, r.Y + 78, 12, w.Selis, 0xFFE8E8F0);
        Text(dc, "Selis", r.X + 46, r.Y + 64, 20, Dim);
        DrawMoon(dc, r.X + 150, r.Y + 78, 12, w.Roan, 0xFFE07060);
        Text(dc, "Roan", r.X + 168, r.Y + 64, 20, Dim);
    }

    /// <summary>A moon in one of its eight phases (0 new, 4 full).</summary>
    private static void DrawMoon(DrawingContext dc, double cx, double cy, double r, int phase, uint colour)
    {
        dc.DrawEllipse(Brush(0xFF202030), null, new Point(cx, cy), r, r);
        if (phase == 0)
        {
            dc.DrawEllipse(null, Pen(Sprites.Shade(colour, 0.4)), new Point(cx, cy), r, r);
            return;
        }
        // Lit fraction 0..1..0 over the cycle; waxing lights the right side.
        double lit = phase <= 4 ? phase / 4.0 : (8 - phase) / 4.0;
        bool waxing = phase < 4;
        for (int i = 0; i < (int)(r * 2); i++)
        {
            double yy = -r + i + 0.5;
            double half = Math.Sqrt(Math.Max(0, r * r - yy * yy));
            double edge = half * (1 - 2 * lit);
            double x0 = waxing ? cx + edge : cx - half, x1 = waxing ? cx + half : cx - edge;
            if (phase == 4) { x0 = cx - half; x1 = cx + half; }
            if (x1 > x0) dc.FillRectangle(Brush(colour), new Rect(x0, cy + yy - 0.5, x1 - x0, 1.2));
        }
    }

    private void DrawMessages(DrawingContext dc, IReadOnlyList<string> log)
    {
        var r = Messages;
        Box(dc, r);
        const double size = 20, lh = 20;
        int room = (int)((r.Height - 16) / lh);
        var lines = new List<(string, bool)>();
        for (int i = log.Count - 1; i >= 0 && lines.Count < room; i--)
        {
            var wrapped = Wrap(log[i], r.Width - 20, size);
            for (int j = wrapped.Count - 1; j >= 0 && lines.Count < room; j--) lines.Add((wrapped[j], i == log.Count - 1));
        }
        lines.Reverse();
        for (int i = 0; i < lines.Count; i++) Text(dc, lines[i].Item1, r.X + 10, r.Y + 8 + i * lh, size, lines[i].Item2 ? Ink : Dim);
    }

    // ───────────────────────────── panels ─────────────────────────────

    private void DrawPanelFor(DrawingContext dc, Game.Panel p)
    {
        switch (p.Kind)
        {
            case PanelKind.Talk or PanelKind.Shop:
            {
                var r = new Rect(28, 372, 680, 340);
                Box(dc, r);
                if (p.Picture is { } pic)
                {
                    var face = new Rect(r.X + 14, r.Y + 14, 96, 96);
                    dc.FillRectangle(Brush(0xFF1E1E2C), face);
                    uint tint = Engine.Talking is { } n && p.Kind == PanelKind.Talk ? NpcTint(n) : p.Kind == PanelKind.Shop ? 0xFF2A7040 : p.PictureTint;
                    Sprite(dc, pic, tint, face.X + 8, face.Y + 8, 80);
                }
                DrawPanel(dc, new Rect(r.X + 120, r.Y, r.Width - 120, r.Height), p, frame: false);
                return;
            }
            case PanelKind.Big or PanelKind.Gem:
                DrawPanel(dc, View, p);
                return;
            case PanelKind.Sheet:
                if (p.Who is { } who) DrawSheet(dc, View, p, who);
                else DrawVirtues(dc, View, p);
                return;
            case PanelKind.Shrine:
                DrawPanel(dc, new Rect(28, 452, 680, 260), p);
                return;
            case PanelKind.Codex:
                DrawPanel(dc, new Rect(28, 452, 680, 260), p);
                return;
            default:
                if (p.Picture is { } picture)
                {
                    var r = new Rect(28, 400, 680, 312);
                    Box(dc, r);
                    Sprite(dc, picture, p.PictureTint, r.X + 22, r.Y + 22, 80);
                    DrawPanel(dc, new Rect(r.X + 120, r.Y, r.Width - 120, r.Height), p, frame: false);
                    return;
                }
                DrawPanel(dc, new Rect(48, 420, 640, 292), p);
                return;
        }
    }

    /// <summary>A panel: title, wrapped paragraphs, a typing line or the options (scrolled to keep the choice in view), and the chosen option's note.</summary>
    private void DrawPanel(DrawingContext dc, Rect r, Game.Panel p, bool frame = true)
    {
        if (frame) Box(dc, r);
        double x = r.X + 18, width = r.Width - 36, y = r.Y + 10;
        const double size = 22, lh = 23;
        if (p.Title.Length > 0)
        {
            Text(dc, p.Title, x, y, 28, Gold);
            y += 34;
        }
        double textBottom = p.Entry is not null ? r.Bottom - 48 : r.Bottom - lh * Math.Min(Math.Max(1, p.Options.Count), 6) - 14;
        // The text: for a conversation, the newest lines (scrolled up as it grows); otherwise from the top.
        var all = p.Text.SelectMany(t => Wrap(t, width, size).Select((l, i) => (Line: l, First: i == 0))).ToList();
        int fit = Math.Max(1, (int)((textBottom - y) / lh));
        var shown = p.Kind == PanelKind.Talk ? all.Skip(Math.Max(0, all.Count - fit)) : all.Take(fit);
        foreach (var (line, _) in shown)
        {
            uint col = line.StartsWith('>') ? Blue : Ink;
            Text(dc, line, x, y, size, col);
            y += lh;
        }
        if (p.Entry is not null)
        {
            double ey = r.Bottom - 42;
            dc.FillRectangle(Brush(0xFF060610), new Rect(x - 4, ey, width + 8, 32));
            string cursor = Engine.Clock % 1 < 0.5 ? "_" : " ";
            Text(dc, $"{p.Prompt} {p.Entry}{cursor}", x + 4, ey + 2, 26, Gold);
            return;
        }
        y += 6;
        string? note = p.Options.Count > 0 && p.Selected < p.Options.Count ? p.Options[p.Selected].Note : null;
        double noteSpace = note is null ? 0 : lh * Math.Min(2, Wrap(note, width, 20).Count) + 6;
        int room = Math.Max(1, (int)((r.Bottom - 10 - noteSpace - y) / lh));
        int first = Math.Clamp(p.Selected - room / 2, 0, Math.Max(0, p.Options.Count - room));
        for (int i = first; i < Math.Min(p.Options.Count, first + room); i++)
        {
            var o = p.Options[i];
            bool sel = i == p.Selected;
            if (sel) dc.FillRectangle(Brush(0xFF2A2A48), new Rect(x - 6, y, width + 12, lh));
            uint col = !o.Enabled ? 0xFF56566A : sel ? Gold : o.Act is null ? Dim : Ink;
            string num = o.Act is null ? "" : i < 9 ? $"{i + 1}. " : "   ";
            Text(dc, num + o.Label, x, y, size, col);
            y += lh;
        }
        if (first > 0) Text(dc, "more above", r.Right - 18, r.Y + 12, 20, Dim, 2);
        if (first + room < p.Options.Count) Text(dc, "more below", r.Right - 18, r.Bottom - 24, 20, Dim, 2);
        if (note is not null)
        {
            double ny = r.Bottom - noteSpace - 4;
            foreach (var line in Wrap(note, width, 20).Take(2))
            {
                Text(dc, line, x, ny, 20, Blue);
                ny += lh;
            }
        }
    }

    // ───────────────────────────── the character sheet and the virtues ─────────────────────────────

    private void DrawSheet(DrawingContext dc, Rect r, Game.Panel p, Character c)
    {
        Box(dc, r);
        int slot = Math.Max(0, Engine.World.Party.IndexOf(c));
        uint tint = MemberTint(slot);
        double x = r.X + 24, y = r.Y + 18;
        Sprite(dc, ClassSprite(c.Class), tint, r.Right - 180, r.Y + 24, 144);
        Text(dc, c.Name, x, y, 44, Sprites.Shade(tint, 1.35));
        Text(dc, $"Level {c.Level} {c.Title}", x, y + 46, 28, Ink);
        string next = c.Level >= Character.MaxLevel ? "highest level" : $"next level at {Character.XpFor(c.Level + 1)} (the Queen raises you)";
        Text(dc, $"Experience {c.Xp}, {next}", x, y + 78, 22, Dim);
        y += 130;
        Text(dc, $"Strength      {c.Str}", x, y, 26, Ink);
        Text(dc, $"Dexterity     {c.Dex}", x, y + 30, 26, Ink);
        Text(dc, $"Intelligence  {c.Int}", x, y + 60, 26, Ink);
        y += 110;
        Text(dc, $"Hit points  {Math.Max(0, c.Hp)} / {c.MaxHp}", x, y, 26, c.Alive ? Good : Bad);
        Text(dc, $"Mana        {c.Mp} / {c.MaxMp}", x, y + 30, 26, Blue);
        Text(dc, $"Condition   {(c.Alive ? c.Status == Rules.Status.Poisoned ? "poisoned" : "good" : "dead")}", x, y + 60, 26, Ink);
        y += 110;
        var wpn = c.WeaponItem;
        Text(dc, $"Weapon  {wpn.Name}  ({wpn.Damage}{(wpn.Range > 1 ? $", range {wpn.Range}" : wpn.Range == 1 ? ", reach 2" : "")})", x, y, 24, Ink);
        Text(dc, $"Armour  {c.ArmourItem.Name}  (defence {c.Defence})", x, y + 28, 24, Ink);
        y += 70;
        foreach (var l in Wrap(ClassInfo.Get(c.Class).Blurb, r.Width - 48, 22))
        {
            Text(dc, l, x, y, 22, Dim);
            y += 24;
        }
        if (Engine.World.Party.Count > 1) Text(dc, "Left/Right: next companion", r.Right - 20, r.Bottom - 36, 22, Dim, 2);
        DrawPanel(dc, new Rect(r.X + 16, r.Bottom - 110, 300, 96), new Game.Panel().Also(q =>
        {
            foreach (var o in p.Options) q.Options.Add(o);
            q.Selected = p.Selected;
        }));
    }

    private void DrawVirtues(DrawingContext dc, Rect r, Game.Panel p)
    {
        Box(dc, r);
        var w = Engine.World;
        Text(dc, "The eight virtues", r.X + 24, r.Y + 16, 36, Gold);
        double y = r.Y + 70;
        foreach (var v in Virtues.All)
        {
            int val = w[v.Virtue];
            dc.DrawEllipse(Brush(v.StoneColour), Pen(0xFF000000), new Point(r.X + 40, y + 14), 10, 10);
            Text(dc, v.Virtue.ToString(), r.X + 60, y, 28, Ink);
            var bar = new Rect(r.X + 240, y + 8, 300, 14);
            dc.FillRectangle(Brush(0xFF202030), bar);
            dc.FillRectangle(Brush(val >= Virtues.Steadfast ? Gold : Sprites.Shade(v.StoneColour, 1.2)), new Rect(bar.X, bar.Y, bar.Width * val / Virtues.Max, bar.Height));
            dc.DrawLine(Pen(0xFFFFFFFF), new Point(bar.X + bar.Width * Virtues.Steadfast / Virtues.Max, bar.Y - 3), new Point(bar.X + bar.Width * Virtues.Steadfast / Virtues.Max, bar.Bottom + 3));
            string tag = w.Elevated.Contains(v.Virtue) ? "RAISED" : Virtues.Standing(val);
            Text(dc, tag, r.Right - 24, y, 24, w.Elevated.Contains(v.Virtue) ? Gold : Dim, 2);
            string marks = (w.Runes.Contains(v.Virtue) ? "rune " : "") + (w.Stones.Contains(v.Virtue) ? "stone" : "");
            Text(dc, marks, r.X + 60, y + 26, 20, Blue);
            y += 66;
        }
        Text(dc, "The white mark is steadfast: enough to be raised at the virtue's shrine.", r.X + 24, r.Bottom - 70, 22, Dim);
        DrawPanel(dc, new Rect(r.Right - 220, r.Bottom - 50, 200, 40), new Game.Panel().Also(q =>
        {
            foreach (var o in p.Options) q.Options.Add(o);
        }), frame: false);
    }
}

internal static class PanelExtensions
{
    public static T Also<T>(this T value, Action<T> act)
    {
        act(value);
        return value;
    }
}
