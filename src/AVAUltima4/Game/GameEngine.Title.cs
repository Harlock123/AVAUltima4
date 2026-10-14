using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Game;

public sealed partial class GameEngine
{
    /// <summary>The seer's reckoning in progress: the virtues still standing, and the scores so far.</summary>
    private List<Virtue> _round = [];
    private List<Virtue> _winners = [];
    private int[] _seerScores = new int[8];
    private string _newName = "";
    public (Virtue A, Virtue B, string Text, string AnswerA, string AnswerB)? SeerQuestion { get; private set; }
    public int SeerAsked { get; private set; }

    public void ShowTitle()
    {
        Mode = Mode.Title;
        Battle = null;
        Demo = null;
        Town = null;
        Idle = 0;
        CloseAll();
        Play(Sfx.MusicStop);
        Play(Sfx.TitleTune);
        var p = new Panel { Title = "AVAUltima4" };
        p.Add("Begin a new life", NewLife);
        p.Add("Continue a saved journey", LoadMenu, Saves.Any());
        p.Add("How to play", Help);
        p.Add("Quit", () => QuitRequested?.Invoke());
        Push(p);
    }

    private void NewLife()
    {
        var p = new Panel { Title = "The Crossroads", Entry = "", Prompt = "Your name:", Back = Pop };
        p.Say("Night, and rain, at a crossroads in Merovale. Under a canvas awning an old woman sits behind a table of painted stones. " +
              "\"Come in out of the wet,\" she says. \"I am the Seer of the Crossroads. Before I read your road, tell me: by what name are you known?\"");
        p.OnEntry = name =>
        {
            if (name.Length == 0) return;
            _newName = name;
            Pop();
            StartSeer();
        };
        Push(p);
    }

    /// <summary>Begins the seer's questions: seven choices between virtues, the winners meeting the winners, until one is left.</summary>
    public void StartSeer(string? name = null)
    {
        if (name is not null) _newName = name;
        _round = [.. Enum.GetValues<Virtue>()];
        _winners = [];
        _seerScores = new int[8];
        SeerAsked = 0;
        Say("The Seer", $"\"{_newName}. A good name.\" She turns over two stones at a time, each painted with a virtue. " +
            "\"I will ask you seven things. There are no right answers - only yours.\"", AskSeer);
    }

    private void AskSeer()
    {
        if (_round.Count < 2)
        {
            if (_winners.Count == 1)
            {
                FinishSeer(_winners[0]);
                return;
            }
            _round = _winners;
            _winners = [];
        }
        var a = _round[0];
        var b = _round[1];
        _round.RemoveRange(0, 2);
        var q = People.Seer.First(s => (s.A == a && s.B == b) || (s.A == b && s.B == a));
        SeerQuestion = q;
        SeerAsked++;
        var p = new Panel { Title = $"The Seer's question {SeerAsked} of 7", Kind = PanelKind.Seer };
        p.Say(q.Text);
        p.Add(q.AnswerA, () => { Pop(); PickVirtue(q.A); });
        p.Add(q.AnswerB, () => { Pop(); PickVirtue(q.B); });
        Push(p);
    }

    /// <summary>Answers the seer's current question for a virtue (tests use this directly).</summary>
    public void PickVirtue(Virtue v)
    {
        _seerScores[(int)v]++;
        _winners.Add(v);
        AskSeer();
    }

    private void FinishSeer(Virtue v)
    {
        SeerQuestion = null;
        var info = Virtues.Get(v);
        var cls = ClassInfo.Get(info.Class);
        var town = People.Town(info.Town);
        Say("The Seer", $"She lays the last stone, the {info.Stone} stone of {v}, in front of you. \"{v} is the road you walk most easily. " +
            $"You will begin as a {cls.Name.ToLowerInvariant()}, in {town.Name}. Go to Queen Isaura at Castle Brightholm; she has been waiting for someone like you.\"",
            () => Begin(_newName, v, _seerScores));
    }

