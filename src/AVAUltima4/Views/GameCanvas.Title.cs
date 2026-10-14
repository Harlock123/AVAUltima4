using Avalonia;
using Avalonia.Media;
using AVAUltima4.Game;
using AVAUltima4.Rules;

namespace AVAUltima4.Views;

public sealed partial class GameCanvas
{
    private void DrawTitle(DrawingContext dc)
    {
        var top = Engine.Top;
        bool seer = top is { Kind: PanelKind.Seer } || top?.Title is "The Seer" or "The Crossroads";
        if (seer)
        {
            DrawSeer(dc, top!);
            return;
        }
        DrawNight(dc, dawn: false);
        if (top is null) return;
        if (Engine.Panels.Count == 1 && top.Title == "AVAUltima4")
        {
            var menu = new Game.Panel().Also(p =>
            {
                foreach (var o in top.Options) p.Options.Add(o);
                p.Selected = top.Selected;
            });
            DrawPanel(dc, new Rect(352, 430, 320, 130), menu);
            Text(dc, "Arrows and Enter to choose.", 512, 690, 22, 0xFF8A8AA0, 1);
            Text(dc, "An original quest in the spirit of the classic virtue RPGs. Every place, person and word here is new.", 512, 716, 20, 0xFF6A6A80, 1);
            return;
        }
        DrawPanel(dc, top.Kind == PanelKind.Big ? new Rect(112, 120, 800, 620) : new Rect(162, 380, 700, 340), top);
    }

