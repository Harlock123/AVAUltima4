using Avalonia;
using Avalonia.Media;
using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Views;

public sealed partial class GameCanvas
{
    private static void Quad(DrawingContext dc, Point a, Point b, Point c, Point d, uint fill, uint? edge = null)
    {
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(a, true);
            ctx.LineTo(b);
            ctx.LineTo(c);
            ctx.LineTo(d);
            ctx.EndFigure(true);
        }
        dc.DrawGeometry(Brush(fill), edge is { } e ? Pen(e) : null, g);
    }

    private static Point Lerp(Point a, Point b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    /// <summary>
    /// The view down a dungeon passage: the squares ahead and to each side, painted far to near, with
    /// whatever stands on the floor - ladders, altars, fountains, chests and seals. With no light, only
    /// the nearest walls show, faintly.
    /// </summary>
    private void Draw3D(DrawingContext dc, Rect v)
    {
        var w = Engine.World;
        var lvl = Engine.Dungeon;
        if (lvl is null) return;
        var def = Dungeons.Get(w.Place);
        bool lit = w.Light > 0;
        int depth = lit ? 5 : 1;
        double dim = lit ? 1 : 0.4;
        double cx = v.X + v.Width / 2, cy = v.Y + v.Height / 2;
        const double focal = 300;
        double HW(double b) => focal / (b + 1);
        double HH(double b) => HW(b) * 0.92;
        uint wall = Sprites.Parse(def.Wall), floor = Sprites.Parse(def.Floor);

        dc.FillRectangle(Brush(0xFF000000), v);
        for (int i = 0; i < 16; i++)
        {
            double t0 = i / 16.0;
            double fade = lit ? 0.25 + 0.75 * t0 : 0.1 + 0.3 * t0;
            dc.FillRectangle(Brush(Sprites.Shade(Sprites.Shade(floor, 0.7), fade)), new Rect(v.X, v.Y + (cy - v.Y) * (1 - t0) - (cy - v.Y) / 16, v.Width, (cy - v.Y) / 16 + 1));
            dc.FillRectangle(Brush(Sprites.Shade(floor, fade)), new Rect(v.X, cy + (v.Bottom - cy) * t0, v.Width, (v.Bottom - cy) / 16 + 1));
        }

        var f = w.Facing.Step();
        var rt = w.Facing.Right().Step();
        int[] laterals = [-2, 2, -1, 1, 0];
        for (int k = depth; k >= 0; k--)
        {
            double nearB = k == 0 ? -0.6 : k - 1, farB = k;
            double shade = Math.Max(0.18, 1 - 0.16 * k) * dim;
            // What stands on the floor at this depth.
            foreach (int l in laterals)
            {
                int mx = w.X + k * f.X + l * rt.X, my = w.Y + k * f.Y + l * rt.Y;
                var cell = lvl[mx, my];
                if (cell == Cell.Wall || k == 0) continue;
                double mid = k - 0.5;
                double ox = cx + 2 * l * HW(mid), fy = cy + HH(mid), size = HW(mid);
                DrawThing(dc, cell, mx, my, ox, fy, cy - HH(mid), size, shade);
            }
            // The walls at this depth: side faces, then front faces.
            for (int pass = 0; pass < 2; pass++)
            foreach (int l in laterals)
            {
                int mx = w.X + k * f.X + l * rt.X, my = w.Y + k * f.Y + l * rt.Y;
                if (lvl[mx, my] != Cell.Wall) continue;
                if (pass == 0 && l != 0)
                {
                    double edge = l < 0 ? 2 * l + 1 : 2 * l - 1;
                    double x1 = cx + edge * HW(nearB), x2 = cx + edge * HW(farB);
                    var nt = new Point(x1, cy - HH(nearB));
                    var nb = new Point(x1, cy + HH(nearB));
                    var ft = new Point(x2, cy - HH(farB));
                    var fb = new Point(x2, cy + HH(farB));
                    uint col = Sprites.Shade(wall, shade * 0.72);
                    Quad(dc, nt, ft, fb, nb, col, Sprites.Shade(wall, shade * 0.4));
                    for (int i = 1; i < 4; i++)
                        dc.DrawLine(Pen(Sprites.Shade(wall, shade * 0.45)), Lerp(nt, nb, i / 4.0), Lerp(ft, fb, i / 4.0));
                }
                if (pass == 1 && k >= 1)
                {
                    double x1 = cx + (2 * l - 1) * HW(nearB), x2 = cx + (2 * l + 1) * HW(nearB);
                    double top = cy - HH(nearB), bot = cy + HH(nearB);
                    var r = new Rect(x1, top, x2 - x1, bot - top);
                    dc.FillRectangle(Brush(Sprites.Shade(wall, shade)), r);
                    uint line = Sprites.Shade(wall, shade * 0.5);
                    // Courses of stone.
                    for (int i = 1; i < 6; i++)
                    {
                        double yy = top + (bot - top) * i / 6;
                        dc.DrawLine(Pen(line), new Point(x1, yy), new Point(x2, yy));
                        for (int j = 0; j < 3; j++)
                        {
                            double xx = x1 + (x2 - x1) * (j + (i % 2 == 0 ? 0.5 : 0.25)) / 3;
                            dc.DrawLine(Pen(line), new Point(xx, yy - (bot - top) / 6), new Point(xx, yy));
                        }
                    }
                    dc.DrawRectangle(Pen(Sprites.Shade(wall, shade * 0.35), 1.5), r);
                }
            }
        }
        // What's underfoot.
        var here = lvl[w.X, w.Y];
        string? underfoot = here switch
        {
            Cell.Up => w.Level == 1 ? "A ladder up to the daylight (Enter)" : "A ladder up (Enter)",
            Cell.Down => "A ladder down (Enter)",
            Cell.Seal when w.Has($"seal:{w.Level}") => w.Level == Dungeons.Levels ? "The Codex chamber" : "The open seal: a way down (Enter)",
            Cell.Seal => "A sealed way (Enter to look)",
            Cell.Altar => "An altar",
            _ => null,
        };
        if (underfoot is not null) Shadowed(dc, underfoot, cx, v.Y + 14, 26, 0xFF80F090, 1);
        if (!lit) Shadowed(dc, "Darkness. L lights a torch.", cx, v.Bottom - 40, 24, Dim, 1);
    }

    /// <summary>A ladder, altar, fountain, chest or seal standing on a square of floor.</summary>
    private void DrawThing(DrawingContext dc, Cell cell, int mx, int my, double x, double floorY, double ceilY, double s, double shade)
    {
        var w = Engine.World;
        uint wood = Sprites.Shade(0xFF8A5A30, shade), dark = Sprites.Shade(0xFF101018, 1);
        switch (cell)
        {
            case Cell.Up:
            {
                dc.DrawEllipse(Brush(Sprites.Shade(0xFF000000, 1)), null, new Point(x, ceilY + s * 0.05), s * 0.5, s * 0.12);
                DrawLadder(dc, x, ceilY, floorY, s, wood);
                break;
            }
            case Cell.Down:
                dc.DrawEllipse(Brush(dark), null, new Point(x, floorY - s * 0.05), s * 0.55, s * 0.14);
                DrawLadder(dc, x, floorY - s * 0.5, floorY - s * 0.05, s, wood);
                break;
            case Cell.Altar:
            {
                var r = new Rect(x - s * 0.4, floorY - s * 0.55, s * 0.8, s * 0.55);
                dc.FillRectangle(Brush(Sprites.Shade(0xFFB0ACA4, shade)), r);
                dc.FillRectangle(Brush(Sprites.Shade(0xFFD8D4CC, shade)), new Rect(r.X - s * 0.05, r.Y - s * 0.08, r.Width + s * 0.1, s * 0.1));
                var def = Dungeons.Get(w.Place);
                if (def.Stones.TryGetValue(w.Level, out var v) && !w.Stones.Contains(v))
                {
                    uint col = Virtues.Get(v).StoneColour;
                    double glow = 0.6 + 0.4 * Math.Sin(Engine.Clock * 3);
                    dc.DrawEllipse(Brush((uint)(glow * 90) << 24 | (col & 0x00FFFFFF)), null, new Point(x, r.Y - s * 0.2), s * 0.32, s * 0.32);
                    dc.DrawEllipse(Brush(col), Pen(0xFF000000), new Point(x, r.Y - s * 0.17), s * 0.13, s * 0.11);
                }
                break;
            }
            case Cell.Fountain:
            {
                dc.DrawEllipse(Brush(Sprites.Shade(0xFF9A968E, shade)), null, new Point(x, floorY - s * 0.12), s * 0.55, s * 0.18);
                dc.DrawEllipse(Brush(Sprites.Shade(0xFF3A78C8, shade)), null, new Point(x, floorY - s * 0.15), s * 0.45, s * 0.12);
                dc.FillRectangle(Brush(Sprites.Shade(0xFF9A968E, shade)), new Rect(x - s * 0.05, floorY - s * 0.6, s * 0.1, s * 0.45));
                double jet = Engine.Clock * 4 % 1;
                dc.DrawEllipse(Brush(Sprites.Shade(0xFF8AC0F0, shade)), null, new Point(x, floorY - s * (0.6 + 0.1 * jet)), s * 0.08, s * 0.05);
                break;
            }
            case Cell.Chest when !w.Has($"chest:{w.Place}:{w.Level}:{mx},{my}"):
            {
                var r = new Rect(x - s * 0.3, floorY - s * 0.32, s * 0.6, s * 0.3);
                dc.FillRectangle(Brush(wood), r);
                dc.FillRectangle(Brush(Sprites.Shade(0xFFF0D040, shade)), new Rect(r.X, r.Y + r.Height * 0.3, r.Width, r.Height * 0.12));
                dc.DrawRectangle(Pen(Sprites.Shade(0xFF4A3020, shade), 1.5), r);
                break;
            }
            case Cell.Seal:
            {
                if (w.Has($"seal:{w.Level}"))
                {
                    if (w.Level == Dungeons.Levels)
                    {
                        // The Codex on its stand.
                        dc.FillRectangle(Brush(Sprites.Shade(0xFFE8E4DC, shade)), new Rect(x - s * 0.08, floorY - s * 0.5, s * 0.16, s * 0.5));
                        double glow = 0.6 + 0.4 * Math.Sin(Engine.Clock * 2);
                        dc.DrawEllipse(Brush((uint)(glow * 120) << 24 | 0x00FFF0C0), null, new Point(x, floorY - s * 0.6), s * 0.45, s * 0.3);
                        Quad(dc, new Point(x - s * 0.35, floorY - s * 0.58), new Point(x, floorY - s * 0.5), new Point(x, floorY - s * 0.62), new Point(x - s * 0.33, floorY - s * 0.7), Sprites.Shade(0xFFF8F4E8, shade));
                        Quad(dc, new Point(x + s * 0.35, floorY - s * 0.58), new Point(x, floorY - s * 0.5), new Point(x, floorY - s * 0.62), new Point(x + s * 0.33, floorY - s * 0.7), Sprites.Shade(0xFFF0ECE0, shade));
                    }
                    else dc.DrawEllipse(Brush(dark), null, new Point(x, floorY - s * 0.05), s * 0.55, s * 0.14);
                    break;
                }
                var (a, b) = Dungeons.SealStones(w.Level);
                var c = new Point(x, floorY - s * 0.55);
                dc.DrawEllipse(Brush(Sprites.Shade(0xFF1A1622, 1)), Pen(Sprites.Shade(0xFF6A5A8A, shade), 2), c, s * 0.55, s * 0.55);
                dc.DrawEllipse(Brush(Sprites.Shade(Virtues.Get(a).StoneColour, shade * 0.8)), null, new Point(c.X - s * 0.2, c.Y), s * 0.1, s * 0.1);
                dc.DrawEllipse(Brush(Sprites.Shade(Virtues.Get(b).StoneColour, shade * 0.8)), null, new Point(c.X + s * 0.2, c.Y), s * 0.1, s * 0.1);
                break;
            }
        }
    }

    private static void DrawLadder(DrawingContext dc, double x, double top, double bottom, double s, uint wood)
    {
        double half = s * 0.18;
        var pen = Pen(wood, Math.Max(1.5, s * 0.04));
        dc.DrawLine(pen, new Point(x - half, top), new Point(x - half, bottom));
        dc.DrawLine(pen, new Point(x + half, top), new Point(x + half, bottom));
        for (double y = top + (bottom - top) / 8; y < bottom; y += (bottom - top) / 6)
            dc.DrawLine(pen, new Point(x - half, y), new Point(x + half, y));
    }
}
