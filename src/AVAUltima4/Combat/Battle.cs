using AVAUltima4.Game;
using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Combat;

public enum Outcome { None, Won, Lost, Fled }

/// <summary>What the battlefield looks like: it follows the ground where the fight broke out.</summary>
public enum Ground { Grass, Forest, Hills, Swamp, Sand, Dungeon, Sea }

/// <summary>
/// A fight on an eleven-by-eleven field. Each round every member of the party acts in turn - steps
/// one square (stepping into a foe strikes it, stepping off the edge runs away), attacks along a
/// line, casts a spell or waits - and then every foe acts. Creatures that are not evil run when
/// badly hurt; how the party treats them is noticed.
/// </summary>
public sealed class Battle
{
    public const int W = 11, H = 11;

    // The balance of a fight (tuned against the balance probe in the tests).
    public const int PartyAim = 52, FoeAim = 44, FoeHit = 17;

    private const byte Open = 0, Block = 1, Water = 2;
    private readonly byte[,] _ground = new byte[W, H];
    private readonly Rng _rng;
    private readonly WorldState _world;
    private int _partyIndex = -1;
    private int _foeIndex = -1;
    private bool _foesTurn;
    private double _wait;

    public Ground Ground { get; }
    public List<Fighter> All { get; } = [];
    public int Round { get; private set; }
    public Outcome Outcome { get; private set; }
    public List<string> Log { get; } = [];
    public List<Floater> Floaters { get; } = [];
    public List<Bolt> Bolts { get; } = [];
    public int Gold { get; private set; }
    public bool Warded { get; private set; }
    /// <summary>Wicked foes slain: each is a small act of Daring.</summary>
    public int EvilSlain { get; private set; }
    /// <summary>Creatures that ran from the field without being struck as they ran.</summary>
    public int Spared { get; private set; }
    /// <summary>Creatures that were not evil, struck as they ran away.</summary>
    public int StruckFleeing { get; private set; }
    /// <summary>Someone in the party ran from the fight.</summary>
    public bool Ran { get; private set; }
    /// <summary>No pauses between actions: for tests and the attract demo.</summary>
    public bool Instant { get; set; }
    /// <summary>The computer fights for the party.</summary>
    public bool PartyQuick { get; set; }
    public event Action<Sfx>? Sound;

    public IEnumerable<Fighter> Party => All.Where(f => f.Side == Side.Party);
    public IEnumerable<Fighter> Foes => All.Where(f => f.Side == Side.Foe);
    public IEnumerable<Fighter> LiveFoes => Foes.Where(f => !f.Gone);
    public IEnumerable<Fighter> LiveParty => Party.Where(f => !f.Gone);

    public Fighter? Current => Outcome == Outcome.None && !_foesTurn && _partyIndex >= 0 && _partyIndex < PartyList.Count ? PartyList[_partyIndex] : null;
    private List<Fighter> PartyList => Party.ToList();

    /// <summary>A member of the party is waiting to be told what to do.</summary>
    public bool WaitingForPlayer => Current is { } c && !c.Gone && !PartyQuick && _wait <= 0;

