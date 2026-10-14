using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using AVAUltima4.Combat;
using AVAUltima4.Game;
using AVAUltima4.Rules;
using AVAUltima4.Views;
using AVAUltima4.World;

namespace AVAUltima4.Tests;

/// <summary>
/// Draws the game in a real (headless) window at the moments worth showing. Set ULTIMA4_SHOTS to a
/// folder to save them as PNGs: the README's screenshots come from here.
/// </summary>
public sealed class ScreenshotTests
{
    private static (Window W, GameCanvas Canvas, GameEngine G) Open(int seed = 11)
    {
        var g = new GameEngine(seed, Helpers.TempSaves());
        var canvas = new GameCanvas(g, new SoundManager(silent: true), startTimer: false);
        var w = new Window { Width = 1024, Height = 768, Content = canvas };
        w.Show();
        Pump();
        return (w, canvas, g);
    }

    private static void Pump()
    {
        for (int i = 0; i < 10; i++) Dispatcher.UIThread.RunJobs();
    }

    private static void Shot(Window w, GameCanvas c, string name)
    {
        c.InvalidateVisual();
        Pump();
        var frame = w.CaptureRenderedFrame();
        Assert.NotNull(frame);
        var dir = Environment.GetEnvironmentVariable("ULTIMA4_SHOTS");
        if (string.IsNullOrEmpty(dir)) return;
        Directory.CreateDirectory(dir);
        frame.Save(Path.Combine(dir, $"AVAUltima4-{name}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }

    [AvaloniaFact]
    public void Title_and_the_seer()
    {
        var (w, c, g) = Open();
        c.Tick(1.5);
        Shot(w, c, "title");
        g.Press("Enter");
        g.Type("Ash");
        Shot(w, c, "crossroads");
        g.Press("Enter");
        g.Press("Enter");
        c.Tick(0.4);
        Shot(w, c, "seer");
    }

    [AvaloniaFact]
    public void A_town_and_a_conversation()
    {
        var (w, c, g) = Open();
        g.Begin("Ash", Virtue.Daring);
        g.CloseAll();
        for (int i = 0; i < 5; i++) g.Press("Up");
        Shot(w, c, "town");
        var gerrit = g.Town!.Npcs.First(n => n.Name == "Gerrit");
        Helpers.StandBeside(g, (gerrit.X, gerrit.Y));
        g.Talk(gerrit);
        g.Speak("name");
        g.Speak("job");
        g.Speak("mantra");
        g.Speak("dragon");
        c.Tick(0.1);
        Shot(w, c, "talk");
        g.Speak("yes");
        g.CloseAll();
        var keeper = g.Town!.Npcs.First(n => n.Shop == "weapons");
        g.OpenShop(keeper);
        g.World.Gold = 400;
        g.Refresh();
        g.Top!.Selected = 4;
        Shot(w, c, "shop");
    }

    [AvaloniaFact]
    public void The_open_land()
    {
        var (w, c, g) = Open();
        g.Begin("Ash", Virtue.Daring);
        g.World.Party.Add(Character.Companion("Brenna", ClassKind.Fighter, 3, "axe", "chain"));
        g.World.Party.Add(Character.Companion("Sela", ClassKind.Ranger, 3, "bow", "leather"));
        g.World.Party.Add(Character.Companion("Mariel", ClassKind.Mage, 2, "staff", "cloth"));
        var castle = GameEngine.Land.Site("brightholm");
        Helpers.OnLand(g, castle.X - 2, castle.Y + 2);
        g.World.Turn = WorldState.SelisPeriod * 3 + WorldState.RoanPeriod * 2;
        g.Roamers.Add(new Roamer { X = castle.X + 2, Y = castle.Y + 5, Group = [Monsters.Get("orc"), Monsters.Get("orc")] });
        g.Roamers.Add(new Roamer { X = castle.X - 5, Y = castle.Y - 1, Group = [Monsters.Get("pilgrim")] });
        c.Tick(0.3);
        Shot(w, c, "land");
        g.World.Gems = 1;
        g.Press("G");
        Shot(w, c, "gem");
        g.CloseAll();
        var gate = GameEngine.Land.Gates[g.World.Selis];
        Helpers.OnLand(g, gate.X, gate.Y + 1);
        c.Tick(0.3);
        Shot(w, c, "moongate");
    }

    [AvaloniaFact]
    public void At_sea()
    {
        var (w, c, g) = Open();
        g.Begin("Ash", Virtue.Mercy);
        var land = GameEngine.Land;
        g.World.HasShip = true;
        var isle = land.Site("undermere");
        Helpers.OnLand(g, isle.X - 6, isle.Y - 1);
        g.World.Aboard = true;
        (g.World.ShipX, g.World.ShipY) = (g.World.X, g.World.Y);
        g.Roamers.Add(new Roamer { X = isle.X - 9, Y = isle.Y + 2, Group = [Monsters.Get("serpent")] });
        c.Tick(0.3);
        Shot(w, c, "sea");
    }

    [AvaloniaFact]
    public void A_dungeon()
    {
        var (w, c, g) = Open();
        g.Begin("Ash", Virtue.Daring);
        Helpers.Strong(g);
        g.EnterDungeon("hollowroot");
        g.World.Light = 200;
        // Turn to face the longest open passage.
        var lvl = g.Dungeon!;
        int Run(Dir d)
        {
            int n = 0;
            var (dx, dy) = d.Step();
            while (DungeonLevel.Open(lvl[g.World.X + dx * (n + 1), g.World.Y + dy * (n + 1)])) n++;
            return n;
        }
        g.World.Facing = Enum.GetValues<Dir>().OrderByDescending(Run).First();
        c.Tick(0.1);
        Shot(w, c, "dungeon");
        // Stand before the altar of the second level.
        g.World.Level = 2;
        var l2 = g.Dungeon!;
        var altar = l2.Altar;
        foreach (var d in Enum.GetValues<Dir>())
        {
            var (dx, dy) = d.Step();
            if (DungeonLevel.Open(l2[altar.X - dx, altar.Y - dy]) && DungeonLevel.Open(l2[altar.X - 2 * dx, altar.Y - 2 * dy]))
            {
                (g.World.X, g.World.Y) = (altar.X - 2 * dx, altar.Y - 2 * dy);
                g.World.Facing = d;
                break;
            }
        }
        c.Tick(0.1);
        Shot(w, c, "altar");
    }

    [AvaloniaFact]
    public void A_battle()
    {
        var (w, c, g) = Open(23);
        g.Begin("Ash", Virtue.Daring);
        Helpers.Strong(g, 3);
        g.World.Doses["spark"] = 3;
        var castle = GameEngine.Land.Site("brightholm");
        Helpers.OnLand(g, castle.X - 3, castle.Y);
        g.StartBattle([Monsters.Get("orc"), Monsters.Get("orc"), Monsters.Get("goblin"), Monsters.Get("goblin"), Monsters.Get("archer"), Monsters.Get("shaman")], Ground.Forest);
        var b = g.Battle!;
        for (int i = 0; i < 2000 && b.Round < 3; i++)
        {
            if (b.WaitingForPlayer) g.Press("Up");
            c.Tick(0.05);
        }
        for (int i = 0; i < 600 && !b.WaitingForPlayer; i++) c.Tick(0.05);
        Shot(w, c, "battle");
        g.Press("Q");
        for (int i = 0; i < 4000 && g.Mode == Mode.Battle; i++)
        {
            c.Tick(0.05);
            if (b.Bolts.Count > 0 && b.Floaters.Count > 0 && b.Round >= 4) { Shot(w, c, "battle-shot"); break; }
        }
    }

    [AvaloniaFact]
    public void A_shrine_and_the_virtues()
    {
        var (w, c, g) = Open();
        g.Begin("Ash", Virtue.Daring);
        g.World.Runes.Add(Virtue.Daring);
        g.World.Runes.Add(Virtue.Candor);
        g.World.Stones.Add(Virtue.Mercy);
        g.World[Virtue.Daring] = 80;
        g.World.Elevated.Add(Virtue.Candor);
        var s = GameEngine.Land.Site("shrine-daring");
        Helpers.OnLand(g, s.X, s.Y);
        g.Press("Enter");
        c.Tick(0.2);
        Shot(w, c, "shrine");
        g.Press("Enter");
        g.Type("KUR");
        g.Press("Enter");
        c.Tick(1.0);
        Shot(w, c, "raised");
        g.CloseAll();
        g.Press("V");
        g.Top!.Selected = g.Top.Options.Count - 2;
        g.Press("Enter");
        Shot(w, c, "virtues");
        g.CloseAll();
        g.ViewMember(0);
        Shot(w, c, "sheet");
    }

    [AvaloniaFact]
    public void The_ending()
    {
        var (w, c, g) = Open();
        g.Begin("Ash", Virtue.Daring);
        foreach (var v in Enum.GetValues<Virtue>()) g.World.Elevated.Add(v);
        g.World.Where = Where.Dungeon;
        g.World.Place = "undermere";
        g.World.Level = 4;
        g.Codex();
        g.Press("Enter");
        c.Tick(0.2);
        Shot(w, c, "codex");
        foreach (var v in Virtues.All) g.AnswerCodex(v.Virtue.ToString());
        foreach (var p in Virtues.Principles) g.AnswerCodex(p.Principle.ToString());
        g.AnswerCodex(Virtues.LastWord);
        c.Tick(0.2);
        Shot(w, c, "ending");
    }
}
