using Avalonia;
using Avalonia.Media;
using AVAUltima4.Combat;
using AVAUltima4.Game;
using AVAUltima4.Rules;
using Field = AVAUltima4.Combat.Ground;

namespace AVAUltima4.Views;

public sealed partial class GameCanvas
{
    private uint TintOf(Fighter f) =>
        f.Pc is { } pc ? MemberTint(Math.Max(0, Engine.World.Party.IndexOf(pc))) : Sprites.Parse(f.Kind!.Tint);

    private void DrawBattleScreen(DrawingContext dc)
    {
        var b = Engine.Battle;
        if (b is null) return;
        Box(dc, View.Inflate(4), 0xFF000000);
        string baseGround = b.Ground switch
        {
            Field.Swamp => "swamp",
            Field.Sand => "sand",
            Field.Dungeon => "path",
            Field.Sea => "floor",
            _ => "grass",
        };
        for (int y = 0; y < Battle.H; y++)
        for (int x = 0; x < Battle.W; x++)
        {
            double px = View.X + x * T, py = View.Y + y * T;
            if (b.IsWater(x, y)) Image(dc, Sprites.GroundBitmap("water", (WaveFrame + x) % 8), px, py, T);
            else
            {
                Image(dc, Sprites.GroundBitmap(baseGround), px, py, T);
                if (b.Ground == Field.Dungeon) dc.FillRectangle(Brush(0x60000000), new Rect(px, py, T, T));
            }
            if (!b.IsObstacle(x, y)) continue;
            switch (b.Ground)
            {
                case Field.Dungeon:
                    dc.FillRectangle(Brush(0xFF6A6474), new Rect(px + 12, py + 4, T - 24, T - 8));
                    dc.FillRectangle(Brush(0xFF8A8494), new Rect(px + 12, py + 4, 10, T - 8));
                    dc.FillRectangle(Brush(0xFF4A4454), new Rect(px + 6, py + T - 12, T - 12, 8));
                    break;
                case Field.Hills or Field.Sand:
                    dc.DrawEllipse(Brush(0xFF7A766E), Pen(0xFF4A4642, 2), new Point(px + T / 2, py + T * 0.6), T * 0.38, T * 0.3);
                    dc.DrawEllipse(Brush(0xFF9A968E), null, new Point(px + T * 0.4, py + T * 0.5), T * 0.14, T * 0.1);
                    break;
                default:
                    Sprite(dc, "tree", 0xFFFFFFFF, px, py, T);
                    break;
            }
        }

        var cur = b.Current;
        foreach (var f in b.All)
        {
            if (f.Gone) continue;
            double x = View.X + f.X * T, y = View.Y + f.Y * T;
            if (f == cur)
            {
                double pulse = 0.5 + 0.5 * Math.Sin(Engine.Clock * 8);
                dc.DrawRectangle(Pen(Sprites.Shade(Gold, 0.6 + 0.4 * pulse), 3), new Rect(x + 1.5, y + 1.5, T - 3, T - 3));
            }
            Sprite(dc, f.Sprite, TintOf(f), x, y, T, f.Asleep > 0 ? 0.6 : 1);
            double frac = Math.Clamp((double)f.Hp / Math.Max(1, f.MaxHp), 0, 1);
            dc.FillRectangle(Brush(0xFF200000), new Rect(x + 6, y + T - 6, T - 12, 4));
            dc.FillRectangle(Brush(frac > 0.5 ? Good : frac > 0.25 ? Warn : Bad), new Rect(x + 6, y + T - 6, (T - 12) * frac, 4));
            string tag = f.Asleep > 0 ? "Zz" : f.Fleeing ? "flees" : "";
            if (tag.Length > 0) Shadowed(dc, tag, x + T / 2, y - 6, 20, 0xFFC0D0FF, 1);
        }
        foreach (var bolt in b.Bolts)
        {
            double a = Math.Clamp(1 - bolt.Age / 0.3, 0, 1);
            uint col = (bolt.Colour & 0x00FFFFFF) | ((uint)(a * 255) << 24);
            dc.DrawLine(Pen(col, 4), new Point(View.X + (bolt.X0 + 0.5) * T, View.Y + (bolt.Y0 + 0.5) * T), new Point(View.X + (bolt.X1 + 0.5) * T, View.Y + (bolt.Y1 + 0.5) * T));
        }
        foreach (var fl in b.Floaters)
        {
            double a = Math.Clamp(1 - fl.Age, 0, 1);
            uint col = (fl.Colour & 0x00FFFFFF) | ((uint)(a * 255) << 24);
            double fx = View.X + fl.X * T + T / 2, fy = View.Y + fl.Y * T - 4 - fl.Age * 30;
            Text(dc, fl.Text, fx + 2, fy + 2, 26, (uint)(a * 200) << 24, 1);
            Text(dc, fl.Text, fx, fy, 26, col, 1);
        }

        DrawRoster(dc, b);
        DrawStatus(dc);
        DrawMessages(dc, b.Log);
        string help = b.Outcome != Outcome.None ? "Enter to go on"
            : !b.WaitingForPlayer ? (b.PartyQuick ? "The computer is fighting - Q takes command back" : "")
            : Engine.Ui switch
            {
                BattleUi.Aim => $"{(Engine.Aiming?.Name ?? "Attack")}: which way? (Esc to cancel)",
                BattleUi.Spells or BattleUi.Ally => "Up/Down choose, Enter cast, Esc cancel",
                _ => "Arrows step/strike   A attack   C cast   Space wait   Q auto",
            };
        Text(dc, cur is not null && b.WaitingForPlayer ? $"{cur.Name}'s turn" : "", Bar.X + 4, Bar.Y, 24, Gold);
        Text(dc, help, Bar.Right - 4, Bar.Y, 22, Dim, 2);

        if (b.WaitingForPlayer && cur is not null && Engine.Ui is BattleUi.Spells or BattleUi.Ally)
        {
            var p = new Game.Panel { Title = Engine.Ui == BattleUi.Spells ? "Cast which spell?" : $"{Engine.Aiming?.Name} on whom?" };
            if (Engine.Ui == BattleUi.Spells)
                foreach (var s in b.Castable(cur))
                    p.Add($"{s.Name,-9} x{Engine.World.Doses.GetValueOrDefault(s.Id)}  {s.Mana} mana", () => { }, b.CanCast(cur, s), s.About);
            else
                foreach (var f in b.Party.Where(f => !f.Escaped))
                    p.Add($"{f.Name}  {Math.Max(0, f.Hp)}/{f.MaxHp}", () => { });
            p.Selected = Math.Clamp(Engine.UiIndex, 0, Math.Max(0, p.Options.Count - 1));
            DrawPanel(dc, new Rect(View.X + 150, View.Y + 150, 404, 330), p);
        }
        if (b.Outcome != Outcome.None)
        {
            string banner = b.Outcome switch { Outcome.Won => "VICTORY", Outcome.Lost => "DEFEAT", _ => "ESCAPED" };
            dc.FillRectangle(Brush(0xA0000000), new Rect(View.X, View.Y + View.Height / 2 - 50, View.Width, 100));
            Shadowed(dc, banner, View.X + View.Width / 2, View.Y + View.Height / 2 - 40, 72, b.Outcome == Outcome.Won ? Gold : Bad, 1);
        }
    }
}