    public bool IsBlocked(int x, int y) => x < 0 || y < 0 || x >= W || y >= H || _ground[x, y] == Block;
    public bool IsWater(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && _ground[x, y] == Water;
    public bool IsObstacle(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && _ground[x, y] == Block;

    public Fighter? At(int x, int y) => All.FirstOrDefault(f => f.X == x && f.Y == y && !f.Gone);

    public Battle(IReadOnlyList<Character> party, IReadOnlyList<Monster> foes, Ground ground, WorldState world, Rng rng)
    {
        _rng = rng;
        _world = world;
        Ground = ground;
        LayGround();
        int[][] spots = [[5, 9], [4, 9], [6, 9], [3, 9], [7, 9], [2, 9], [8, 9], [5, 10]];
        int s = 0;
        foreach (var pc in party)
        {
            if (!pc.Alive) continue;
            var (x, y) = (spots[s][0], spots[s][1]);
            s++;
            _ground[x, y] = Open;
            All.Add(new Fighter { Name = pc.Name, Side = Side.Party, Pc = pc, MaxHp = pc.MaxHp, X = x, Y = y });
        }
        var counts = foes.GroupBy(m => m.Id).ToDictionary(g => g.Key, g => g.Count());
        var numbered = new Dictionary<string, int>();
        foreach (var m in foes)
        {
            (int X, int Y) spot = (5, 1);
            for (int tries = 0; tries < 200; tries++)
            {
                spot = (1 + _rng.Next(W - 2), 1 + _rng.Next(3));
                if (IsBlocked(spot.X, spot.Y) || At(spot.X, spot.Y) is not null) continue;
                if (IsWater(spot.X, spot.Y) && !m.Is(Traits.Sea)) continue;
                break;
            }
            if (_ground[spot.X, spot.Y] == Block) _ground[spot.X, spot.Y] = Open;
            // Deeper things are tougher than their dice alone: a fifth more for every tier past the first.
            int hp = Math.Max(1, m.Hp.Roll(_rng) * (10 + 2 * Math.Max(0, m.Tier - 1)) / 10);
            string name = m.Name;
            if (counts[m.Id] > 1)
            {
                numbered[m.Id] = numbered.GetValueOrDefault(m.Id) + 1;
                name = $"{m.Name} {numbered[m.Id]}";
            }
            var f = new Fighter { Name = name, Side = Side.Foe, Kind = m, MaxHp = hp, X = spot.X, Y = spot.Y };
            f.Hp = hp;
            All.Add(f);
        }
        Say(Describe(foes));
        StartRound();
    }

    public static string Describe(IReadOnlyList<Monster> foes) =>
        string.Join(", ", foes.GroupBy(m => m.Id).Select(g => g.Count() == 1 ? ("AEIOU".Contains(g.First().Name[0]) ? "an " : "a ") + g.First().Name : $"{g.Count()} {g.First().Plural}")) + "!";

    private void LayGround()
    {
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            int roll = _rng.Next(100);
            _ground[x, y] = Ground switch
            {
                Ground.Forest => roll < 22 ? Block : Open,
                Ground.Hills => roll < 14 ? Block : Open,
                Ground.Swamp => roll < 10 ? Water : roll < 15 ? Block : Open,
                Ground.Grass or Ground.Sand => roll < 4 ? Block : Open,
                Ground.Dungeon => x % 4 == 1 && y % 4 == 1 && y < 8 ? Block : Open,
                Ground.Sea => y < 5 ? Water : Open,
                _ => Open,
            };
        }
        // Keep the party's row clear.
        for (int x = 0; x < W; x++)
        {
            if (_ground[x, 9] == Block) _ground[x, 9] = Open;
            if (_ground[x, 10] == Block) _ground[x, 10] = Open;
        }
    }

    public void Say(string text)
    {
        Log.Add(text);
        if (Log.Count > 60) Log.RemoveAt(0);
    }

    private void Float(Fighter f, string text, uint colour) => Floaters.Add(new Floater(f.X, f.Y, text, colour));
    private void Play(Sfx s) => Sound?.Invoke(s);

    private void Pause(double seconds)
    {
        if (!Instant) _wait = Math.Max(_wait, seconds);
    }

    // ───────────────────────────── rounds and turns ─────────────────────────────

    private void StartRound()
    {
        if (Outcome != Outcome.None) return;
        Round++;
        _foesTurn = false;
        _partyIndex = -1;
        NextPartyMember();
    }

    private void NextPartyMember()
    {
        var list = PartyList;
        while (Outcome == Outcome.None)
        {
            _partyIndex++;
            if (_partyIndex >= list.Count)
            {
                StartFoes();
                return;
            }
            var f = list[_partyIndex];
            if (f.Gone) continue;
            if (f.Pc!.Status == Status.Poisoned)
            {
                Hurt(f, 1, 0xFF80FF40, null);
                if (f.Gone) continue;
            }
            if (f.Asleep > 0)
            {
                f.Asleep--;
                Float(f, "Zzz", 0xFFA0C0FF);
                continue;
            }
            if (PartyQuick) Pause(0.25);
            return;
        }
    }

    private void StartFoes()
    {
        _foesTurn = true;
        _foeIndex = -1;
        Pause(0.2);
    }

    /// <summary>The current member's turn is over.</summary>
    private void Done()
    {
        CheckEnd();
        if (Outcome != Outcome.None) return;
        NextPartyMember();
    }

    private void CheckEnd()
    {
        if (Outcome != Outcome.None) return;
        if (!LiveFoes.Any())
        {
            Outcome = Outcome.Won;
            Say("The field is yours!");
            Play(Sfx.Victory);
        }
        else if (!LiveParty.Any())
        {
            if (Party.Any(p => p.Escaped))
            {
                Outcome = Outcome.Fled;
                Say("The party has fled.");
            }
            else
            {
                Outcome = Outcome.Lost;
                Say("The party has fallen.");
                Play(Sfx.Defeat);
            }
        }
    }

    /// <summary>Runs the clock: floaters fade, and the foes (or the computer's party) act with short pauses to watch.</summary>
    public void Tick(double dt)
    {
        foreach (var f in Floaters) f.Age += dt;
        Floaters.RemoveAll(f => f.Age > 1.0);
        foreach (var b in Bolts) b.Age += dt;
        Bolts.RemoveAll(b => b.Age > 0.3);
        if (Outcome != Outcome.None) return;
        if (_wait > 0)
        {
            _wait -= dt;
            return;
        }
        if (_foesTurn)
        {
            FoeStep();
            return;
        }
        if (Current is { } c && PartyQuick) AutoAct(c);
    }

    /// <summary>Plays the fight to its end with no pauses, the computer fighting for the party.</summary>
    public Outcome RunToEnd(int maxSteps = 20000)
    {
        Instant = true;
        PartyQuick = true;
        for (int i = 0; i < maxSteps && Outcome == Outcome.None; i++) Tick(0.02);
        return Outcome;
    }

    // ───────────────────────────── the party's actions ─────────────────────────────

    /// <summary>Steps one square. Into a foe, it strikes; off the edge of the field, it runs away.</summary>
    public bool Move(Dir d)
    {
        var c = Current;
        if (c is null) return false;
        var (dx, dy) = d.Step();
        int nx = c.X + dx, ny = c.Y + dy;
        if (nx < 0 || ny < 0 || nx >= W || ny >= H)
        {
            c.Escaped = true;
            Ran = true;
            Say($"{c.Name} runs from the fight!");
            Play(Sfx.Step);
            Done();
            return true;
        }
        if (At(nx, ny) is { } other)
        {
            if (other.Side == Side.Foe)
            {
                Strike(c, other, ranged: false);
                Pause(0.2);
                Done();
                return true;
            }
            return false;
        }
        if (IsBlocked(nx, ny) || IsWater(nx, ny)) return false;
        c.X = nx;
        c.Y = ny;
        Play(Sfx.Step);
        Done();
        return true;
    }

    /// <summary>How far the current member's weapon reaches along a line.</summary>
    public static int Reach(Character pc) => pc.WeaponItem.Range switch { 0 => 1, 1 => 2, var r => r };

    /// <summary>The first foe along a line from a square, within a reach, if nothing solid is in the way.</summary>
    public Fighter? FirstAlong(int x, int y, Dir d, int reach)
    {
        var (dx, dy) = d.Step();
        for (int i = 1; i <= reach; i++)
        {
            int nx = x + dx * i, ny = y + dy * i;
            if (IsBlocked(nx, ny)) return null;
            if (At(nx, ny) is { } f) return f.Side == Side.Foe ? f : null;
        }
        return null;
    }

    /// <summary>Attacks along a line: the weapon's reach, or a missile weapon's range.</summary>
    public bool Attack(Dir d)
    {
        var c = Current;
        if (c is null) return false;
        int reach = Reach(c.Pc!);
        var t = FirstAlong(c.X, c.Y, d, reach);
        bool missile = c.Pc!.WeaponItem.Range > 1;
        if (missile)
        {
            var (dx, dy) = d.Step();
            var end = t is not null ? (t.X, t.Y) : (Math.Clamp(c.X + dx * reach, 0, W - 1), Math.Clamp(c.Y + dy * reach, 0, H - 1));
            Bolts.Add(new Bolt(c.X, c.Y, end.Item1, end.Item2, 0xFFE0D0A0));
            Play(Sfx.Shoot);
        }
        if (t is null)
        {
            Say($"{c.Name} {(missile ? "shoots" : "swings")} at nothing.");
            Done();
            return true;
        }
        Strike(c, t, ranged: missile);
        Pause(0.2);
        Done();
        return true;
    }

    public void Pass()
    {
        var c = Current;
        if (c is null) return;
        Done();
    }

    private void Strike(Fighter a, Fighter t, bool ranged)
    {
        int chance;
        int dmg;
        if (a.Pc is { } pc)
        {
            chance = Math.Clamp(PartyAim + pc.Dex / 2 - 5 * t.Kind!.Defence, 15, 95);
            dmg = pc.RollDamage(_rng);
        }
        else
        {
            int def = t.Pc!.Defence + (Warded ? 2 : 0);
            chance = Math.Clamp(FoeAim + a.Kind!.Skill * 4 - 6 * def, 8, 92);
            dmg = a.Kind.Damage.Roll(_rng) * FoeHit / 10;
        }
        if (t.Asleep > 0) chance = 100;
        if (_rng.Next(100) >= chance)
        {
            if (!ranged) Play(Sfx.Miss);
            Float(t, "miss", 0xFFC0C0C0);
            Say($"{a.Name} misses {t.Name}.");
            return;
        }
        Play(Sfx.Hit);
        Say($"{a.Name} hits {t.Name}.");
        Hurt(t, Math.Max(1, dmg), 0xFFFF6040, a);
        if (t.Gone || a.Kind is null || t.Pc is null) return;
        if (a.Kind.Is(Traits.Poison) && _rng.Chance(30) && t.Pc.Status == Status.Good)
        {
            t.Pc.Status = Status.Poisoned;
            Say($"{t.Name} is poisoned!");
            Float(t, "poison", 0xFF80FF40);
        }
        if (a.Kind.Is(Traits.Sleep) && _rng.Chance(25))
        {
            t.Asleep = Math.Max(t.Asleep, _rng.Range(1, 3));
            Say($"{t.Name} falls asleep!");
            Play(Sfx.Sleep);
            Float(t, "asleep", 0xFFA0C0FF);
        }
    }

    /// <summary>Wounds someone. The one who struck the blow gets the experience for a kill.</summary>
    public void Hurt(Fighter t, int dmg, uint colour, Fighter? by)
    {
        if (t.Gone) return;
        Float(t, dmg.ToString(), colour);
        t.Asleep = 0;
        if (t.Fleeing && !t.Evil && t.Side == Side.Foe && !t.HitWhileFleeing && by is { IsPc: true })
        {
            t.HitWhileFleeing = true;
            StruckFleeing++;
        }
        if (t.Pc is { } pc)
        {
            pc.Hurt(dmg);
            if (!pc.Alive)
            {
                Say($"{t.Name} is slain!");
                Play(Sfx.Fall);
            }
            else Play(Sfx.Hurt);
        }
        else
        {
            t.Hp -= dmg;
            if (t.Hp <= 0)
            {
                Say($"{t.Name} is slain!");
                Play(Sfx.Kill);
                if (by?.Pc is { } killer) killer.Xp += t.Kind!.Xp;
                Gold += Math.Max(0, t.Kind!.Gold.Roll(_rng));
                if (t.Evil) EvilSlain++;
            }
        }
        CheckEnd();
    }

    // ───────────────────────────── spells ─────────────────────────────

    /// <summary>Whether the current member can cast a spell now: a dose mixed, enough mana, and a spell for battle.</summary>
    public bool CanCast(Fighter c, Spell s) =>
        c.Pc is { } pc && s.Use.HasFlag(SpellUse.Battle) && _world.Doses.GetValueOrDefault(s.Id) > 0 && pc.Mp >= s.Mana;

    public IEnumerable<Spell> Castable(Fighter c) => Spell.All.Where(s => s.Use.HasFlag(SpellUse.Battle) && _world.Doses.GetValueOrDefault(s.Id) > 0);

    /// <summary>Casts a spell: along a line (for Spark), at a member of the party, or over everyone.</summary>
    public bool Cast(string id, Dir? dir = null, Fighter? ally = null)
    {
        var c = Current;
        var s = Spell.Get(id);
        if (c is null || !CanCast(c, s)) return false;
        if (s.Target == SpellTarget.Line && dir is null) return false;
        if (s.Target == SpellTarget.Ally && (ally is null || ally.Side != Side.Party || ally.Escaped)) return false;
        var pc = c.Pc!;
        pc.Mp -= s.Mana;
        _world.Doses[id]--;
        Say($"{c.Name} casts {s.Name}!");
        Play(Sfx.Spell);
        switch (id)
        {
            case "mend":
                if (ally!.Dead) Say("Too late for that.");
                else Heal(ally, 20 + _rng.Range(2, 16));
                break;
            case "purge":
                if (ally!.Pc!.Status == Status.Poisoned)
                {
                    ally.Pc.Status = Status.Good;
                    Float(ally, "cured", 0xFF60FF80);
                    Play(Sfx.Heal);
                }
                break;
            case "spark":
            {
                var t = FirstAlong(c.X, c.Y, dir!.Value, 9);
                var (dx, dy) = dir.Value.Step();
                var end = t is not null ? (t.X, t.Y) : (Math.Clamp(c.X + dx * 9, 0, W - 1), Math.Clamp(c.Y + dy * 9, 0, H - 1));
                Bolts.Add(new Bolt(c.X, c.Y, end.Item1, end.Item2, 0xFFFF8030));
                if (t is not null) Hurt(t, new Dice(3, 6, pc.Level).Roll(_rng), 0xFFFF8030, c);
                break;
            }
            case "hush":
                foreach (var f in LiveFoes.ToList())
                {
                    if (f.Undead || f.Kind!.Is(Traits.Fearless) && _rng.Chance(50) || _rng.Next(20) < f.Kind.Tier * 2) { Float(f, "resists", 0xFFC0C0C0); continue; }
                    f.Asleep = 3;
                    Float(f, "asleep", 0xFFA0C0FF);
                }
                Play(Sfx.Sleep);
                break;
            case "ward":
                Warded = true;
                foreach (var f in LiveParty) Float(f, "warded", 0xFFFFE080);
                break;
            case "blaze":
                Play(Sfx.Fire);
                foreach (var f in LiveFoes.ToList()) Hurt(f, new Dice(4, 6, pc.Level).Roll(_rng), 0xFFFF8030, c);
                break;
            case "banish":
                foreach (var f in LiveFoes.Where(f => f.Undead).ToList())
                {
                    if (_rng.Chance(f.Kind!.Tier >= 5 ? 45 : 75)) Hurt(f, f.Hp, 0xFFFFF0A0, c);
                    else Float(f, "resists", 0xFFC0C0C0);
                }
                break;
            case "restore":
                foreach (var f in LiveParty) Heal(f, 20 + _rng.Range(2, 16));
                break;
        }
        Pause(0.35);
        Done();
        return true;
    }

    private void Heal(Fighter f, int amount)
    {
        f.Pc!.Heal(amount);
        Float(f, "+" + amount, 0xFF60FF80);
        Play(Sfx.Heal);
    }

    // ───────────────────────────── the foes ─────────────────────────────

    private void FoeStep()
    {
        var foes = Foes.ToList();
        while (true)
        {
            _foeIndex++;
            if (_foeIndex >= foes.Count)
            {
                StartRound();
                return;
            }
            var f = foes[_foeIndex];
            if (f.Gone) continue;
            if (f.Asleep > 0)
            {
                f.Asleep--;
                Float(f, "Zzz", 0xFFA0C0FF);
                continue;
            }
            FoeAct(f);
            CheckEnd();
            Pause(0.18);
            return;
        }
    }

    private bool CanEnter(Fighter f, int x, int y) =>
        x >= 0 && y >= 0 && x < W && y < H && !IsBlocked(x, y) && (!IsWater(x, y) || f.Kind?.Is(Traits.Sea) == true) && At(x, y) is null;

    private void FoeAct(Fighter f)
    {
        var m = f.Kind!;
        var targets = LiveParty.ToList();
        if (targets.Count == 0) return;
        if (!f.Fleeing && !m.Is(Traits.Fearless) && f.Hp < f.MaxHp / 4 && _rng.Chance(50))
        {
            f.Fleeing = true;
            Say($"{f.Name} turns to run!");
        }
        var nearest = targets.OrderBy(t => f.Distance(t)).First();
        if (f.Fleeing)
        {
            // Away from the party, and off the field if it can.
            var best = Enum.GetValues<Dir>().Select(d => d.Step()).Select(s => (X: f.X + s.X, Y: f.Y + s.Y))
                .OrderByDescending(p => targets.Min(t => t.Distance(p.X, p.Y))).ToList();
            foreach (var p in best)
            {
                if (p.X < 0 || p.Y < 0 || p.X >= W || p.Y >= H)
                {
                    f.Escaped = true;
                    Say($"{f.Name} escapes.");
                    if (!f.Evil && !f.HitWhileFleeing) Spared++;
                    return;
                }
                if (CanEnter(f, p.X, p.Y))
                {
                    f.X = p.X;
                    f.Y = p.Y;
                    return;
                }
            }
            return;
        }
        if (m.Is(Traits.Healer) && _rng.Chance(40) && LiveFoes.FirstOrDefault(o => o.Hp < o.MaxHp / 2) is { } hurt)
        {
            int amount = new Dice(2, 6, 2).Roll(_rng);
            hurt.Hp = Math.Min(hurt.MaxHp, hurt.Hp + amount);
            Float(hurt, "+" + amount, 0xFF60FF80);
            Say($"{f.Name} heals {hurt.Name}.");
            Play(Sfx.Heal);
            return;
        }
        var beside = targets.Where(f.Beside).ToList();
        if (beside.Count > 0)
        {
            Strike(f, beside[_rng.Next(beside.Count)], ranged: false);
            return;
        }
        if (m.Range > 0 && _rng.Chance(70))
        {
            var shot = targets.Where(t => f.Distance(t) <= m.Range && Clear(f.X, f.Y, t.X, t.Y)).OrderBy(t => f.Distance(t)).FirstOrDefault();
            if (shot is not null)
            {
                bool fire = m.Is(Traits.Fire);
                Bolts.Add(new Bolt(f.X, f.Y, shot.X, shot.Y, fire ? 0xFFFF6020 : 0xFFC0B090));
                Play(fire ? Sfx.Fire : Sfx.Shoot);
                if (fire)
                {
                    Say($"{f.Name} breathes fire at {shot.Name}!");
                    Hurt(shot, m.Damage.Roll(_rng) * FoeHit / 10, 0xFFFF8030, f);
                }
                else Strike(f, shot, ranged: true);
                return;
            }
        }
        Approach(f, nearest);
    }

    /// <summary>One step along the shortest open path toward a target.</summary>
    private void Approach(Fighter f, Fighter target)
    {
        var start = (f.X, f.Y);
        var prev = new Dictionary<(int, int), (int, int)> { [start] = start };
        var q = new Queue<(int X, int Y)>();
        q.Enqueue(start);
        (int, int)? goal = null;
        while (q.Count > 0 && goal is null)
        {
            var (x, y) = q.Dequeue();
            foreach (var d in Enum.GetValues<Dir>())
            {
                var (dx, dy) = d.Step();
                var n = (x + dx, y + dy);
                if (prev.ContainsKey(n)) continue;
                if (Math.Abs(n.Item1 - target.X) + Math.Abs(n.Item2 - target.Y) == 0) continue;
                if (!CanEnter(f, n.Item1, n.Item2)) continue;
                prev[n] = (x, y);
                if (Math.Abs(n.Item1 - target.X) + Math.Abs(n.Item2 - target.Y) == 1) { goal = n; break; }
                q.Enqueue(n);
            }
        }
        if (goal is null)
        {
            // No path: shuffle toward it if possible.
            int sx = Math.Sign(target.X - f.X), sy = Math.Sign(target.Y - f.Y);
            if (sx != 0 && CanEnter(f, f.X + sx, f.Y)) f.X += sx;
            else if (sy != 0 && CanEnter(f, f.X, f.Y + sy)) f.Y += sy;
            return;
        }
        var step = goal.Value;
        while (prev[step] != start) step = prev[step];
        f.X = step.Item1;
        f.Y = step.Item2;
    }

    /// <summary>Whether a missile can fly between two squares without hitting a tree or a rock.</summary>
    public bool Clear(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
        int x = x0, y = y0;
        while (true)
        {
            if ((x, y) != (x0, y0) && (x, y) != (x1, y1) && IsObstacle(x, y)) return false;
            if (x == x1 && y == y1) return true;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x += sx; }
            if (e2 <= dx) { err += dx; y += sy; }
        }
    }

