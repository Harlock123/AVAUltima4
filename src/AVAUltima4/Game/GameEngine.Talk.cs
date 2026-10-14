using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Game;

public sealed partial class GameEngine
{
    /// <summary>Who the party is talking to, and a yes-or-no question waiting for its answer.</summary>
    public TownNpc? Talking { get; private set; }
    private Action<bool>? _yesNo;

    private void TalkToward(Dir d)
    {
        var (dx, dy) = d.Step();
        if (Town!.NpcAt(World.X + dx, World.Y + dy) is { } n) Talk(n);
        else if (Town.KeeperAcross(World.X, World.Y, d) is { } k) OpenShop(k);
        else Note("Nobody there.");
    }

    public void Talk(TownNpc npc)
    {
        if (npc.Sheep)
        {
            Note("The sheep says: Baa.");
            return;
        }
        if (npc.Shop is not null)
        {
            OpenShop(npc);
            return;
        }
        Talking = npc;
        _yesNo = null;
        var def = npc.Def;
        var p = new Panel
        {
            Title = def is null ? npc.Name : Cap(def.Look),
            Kind = PanelKind.Talk,
            Picture = npc.Sprite,
            Entry = "",
            Prompt = "You say:",
        };
        p.Say(def is null ? "\"Move along, citizen.\"" : $"You meet {def.Look}.");
        p.OnEntry = word => Reply(p, word);
        p.Back = EndTalk;
        Push(p);
        if (def?.Kind == NpcKind.Queen) Audience(p);
    }

    private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private void EndTalk()
    {
        Talking = null;
        _yesNo = null;
        if (Top?.Kind == PanelKind.Talk) Pop();
    }

    private void Line(Panel p, string text)
    {
        p.Text.Add(text);
        while (p.Text.Count > 14) p.Text.RemoveAt(0);
    }

    /// <summary>Whether a typed word matches a topic key: the first four letters are all that count.</summary>
    public static bool Matches(string typed, string key)
    {
        typed = typed.ToLowerInvariant();
        key = key.ToLowerInvariant();
        if (typed.Length < 4 || key.Length < 4) return typed == key;
        return typed[..4] == key[..4];
    }

    /// <summary>Says a word to whoever the party is talking to.</summary>
    public void Speak(string word)
    {
        if (Top is { Kind: PanelKind.Talk } p) Reply(p, word);
    }

    private void Reply(Panel p, string word)
    {
        p.Entry = "";
        var npc = Talking;
        if (npc is null) return;
        var def = npc.Def;
        word = word.Trim();
        if (_yesNo is { } yn)
        {
            if (word.Length == 0) return;
            Line(p, "> " + word);
            char c = char.ToLowerInvariant(word[0]);
            if (c != 'y' && c != 'n')
            {
                Line(p, "\"Yes or no?\"");
                return;
            }
            _yesNo = null;
            yn(c == 'y');
            return;
        }
        if (word.Length == 0 || Matches(word, "bye"))
        {
            Note($"{npc.Name}: Farewell.");
            EndTalk();
            return;
        }
        Line(p, "> " + word);
        if (Matches(word, "name"))
        {
            string name = def is null ? "\"I'm one of the town guard.\"" : $"\"I am {def.Name}.\"";
            Line(p, name);
            if (def is not null) p.Title = def.Name;
            return;
        }
        if (Matches(word, "job") || Matches(word, "work"))
        {
            Line(p, def is null ? "\"Keeping the peace.\"" : $"\"{def.Job}\"");
            return;
        }
        if (def is null)
        {
            Line(p, "\"Ask in the town. I've a gate to watch.\"");
            return;
        }
        if (Matches(word, "join"))
        {
            Join(p, npc);
            return;
        }
        if (def.Kind == NpcKind.Beggar && (Matches(word, "give") || Matches(word, "alms") || Matches(word, "coin") || Matches(word, "gold") || Matches(word, "help") || Matches(word, "beg")))
        {
            Beg(p, npc);
            return;
        }
        if (def.Name == "Wenna" && World.Has("purse") && !World.Has("purse-done") && (Matches(word, "purse") || Matches(word, "lost") || Matches(word, "found")))
        {
            Line(p, "\"My purse! Have you found it?\"");
            _yesNo = yes =>
            {
                World.Flags.Add("purse-done");
                if (yes)
                {
                    Line(p, "\"Bless you! Here, it's only fair you have something for your trouble.\" (She gives you 20 gold.)");
                    World.Gold += 20;
                    ChangeVirtue(Virtue.Integrity, 5);
                    ChangeVirtue(Virtue.Candor, 3, quiet: true);
                }
                else
                {
                    Line(p, "\"Oh. Well... keep looking, won't you?\" (The purse is heavy in your pack: 120 gold that isn't yours.)");
                    World.Gold += 120;
                    ChangeVirtue(Virtue.Candor, -5);
                    ChangeVirtue(Virtue.Integrity, -5, quiet: true);
                }
            };
            return;
        }
        var topic = def.Topics.FirstOrDefault(t => t.Keys.Split('|').Any(k => Matches(word, k)));
        if (topic is null)
        {
            Line(p, def.Kind == NpcKind.Child ? "\"Huh?\"" : "\"I can't help you with that.\"");
            return;
        }
        Topic(p, npc, topic);
    }

