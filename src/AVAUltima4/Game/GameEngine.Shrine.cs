using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Game;

public sealed partial class GameEngine
{
    /// <summary>The shrine the party is kneeling at, if any.</summary>
    public Virtue? AtShrine { get; private set; }

    /// <summary>What a vision at a shrine says to someone not yet steadfast.</summary>
    public static string Counsel(Virtue v) => v switch
    {
        Virtue.Candor => "Pay what you owe, even when nobody is counting. Take nothing that isn't yours. Answer truly when you're asked.",
        Virtue.Mercy => "Give to those who beg of you. Spare what runs from you; never strike a creature as it flees.",
        Virtue.Daring => "Stand against the wicked things of the world, and do not run from them.",
        Virtue.Fairness => "Harm nobody who has not harmed you. Judge each case on its merits, and give each its due.",
        Virtue.Selflessness => "Give your blood at the healers, and your bread to the hungry.",
        Virtue.Integrity => "Keep your word, see your quest through, and give back what isn't yours.",
        Virtue.Reverence => "Be still at the shrines, and ask the Queen's blessing.",
        _ => "Don't boast. Do the humble work nobody will thank you for.",
    };

    public void Shrine(Virtue v)
    {
        var info = Virtues.Get(v);
        if (!World.Runes.Contains(v))
        {
            Say($"The Shrine of {v}", $"The doors are carved with the sign of {v}. They will not open for you: you need the Rune of {v}, which is kept in {People.Town(info.Town).Name}.");
            return;
        }
        AtShrine = v;
        Play(Sfx.MusicStop);
        Show(() =>
        {
            var p = new Panel { Title = $"The Shrine of {v}", Kind = PanelKind.Shrine, Back = LeaveShrine };
            p.Say($"The rune fits the door, and it opens. Inside, a single flame burns above an altar of {info.Stone.ToLowerInvariant()} stone. The air is very still.");
            p.Add("Meditate", Meditate);
            p.Add("Leave", LeaveShrine);
            return p;
        });
    }

    private void LeaveShrine()
    {
        AtShrine = null;
        CloseAll();
        Play(Sfx.WorldMusic);
    }

    private void Meditate()
    {
        var p = new Panel { Title = "Meditate", Kind = PanelKind.Shrine, Entry = "", Prompt = "Mantra:", Back = Pop };
        p.Say("You kneel before the flame. What mantra do you chant?");
        p.OnEntry = m =>
        {
            Pop();
            Chant(m);
        };
        Push(p);
    }

    /// <summary>Chants a mantra at the shrine the party is at.</summary>
    public void Chant(string mantra)
    {
        if (AtShrine is not { } v) return;
        var info = Virtues.Get(v);
        for (int i = 0; i < 3; i++) AfterTurn();
        if (!string.Equals(mantra.Trim(), info.Mantra, StringComparison.OrdinalIgnoreCase))
        {
            Play(Sfx.Bump);
            Say("Meditation", "You chant, but the words don't fit the place. Your thoughts wander, and nothing comes to you.", kind: PanelKind.Shrine);
            return;
        }
        Play(Sfx.Chime);
        if (World.Elevated.Contains(v))
        {
            Say("Meditation", $"You have already been raised in {v}. You kneel a while anyway, and are glad you did.", kind: PanelKind.Shrine);
            return;
        }
        if (World[v] >= Virtues.Steadfast)
        {
            World.Elevated.Add(v);
            Flash = 1.2;
            Play(Sfx.LevelUp);
            Note($"You are raised in {v}!");
            World.Note($"Raised in {v} at its shrine.");
            string more = World.Elevated.Count == 8
                ? " You have been raised in all eight virtues. Go to the Queen: she has a word to give you."
                : $" You have been raised in {World.Elevated.Count} of the eight.";
            Say($"Raised in {v}", $"The flame rises until it fills the shrine, and a voice speaks inside you: \"{info.Meaning}\" " +
                $"You are steadfast in {v}, and you are raised in it.{more}", kind: PanelKind.Shrine);
            return;
        }
        if (World.LastShrine != v.ToString())
        {
            World.LastShrine = v.ToString();
            ChangeVirtue(Virtue.Reverence, 3, quiet: true);
        }
        Say("A vision", $"In the flame you see yourself, and your {v} is {Virtues.Standing(World[v])}. {Counsel(v)}", kind: PanelKind.Shrine);
    }

    // ───────────────────────────── the Codex of Echoes ─────────────────────────────

    private List<(string Question, string Answer)> _codex = [];
    public int CodexStep { get; private set; }

    public void Codex()
    {
        if (World.Elevated.Count < 8)
        {
            Say("The Codex of Echoes", "Beyond the seal, on a stand of white stone, lies a great closed book. It will not open. " +
                "A voice says: \"Only one raised at all eight shrines may read me.\"");
            return;
        }
        _codex = [.. Virtues.All.Select(v => (v.Riddle, v.Virtue.ToString())), .. Virtues.Principles.Select(p => (p.Riddle, p.Principle.ToString())),
            ("And what do all eight virtues make, together?", Virtues.LastWord)];
        CodexStep = 0;
        Play(Sfx.MusicStop);
        Say("The Codex of Echoes", "Beyond the seal, on a stand of white stone, lies a great book, and it opens as you come near. " +
            "Its pages are blank - until a voice speaks out of them. \"Answer, and be known.\"", AskCodex);
    }

    private void AskCodex()
    {
        if (CodexStep >= _codex.Count)
        {
            Ending();
            return;
        }
        var (q, _) = _codex[CodexStep];
        var p = new Panel { Title = "The Codex asks", Kind = PanelKind.Codex, Entry = "", Prompt = "You answer:" };
        p.Say(q);
        p.OnEntry = a =>
        {
            Pop();
            AnswerCodex(a);
        };
        Push(p);
    }

    /// <summary>Gives the Codex an answer to its current question.</summary>
    public void AnswerCodex(string answer)
    {
        if (CodexStep >= _codex.Count) return;
        if (string.Equals(answer.Trim(), _codex[CodexStep].Answer, StringComparison.OrdinalIgnoreCase))
        {
            CodexStep++;
            Play(Sfx.Chime);
            AskCodex();
            return;
        }
        Play(Sfx.Shame);
        CodexStep = 0;
        World.Where = Where.World;
        World.X = World.WorldX;
        World.Y = World.WorldY;
        World.Aboard = false;
        Say("The Codex is silent", "The pages go dark. The floor falls away beneath you - and you find yourself outside, on the isle, in the rain. " +
            "Think on what you know, and come back.", () => Play(Sfx.WorldMusic));
    }

    private void Ending()
    {
        Mode = Mode.Ending;
        CloseAll();
        Play(Sfx.Ending);
        World.Flags.Add("exemplar");
        var p = new Panel { Title = "The Exemplar", Kind = PanelKind.Ending };
        p.Say($"The last answer echoes away, and the Codex shines. Its pages fill with every kindness, every truth and every brave act you have done, and many you had forgotten.");
        p.Say($"When you climb out of the Undermere the rain has stopped. Queen Isaura is waiting on the shore, and she bows to you - the first time anyone has seen her bow.");
        p.Say($"\"Merovale has its Exemplar,\" she says. \"Now everyone can see that it can be done.\" And across the land, {World.Leader.Name}, people begin to try.");
        p.Add("Return to the title", ShowTitle);
        Push(p);
    }
}