    /// <summary>The title backdrop: a night over Merovale with both moons up, the castle on its hill.</summary>
    private void DrawNight(DrawingContext dc, bool dawn, bool title = true)
    {
        uint top = dawn ? 0xFF4A3A6A : 0xFF05060E, mid = dawn ? 0xFFE09060 : 0xFF141838, low = dawn ? 0xFFF4C890 : 0xFF223050;
        for (int i = 0; i < 48; i++)
        {
            double t = i / 47.0;
            uint c = t < 0.6 ? Sprites.Mix(top, mid, t / 0.6) : Sprites.Mix(mid, low, (t - 0.6) / 0.4);
            dc.FillRectangle(Brush(c), new Rect(0, i * 10, ScreenWidth, 11));
        }
        var rng = new Random(44);
        if (!dawn)
            for (int i = 0; i < 140; i++)
            {
                double sx = rng.NextDouble() * ScreenWidth, sy = rng.NextDouble() * 360;
                double tw = 0.5 + 0.5 * Math.Sin(Engine.Clock * (1 + i % 5) + i);
                dc.FillRectangle(Brush(Sprites.Mix(0xFF303050, 0xFFFFFFFF, tw)), new Rect(sx, sy, 2, 2));
            }
        if (dawn) dc.DrawEllipse(Brush(0xFFFFE8A0), null, new Point(512, 470), 70, 70);
        else
        {
            DrawMoon(dc, 820, 110, 46, 3, 0xFFE8E8F0);
            DrawMoon(dc, 700, 70, 22, 5, 0xFFE07060);
        }
        // Hills, the castle on the middle one, and the crossroads' signpost in front.
        uint hill = dawn ? 0xFF2A2440 : 0xFF080A14, near = dawn ? 0xFF1A1830 : 0xFF04050A;
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(new Point(0, 560), true);
            ctx.QuadraticBezierTo(new Point(260, 430), new Point(512, 470));
            ctx.QuadraticBezierTo(new Point(760, 420), new Point(1024, 540));
            ctx.LineTo(new Point(1024, 768));
            ctx.LineTo(new Point(0, 768));
            ctx.EndFigure(true);
        }
        dc.DrawGeometry(Brush(hill), null, g);
        uint stone = dawn ? 0xFF3A3050 : 0xFF0C0E1A;
        dc.FillRectangle(Brush(stone), new Rect(452, 380, 120, 100));
        dc.FillRectangle(Brush(stone), new Rect(436, 350, 30, 130));
        dc.FillRectangle(Brush(stone), new Rect(558, 350, 30, 130));
        dc.FillRectangle(Brush(stone), new Rect(495, 330, 34, 60));
        for (int i = 0; i < 3; i++)
        {
            dc.FillRectangle(Brush(stone), new Rect(436 + i * 11, 340, 7, 12));
            dc.FillRectangle(Brush(stone), new Rect(558 + i * 11, 340, 7, 12));
        }
        dc.FillRectangle(Brush(dawn ? 0xFF6A5070 : 0xFFFFE890), new Rect(506, 350, 10, 16));
        dc.FillRectangle(Brush(dawn ? 0xFF6A5070 : 0xFFFFE890), new Rect(470, 420, 8, 12));
        dc.DrawLine(Pen(stone, 2), new Point(512, 330), new Point(512, 300));
        Quad(dc, new Point(512, 300), new Point(538, 306), new Point(512, 312), new Point(512, 300), dawn ? 0xFF8A3040 : 0xFF6A2030);
        var road = new StreamGeometry();
        using (var ctx = road.Open())
        {
            ctx.BeginFigure(new Point(470, 768), true);
            ctx.QuadraticBezierTo(new Point(500, 600), new Point(505, 480));
            ctx.LineTo(new Point(519, 480));
            ctx.QuadraticBezierTo(new Point(540, 600), new Point(600, 768));
            ctx.EndFigure(true);
        }
        dc.DrawGeometry(Brush(dawn ? 0xFF4A3A40 : 0xFF0E1018), null, road);
        dc.FillRectangle(Brush(near), new Rect(250, 590, 8, 120));
        Quad(dc, new Point(200, 600), new Point(300, 604), new Point(310, 614), new Point(200, 618), near);
        Quad(dc, new Point(310, 632), new Point(214, 628), new Point(204, 638), new Point(310, 646), near);
        if (!title) return;
        Shadowed(dc, "AVAULTIMA4", 512, 120, 112, dawn ? 0xFFFFF0C0 : 0xFFFFD860, 1);
        Shadowed(dc, "The Quest of the Exemplar", 512, 232, 40, dawn ? 0xFF4A2A40 : 0xFFB0C0E8, 1);
    }

    /// <summary>The seer's tent: an awning in the rain, a lamp, and two painted stones on the table.</summary>
    private void DrawSeer(DrawingContext dc, Game.Panel top)
    {
        DrawNight(dc, dawn: false, title: false);
        dc.FillRectangle(Brush(0xA0000000), new Rect(0, 0, ScreenWidth, ScreenHeight));
        // Rain.
        var rng = new Random(9);
        for (int i = 0; i < 90; i++)
        {
            double x = rng.NextDouble() * ScreenWidth, y = (rng.NextDouble() * ScreenHeight + Engine.Clock * 500 * (0.8 + rng.NextDouble() * 0.4)) % ScreenHeight;
            dc.DrawLine(Pen(0x506080A0), new Point(x, y), new Point(x - 4, y + 16));
        }
        // The awning and the table.
        Quad(dc, new Point(140, 60), new Point(884, 60), new Point(944, 150), new Point(80, 150), 0xFF5A2A30);
        for (int i = 0; i < 12; i++)
            Quad(dc, new Point(80 + i * 72, 150), new Point(152 + i * 72, 150), new Point(116 + i * 72, 176), new Point(116 + i * 72, 176), i % 2 == 0 ? 0xFF7A3A40u : 0xFFB89048u);
        dc.FillRectangle(Brush(0xFF3A2418), new Rect(180, 330, 664, 30));
        dc.FillRectangle(Brush(0xFF2A1A10), new Rect(200, 360, 20, 40));
        dc.FillRectangle(Brush(0xFF2A1A10), new Rect(804, 360, 20, 40));
        // The seer herself, and her lamp.
        Sprite(dc, "sage", 0xFF6A3A7A, 448, 170, 128);
        double glow = 0.75 + 0.25 * Math.Sin(Engine.Clock * 3);
        dc.DrawEllipse(Brush((uint)(glow * 70) << 24 | 0x00FFC060), null, new Point(760, 300), 90, 70);
        dc.FillRectangle(Brush(0xFFFFD070), new Rect(752, 288, 16, 24));
        dc.FillRectangle(Brush(0xFF6A5A3A), new Rect(748, 312, 24, 18));
        if (Engine.SeerQuestion is { } q && top.Kind == PanelKind.Seer)
        {
            DrawStone(dc, 300, 310, q.A);
            DrawStone(dc, 724 - 140, 310, q.B);
        }
        DrawPanel(dc, new Rect(112, 420, 800, top.Entry is not null ? 220 : 280), top);
    }

    private static void DrawStone(DrawingContext dc, double x, double y, Virtue v)
    {
        var info = Virtues.Get(v);
        dc.DrawEllipse(Brush(info.StoneColour), Pen(0xFF000000, 2), new Point(x + 70, y), 60, 26);
        dc.DrawEllipse(Brush(Sprites.Shade(info.StoneColour, 1.4)), null, new Point(x + 52, y - 8), 20, 7);
        Shadowed(dc, v.ToString(), x + 70, y - 64, 30, Sprites.Shade(info.StoneColour, 1.3), 1);
    }

    /// <summary>Inside a shrine: an altar of the virtue's stone, and a flame above it.</summary>
    private void DrawShrine(DrawingContext dc, Rect r)
    {
        var v = Engine.AtShrine ?? Virtue.Reverence;
        var info = Virtues.Get(v);
        for (int i = 0; i < 40; i++)
            dc.FillRectangle(Brush(Sprites.Mix(0xFF06060C, Sprites.Shade(info.StoneColour, 0.3), i / 40.0)), new Rect(r.X, r.Y + i * r.Height / 40, r.Width, r.Height / 40 + 1));
        double cx = r.X + r.Width / 2;
        // Pillars.
        foreach (double px in new[] { r.X + 90, r.Right - 130 })
        {
            dc.FillRectangle(Brush(0xFF2A2834), new Rect(px, r.Y + 40, 40, 380));
            dc.FillRectangle(Brush(0xFF3A3844), new Rect(px, r.Y + 40, 12, 380));
        }
        // The altar.
        dc.FillRectangle(Brush(Sprites.Shade(info.StoneColour, 0.6)), new Rect(cx - 110, r.Y + 300, 220, 120));
        dc.FillRectangle(Brush(Sprites.Shade(info.StoneColour, 0.85)), new Rect(cx - 124, r.Y + 286, 248, 20));
        // The flame, which leaps when you are raised.
        double h = 90 + 20 * Math.Sin(Engine.Clock * 7) + (Engine.Flash > 0 ? 160 * Engine.Flash : 0);
        for (int i = 5; i >= 0; i--)
        {
            uint col = i switch { 0 => 0xFFFFFFE0, 1 => 0xFFFFF080, 2 => 0xFFFFC040, 3 => 0xFFFF8020, _ => 0x60FF6020 };
            double w = 20 + i * 12;
            var g = new StreamGeometry();
            using (var ctx = g.Open())
            {
                ctx.BeginFigure(new Point(cx - w / 2, r.Y + 286), true);
                ctx.QuadraticBezierTo(new Point(cx - w / 2, r.Y + 286 - h * 0.5), new Point(cx + Math.Sin(Engine.Clock * 5) * 6, r.Y + 286 - h * (1 + i * 0.08)));
                ctx.QuadraticBezierTo(new Point(cx + w / 2, r.Y + 286 - h * 0.5), new Point(cx + w / 2, r.Y + 286));
                ctx.EndFigure(true);
            }
            dc.DrawGeometry(Brush(col), null, g);
        }
        Shadowed(dc, $"The Shrine of {v}", cx, r.Y + 16, 40, Sprites.Shade(info.StoneColour, 1.4), 1);
    }

    private void DrawCodexArt(DrawingContext dc, Rect r)
    {
        dc.FillRectangle(Brush(0xFF04040A), r);
        double cx = r.X + r.Width / 2, cy = r.Y + 220;
        double glow = 0.6 + 0.4 * Math.Sin(Engine.Clock * 2);
        for (int i = 6; i >= 1; i--)
            dc.DrawEllipse(Brush((uint)(glow * 25 * (7 - i)) << 24 | 0x00FFF0C0), null, new Point(cx, cy), 60 * i, 40 * i);
        Quad(dc, new Point(cx - 190, cy + 30), new Point(cx, cy + 60), new Point(cx, cy - 40), new Point(cx - 180, cy - 70), 0xFFF8F4E8, 0xFF8A846A);
        Quad(dc, new Point(cx + 190, cy + 30), new Point(cx, cy + 60), new Point(cx, cy - 40), new Point(cx + 180, cy - 70), 0xFFF0ECE0, 0xFF8A846A);
        dc.FillRectangle(Brush(0xFFE8E4DC), new Rect(cx - 30, cy + 60, 60, 160));
        Shadowed(dc, "The Codex of Echoes", cx, r.Y + 20, 40, Gold, 1);
        if (Engine.CodexStep > 0) Text(dc, $"{Engine.CodexStep} answered", cx, r.Y + 400, 24, Dim, 1);
    }

    private void DrawEnding(DrawingContext dc)
    {
        DrawNight(dc, dawn: true);
        if (Engine.Top is { } p) DrawPanel(dc, new Rect(112, 330, 800, 250), p);
    }
}