    private void Topic(Panel p, TownNpc npc, Topic t)
    {
        var def = npc.Def!;
        string reply = Fill(t.Reply);
        Line(p, $"\"{reply}\"");
        if (t.Note) World.Note($"{def.Name} ({People.Town(def.Town).Name}): {reply}");
        if (t.Teach is { } spell && World.Known.Add(spell))
        {
            Line(p, $"(You write the recipe for {Spell.Get(spell).Name} in your spellbook.)");
            Play(Sfx.Pick);
        }
        switch (t.Flag)
        {
            case "queenheal":
                foreach (var c in World.Party)
                {
                    c.Revive();
                    c.Status = Status.Good;
                    c.Hp = c.MaxHp;
                    c.Mp = c.MaxMp;
                }
                Play(Sfx.Heal);
                break;
            case "bless":
                if (World.Visit.Add("bless")) ChangeVirtue(Virtue.Reverence, 2);
                break;
        }
        if (t.Ask is not { } q) return;
        bool perVisit = t.Flag is "feedfamily" or "flock";
        string flag = $"asked:{def.Id}:{t.Keys}";
        if (perVisit ? World.Visit.Contains(flag) : World.Has(flag))
        {
            Line(p, perVisit ? "\"You've done enough for today. Come back another time.\"" : "\"But I've asked you that already.\"");
            return;
        }
        Line(p, $"\"{q.Text}\"");
        _yesNo = yes =>
        {
            if (perVisit) World.Visit.Add(flag);
            else World.Flags.Add(flag);
            if (yes && t.Flag == "feedfamily")
            {
                if (World.Food < 10)
                {
                    Line(p, "(You haven't ten portions to give.)");
                    World.Visit.Remove(flag);
                    return;
                }
                World.Food -= 10;
            }
            if (yes && t.Flag == "flock") World.Turn += 60;
            Line(p, $"\"{(yes ? q.YesReply : q.NoReply)}\"");
            foreach (var (v, d) in yes ? q.Yes : q.No) ChangeVirtue(v, d);
        };
    }

    private void Beg(Panel p, TownNpc npc)
    {
        Line(p, "\"Spare a little gold?\"");
        _yesNo = yes =>
        {
            if (!yes)
            {
                Line(p, "\"I understand.\"");
                return;
            }
            if (World.Gold < 5)
            {
                Line(p, "(You haven't five gold to give.)");
                return;
            }
            World.Gold -= 5;
            Play(Sfx.Coin);
            Line(p, "\"Thank you. You can't know what it means.\" (You give five gold.)");
            if (World.Visit.Add("beg:" + npc.Name)) ChangeVirtue(Virtue.Mercy, 3);
        };
    }

    private void Join(Panel p, TownNpc npc)
    {
        var def = npc.Def!;
        if (def.Kind != NpcKind.Companion || def.Joins is not { } cls)
        {
            Line(p, "\"My place is here. But thank you.\"");
            return;
        }
        var v = People.Town(def.Town).Virtue!.Value;
        if (World.Party.Count >= 8)
        {
            Line(p, "\"Your company is full.\"");
            return;
        }
        if (World[v] < 40)
        {
            Line(p, $"\"I'll walk with someone who shows more {v} than you do. Come back when you've grown.\"");
            return;
        }
        var c = Character.Companion(def.Name, cls, def.JoinLevel, def.JoinWeapon ?? ClassInfo.Get(cls).Weapons[0], def.JoinArmour ?? ClassInfo.Get(cls).Armour[0]);
        World.Party.Add(c);
        World.Flags.Add("joined:" + def.Name);
        Town!.Npcs.Remove(npc);
        Play(Sfx.LevelUp);
        Note($"{def.Name} joins your company!");
        EndTalk();
        Say("A new companion", $"\"Then I'm with you,\" says {def.Name}. A level {c.Level} {c.Title.ToLowerInvariant()} joins the company.");
    }

