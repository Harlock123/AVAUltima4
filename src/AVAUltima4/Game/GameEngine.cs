using AVAUltima4.Combat;
using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Game;

public enum Mode { Title, Play, Battle, Ending }

/// <summary>
/// The whole game, apart from drawing it: the title and the seer, walking the land, the towns and
/// the dungeons, talking, trading, the shrines, magic, and handing fights to <see cref="Combat.Battle"/>.
/// Keys arrive as names ("Up", "Enter", "S"), so tests and the attract demo play exactly as a person does.
/// </summary>
public sealed partial class GameEngine
{
    public const double DemoAfter = 30;
    public const double DemoLength = 70;

    public Mode Mode { get; private set; } = Mode.Title;
    public WorldState World { get; private set; } = new();
    public Rng Rng { get; private set; }
    public Battle? Battle { get; private set; }
    public List<Panel> Panels { get; } = [];
    public Panel? Top => Panels.Count > 0 ? Panels[^1] : null;
    /// <summary>The message scroll, newest last.</summary>
    public List<string> Log { get; } = [];
    public SaveStore Saves { get; }
    public double Clock { get; private set; }
    /// <summary>Seconds on the title screen with nothing pressed.</summary>
    public double Idle { get; private set; }
    public DemoPilot? Demo { get; private set; }
    public bool InDemo => Demo is not null;
    public event Action<Sfx>? Sound;
    public event Action? QuitRequested;
    /// <summary>A short red flash when the party bumps into something.</summary>
    public double Bumped { get; private set; }
    /// <summary>A short white flash: a moongate, a spell, an elevation.</summary>
    public double Flash { get; private set; }

    public static Overworld Land => Overworld.Instance;
    public TownMap? Town { get; private set; }

    public GameEngine(int seed, SaveStore? saves = null)
    {
        Rng = new Rng((ulong)seed * 2654435761UL + 1);
        Saves = saves ?? new SaveStore();
        ShowTitle();
    }

    public void Play(Sfx s) => Sound?.Invoke(s);

    public IEnumerable<Character> Living => World.Party.Where(c => c.Alive);

    // ───────────────────────────── the clock ─────────────────────────────

    public void Update(double dt)
    {
        Clock += dt;
        if (Bumped > 0) Bumped -= dt;
        if (Flash > 0) Flash -= dt;
        if (Demo is not null)
        {
            Demo.Update(dt);
            if (Demo is not null && Demo.Elapsed > DemoLength) EndDemo();
        }
        else if (Mode == Mode.Title && Panels.Count == 1)
        {
            Idle += dt;
            if (Idle > DemoAfter) StartDemo();
        }
        if (Mode == Mode.Battle && Battle is { } b)
        {
            b.Tick(dt);
            if (b.Outcome != Outcome.None)
            {
                _battleOver += dt;
                if (_battleOver > (b.Instant ? 0 : 1.4)) FinishBattle();
            }
        }
    }

    // ───────────────────────────── panels ─────────────────────────────

    public Panel Push(Panel p)
    {
        Panels.Add(p);
        return p;
    }

    /// <summary>Shows a panel built by a function, so that it can rebuild itself after each choice.</summary>
    public Panel Show(Func<Panel> make)
    {
        var p = make();
        p.Make = make;
        return Push(p);
    }

    public void Pop()
    {
        if (Panels.Count > 0) Panels.RemoveAt(Panels.Count - 1);
        Refresh();
    }

    public void CloseAll() => Panels.Clear();

    /// <summary>Rebuilds the top panel (if it can be rebuilt), keeping the highlighted option.</summary>
    public void Refresh()
    {
        if (Top is not { Make: { } make } old) return;
        var p = make();
        p.Make = make;
        p.Selected = Math.Clamp(old.Selected, 0, Math.Max(0, p.Options.Count - 1));
        Panels[^1] = p;
    }

    /// <summary>A message with a Continue button.</summary>
    public void Say(string title, string text, Action? then = null, PanelKind kind = PanelKind.Plain)
    {
        var p = new Panel { Title = title, Kind = kind }.Say(text);
        p.Add("Continue", () => { Pop(); then?.Invoke(); });
        p.Back = () => { Pop(); then?.Invoke(); };
        Push(p);
    }

    /// <summary>A line on the message scroll.</summary>
    public void Note(string text)
    {
        Log.Add(text);
        if (Log.Count > 60) Log.RemoveAt(0);
    }

    // ───────────────────────────── keys ─────────────────────────────

    /// <summary>A key, by name: "Up", "Enter", "Esc", "Space", "Back", a letter or a digit.</summary>
    public void Press(string key)
    {
        if (Demo is not null)
        {
            EndDemo();
            return;
        }
        Input(key);
    }

    /// <summary>A key, without the demo-ending check: how the attract pilot plays.</summary>
    internal void Input(string key)
    {
        Idle = 0;
        if (Top is { } p)
        {
            PanelKey(p, key);
            return;
        }
        switch (Mode)
        {
            case Mode.Play: PlayKey(key); break;
            case Mode.Battle: BattleKey(key); break;
        }
    }

    /// <summary>Typed text: names, words to say, mantras.</summary>
    public void Type(string text)
    {
        if (Top is not { Entry: not null } p) return;
        foreach (char ch in text)
            if ((char.IsLetterOrDigit(ch) || ch is ' ' or '\'' or '-') && p.Entry!.Length < 16)
                p.Entry += ch;
    }

    private void PanelKey(Panel p, string key)
    {
        if (p.Entry is not null)
        {
            switch (key)
            {
                case "Enter":
                    p.OnEntry?.Invoke(p.Entry.Trim());
                    return;
                case "Back":
                    if (p.Entry.Length > 0) p.Entry = p.Entry[..^1];
                    return;
                case "Esc":
                    p.Back?.Invoke();
                    return;
            }
            return;
        }
        var opts = p.Options;
        switch (key)
        {
            case "Up":
                if (opts.Count > 0) { p.Selected = (p.Selected + opts.Count - 1) % opts.Count; Play(Sfx.Menu); }
                return;
            case "Down":
                if (opts.Count > 0) { p.Selected = (p.Selected + 1) % opts.Count; Play(Sfx.Menu); }
                return;
            case "Left" or "Right":
            {
                int d = key == "Left" ? -1 : 1;
                if (opts.Count > 0 && opts[p.Selected].Adjust is { } adj) { adj(d); Play(Sfx.Menu); Refresh(); }
                else if (p.Turn is { } turn) { turn(d); Play(Sfx.Menu); }
                return;
            }
            case "Enter" or "Space":
                Choose(p, p.Selected);
                return;
            case "Esc" or "Back":
                if (p.Back is { } back) { back(); Play(Sfx.Menu); }
                return;
        }
        if (key.Length == 1 && char.IsDigit(key[0]) && key[0] != '0')
        {
            int n = key[0] - '1';
            if (n < opts.Count) { p.Selected = n; Choose(p, n); }
        }
    }

    private void Choose(Panel p, int index)
    {
        if (index < 0 || index >= p.Options.Count) return;
        var o = p.Options[index];
        if (!o.Enabled || o.Act is null) return;
        Play(Sfx.Pick);
        o.Act();
        if (Top == p) Refresh();
    }
}