    // ───────────────────────────── the computer, fighting for the party ─────────────────────────────

    private void AutoAct(Fighter c)
    {
        var pc = c.Pc!;
        // Heal a badly hurt friend, if there's a dose and the mana.
        var mend = Spell.Get("mend");
        if (CanCast(c, mend) && LiveParty.Where(p => p.Pc!.Hp < p.Pc.MaxHp / 3).OrderBy(p => p.Pc!.Hp).FirstOrDefault() is { } low)
        {
            Cast("mend", ally: low);
            return;
        }
        int reach = Reach(pc);
        foreach (var d in Enum.GetValues<Dir>())
            if (FirstAlong(c.X, c.Y, d, reach) is { } t && (!t.Fleeing || t.Evil))
            {
                Attack(d);
                return;
            }
        var spark = Spell.Get("spark");
        if (CanCast(c, spark) && pc.Mp > pc.MaxMp / 2)
            foreach (var d in Enum.GetValues<Dir>())
                if (FirstAlong(c.X, c.Y, d, 9) is { Evil: true })
                {
                    Cast("spark", d);
                    return;
                }
        var foes = LiveFoes.Where(f => !f.Fleeing || f.Evil).ToList();
        if (foes.Count == 0)
        {
            Pass();
            return;
        }
        var target = foes.OrderBy(f => Math.Abs(f.X - c.X) + Math.Abs(f.Y - c.Y)).First();
        if (pc.WeaponItem.Range > 1)
        {
            // An archer lines up rather than closing in.
            if (target.X != c.X && target.Y != c.Y)
            {
                var d = Math.Abs(target.X - c.X) < Math.Abs(target.Y - c.Y)
                    ? (target.X > c.X ? Dir.East : Dir.West)
                    : (target.Y > c.Y ? Dir.South : Dir.North);
                if (TryStep(c, d)) return;
            }
        }
        var start = (c.X, c.Y);
        var prev = new Dictionary<(int, int), (int, int)> { [start] = start };
        var q = new Queue<(int X, int Y)>();
        q.Enqueue(start);
        (int, int)? goal = null;
        while (q.Count > 0 && goal is null)
        {
            var (x, y) = q.Dequeue();
            foreach (var d in Enum.GetValues<Dir>())
            {
                var (dx, dy) = d.Step();
                var n = (x + dx, y + dy);
                if (prev.ContainsKey(n) || IsBlocked(n.Item1, n.Item2) || IsWater(n.Item1, n.Item2) || At(n.Item1, n.Item2) is not null) continue;
                prev[n] = (x, y);
                if (Math.Abs(n.Item1 - target.X) + Math.Abs(n.Item2 - target.Y) == 1) { goal = n; break; }
                q.Enqueue(n);
            }
        }
        if (goal is not null)
        {
            var step = goal.Value;
            while (prev[step] != start) step = prev[step];
            var dir = step.Item1 > c.X ? Dir.East : step.Item1 < c.X ? Dir.West : step.Item2 > c.Y ? Dir.South : Dir.North;
            if (TryStep(c, dir)) return;
        }
        Pass();
    }

    private bool TryStep(Fighter c, Dir d)
    {
        var (dx, dy) = d.Step();
        int nx = c.X + dx, ny = c.Y + dy;
        if (nx < 0 || ny < 0 || nx >= W || ny >= H || IsBlocked(nx, ny) || IsWater(nx, ny) || At(nx, ny) is not null) return false;
        return Move(d);
    }
}