    /// <summary>An audience with the Queen: she raises everyone who has earned a new level.</summary>
    private void Audience(Panel p)
    {
        Line(p, "\"Welcome. Ask me what you will - about the EXEMPLAR, the VIRTUES, the PRINCIPLES, or for HELP. Or let me HEAL you.\"");
        foreach (var c in World.Party.Where(c => c.Alive))
        {
            bool rose = false;
            while (c.CanRise)
            {
                c.Rise(Rng);
                rose = true;
            }
            if (rose)
            {
                Line(p, $"(The Queen touches {c.Name}'s brow: {c.Name} is now level {c.Level}!)");
                Note($"{c.Name} rises to level {c.Level}!");
                Play(Sfx.LevelUp);
            }
        }
    }

    // ───────────────────────────── words filled in from the land ─────────────────────────────

    /// <summary>Fills in a reply's placeholders: places on the map, and the Queen's advice.</summary>
    public string Fill(string text)
    {
        string Coords((int X, int Y) p) => $"{p.X},{p.Y}";
        text = text.Replace("{horn}", Coords(Land.Horn)).Replace("{taper}", Coords(Land.Taper));
        var isle = Land.Site("undermere");
        text = text.Replace("{isle}", $"{isle.X},{isle.Y}");
        if (text.Contains("{progress}")) text = text.Replace("{progress}", Progress());
        if (text.Contains("{lastword}"))
            text = text.Replace("{lastword}", World.Elevated.Count == 8
                ? $"You have been raised in all eight. Then hear it: the word that holds all eight virtues together is {Virtues.LastWord}. The Codex will ask it of you last."
                : "There is a word that holds all eight virtues together. When you have been raised at all eight shrines, come back, and I will tell it to you.");
        int at;
        while ((at = text.IndexOf("{dir:", StringComparison.Ordinal)) >= 0)
        {
            int end = text.IndexOf('}', at);
            string id = text[(at + 5)..end];
            var site = Land.Site(id);
            text = text[..at] + Direction(World.WorldX, World.WorldY, site.X, site.Y) + text[(end + 1)..];
        }
        return text;
    }

    /// <summary>"north-east of here, not far": the way from one place to another, as people say it.</summary>
    public static string Direction(int x0, int y0, int x1, int y1)
    {
        int dx = x1 - x0, dy = y1 - y0;
        double ang = Math.Atan2(dx, -dy) * 180 / Math.PI;
        if (ang < 0) ang += 360;
        string[] names = ["north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west"];
        string dir = names[(int)Math.Round(ang / 45) % 8];
        int d = Math.Max(Math.Abs(dx), Math.Abs(dy));
        return d < 10 ? $"{dir} of here, not far" : d < 25 ? $"{dir} of here" : $"far to the {dir}";
    }

    /// <summary>The Queen's advice: what the party should do next.</summary>
    public string Progress()
    {
        var w = World;
        var tips = new List<string>();
        if (w.Party.Count == 1) tips.Add("Find companions: in every town there is someone who will JOIN you, if you show their town's virtue.");
        var notSteady = Enum.GetValues<Virtue>().Where(v => !w.Elevated.Contains(v) && w[v] < Virtues.Steadfast).OrderBy(v => w[v]).ToList();
        var ready = Enum.GetValues<Virtue>().Where(v => !w.Elevated.Contains(v) && w[v] >= Virtues.Steadfast).ToList();
        foreach (var v in ready)
            tips.Add(w.Runes.Contains(v) ? $"You are steadfast in {v}: go to its shrine and chant its mantra." : $"You are steadfast in {v}, but you need its rune, hidden in {People.Town(Virtues.Get(v).Town).Name}.");
        if (notSteady.Count > 0) tips.Add($"Your {notSteady[0]} is {Virtues.Standing(w[notSteady[0]])}. Work on it.");
        if (w.Runes.Count < 8) tips.Add($"You have {w.Runes.Count} of the eight runes.");
        if (w.Stones.Count < 8) tips.Add($"You have {w.Stones.Count} of the eight stones; they lie on altars in the four dungeons.");
        if (w.Keys.Count < 3) tips.Add($"You have {w.Keys.Count} of the three keys.");
        if (!w.HasShip) tips.Add("You will need a ship; the shipwright of Hearthwell sells one.");
        if (w.Party.Any(c => c.CanRise)) tips.Add("Some of your company have earned a new level - I have raised them.");
        if (tips.Count == 0) tips.Add("You have all you need. Go down into the Undermere, and read the Codex.");
        return string.Join(" ", tips.Take(3));
    }
}