    /// <summary>Starts a new game: one adventurer of the calling the seer read, in that virtue's town.</summary>
    public void Begin(string name, Virtue path, int[]? scores = null)
    {
        var info = Virtues.Get(path);
        var hero = Character.Make(name, info.Class);
        World = new WorldState
        {
            Party = [hero],
            Gold = 150,
            Food = 200,
            Torches = 2,
            Seed = Rng.State,
        };
        for (int i = 0; i < 8; i++) World.Virtues[i] = 35 + 5 * (scores?[i] ?? (i == (int)path ? 3 : 0));
        foreach (var s in Spell.All.Where(s => s.Taught)) World.Known.Add(s.Id);
        World.Reagents[(int)Reagent.Thistle] = 6;
        World.Reagents[(int)Reagent.Moonpetal] = 6;
        World.Reagents[(int)Reagent.Cinder] = 4;
        World.Reagents[(int)Reagent.Amber] = 4;
        World.Doses["mend"] = 2;
        if (hero.MaxMp > 0) World.Doses["spark"] = 2;
        Log.Clear();
        CloseAll();
        Battle = null;
        Mode = Mode.Play;
        var home = Land.Site(info.Town);
        World.WorldX = home.X;
        World.WorldY = home.Y;
        EnterTown(info.Town);
        Note($"Welcome to {Town!.Name}, {name}.");
        Note("H for help at any time.");
    }

    // ───────────────────────────── the attract demo ─────────────────────────────

    /// <summary>A ready-made company out on the road, for the demo.</summary>
    public static List<Character> DemoParty() =>
    [
        Character.Companion("Aldric", ClassKind.Paladin, 3, "sword", "chain"),
        Character.Companion("Brenna", ClassKind.Fighter, 3, "axe", "chain"),
        Character.Companion("Sela", ClassKind.Ranger, 3, "bow", "leather"),
        Character.Companion("Mariel", ClassKind.Mage, 3, "staff", "cloth"),
    ];

    public void StartDemo()
    {
        var rng = new Rng((ulong)(Clock * 1000) + 7);
        Rng = rng;
        World = new WorldState { Party = DemoParty(), Gold = 0, Food = 500, Seed = rng.State };
        for (int i = 0; i < 8; i++) World.Virtues[i] = 50;
        World.Doses["mend"] = 6;
        World.Doses["spark"] = 6;
        Log.Clear();
        CloseAll();
        Mode = Mode.Play;
        var castle = Land.Site("brightholm");
        World.Where = Where.World;
        World.X = castle.X;
        World.Y = castle.Y + 1;
        Town = null;
        Roamers.Clear();
        Play(Sfx.MusicStop);
        Play(Sfx.WorldMusic);
        Demo = new DemoPilot(this);
        Note("The company sets out from Castle Brightholm.");
    }

    public void EndDemo()
    {
        Demo = null;
        World = new WorldState();
        ShowTitle();
    }

    // ───────────────────────────── help ─────────────────────────────

    public void Help()
    {
        var p = new Panel { Title = "How to play", Kind = PanelKind.Big, Back = Pop };
        p.Say("Become the Exemplar: grow steadfast in all eight virtues, be raised at each of their shrines, gather the eight stones and the three keys, and read the Codex of Echoes at the bottom of the Undermere.");
        p.Say("Arrows walk (in a dungeon: Up forward, Down back, Left and Right turn). Walk into people to talk, and into a shop counter to trade. Enter goes into towns, shrines and dungeons, and climbs ladders.");
        p.Say("Talking: type a word and press Enter. Everyone answers NAME, JOB and BYE; ask about anything they mention. Only the first four letters matter.");
        p.Say("S search here   M mix reagents   C cast   V party   I items   J journal   G peer at a gem   L light a torch   F fire the ship's cannon   Space wait   Esc menu (save, load)");
        p.Say("In a fight: arrows step (into a foe to strike it; off the edge to run), A then an arrow attacks along a line, C casts, Space waits, Q lets the computer fight.");
        p.Say("Your virtues grow and shrink with what you do, not with what you say you'd do. People are watching. So are you.");
        p.Add("Close", Pop);
        Push(p);
    }
}
