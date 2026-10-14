using AVAUltima4.Combat;
using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Game;

public sealed partial class GameEngine
{
    private int _doorStep;

    public void EnterDungeon(string id)
    {
        var level = Dungeons.Level(id, 1);
        World.Where = Where.Dungeon;
        World.Place = id;
        World.Level = 1;
        (World.X, World.Y) = level.Up;
        World.Facing = Enum.GetValues<Dir>().First(d => DungeonLevel.Open(level[World.X + d.Step().X, World.Y + d.Step().Y]));
        Roamers.Clear();
        Play(Sfx.MusicStop);
        Play(Sfx.DungeonMusic);
        Note($"You go down into {Dungeons.Get(id).Name}.");
        if (World.Light <= 0) Note(World.Torches > 0 ? "It's dark. L lights a torch." : "It's dark, and you have no torches.");
    }

    private void LeaveDungeon()
    {
        World.Where = Where.World;
        World.X = World.WorldX;
        World.Y = World.WorldY;
        World.Level = 0;
        Note("You climb out into the daylight.");
        Play(Sfx.MusicStop);
        Play(Sfx.WorldMusic);
    }

    private void DungeonKey(string key)
    {
        switch (key)
        {
            case "Up": DungeonStep(World.Facing); return;
            case "Down": DungeonStep(World.Facing.Back(), turn: false); return;
            case "Left": World.Facing = World.Facing.Left(); return;
            case "Right": World.Facing = World.Facing.Right(); return;
        }
        CommonKey(key);
    }

    internal void DungeonStep(Dir d, bool turn = true)
    {
        var lvl = Dungeon!;
        var (dx, dy) = d.Step();
        int nx = World.X + dx, ny = World.Y + dy;
        if (!DungeonLevel.Open(lvl[nx, ny]))
        {
            Bump();
            return;
        }
        if (turn) World.Facing = d;
        World.X = nx;
        World.Y = ny;
        Play(Sfx.Step);
        AfterTurn();
        if (Mode != Mode.Play || !Living.Any()) return;
        if (!ArriveAt(lvl, lvl[nx, ny]) && Rng.Chance(8)) DungeonFight();
    }

    private string Here(string what) => $"{what}:{World.Place}:{World.Level}:{World.X},{World.Y}";

    /// <summary>What happens on stepping onto a square: true if something did.</summary>
    private bool ArriveAt(DungeonLevel lvl, Cell cell)
    {
        switch (cell)
        {
            case Cell.Up:
                Note(World.Level == 1 ? "A ladder up to the daylight (Enter)." : "A ladder up (Enter).");
                return true;
            case Cell.Down:
                Note("A ladder down (Enter).");
                return true;
            case Cell.Trap when !World.Has(Here("trap")):
                World.Flags.Add(Here("trap"));
                Play(Sfx.Trap);
                Bumped = 0.3;
                Note("A trap! Darts fly from the walls.");
                foreach (var c in Living.ToList()) c.Hurt(Rng.Range(2, 6 + World.Level * 2));
                if (!Living.Any()) PartyLost();
                return true;
            case Cell.Chest when !World.Has(Here("chest")):
            {
                World.Flags.Add(Here("chest"));
                if (Dungeons.Get(World.Place).Treasure?.GetValueOrDefault(World.Level) is { } treasure && lvl.Chests().First() == (World.X, World.Y))
                {
                    Treasure(treasure);
                    return true;
                }
                int gold = Rng.Range(10, 40) * (World.Level + Dungeons.Get(World.Place).Tier - 1);
                World.Gold += gold;
                Play(Sfx.Coin);
                Note($"You open a chest: {gold} gold!");
                if (Rng.Chance(20))
                {
                    var c = Rng.Pick(Living.ToList());
                    c.Status = Status.Poisoned;
                    Note($"A poison needle! {c.Name} is poisoned.");
                    Play(Sfx.Hurt);
                }
                else if (Rng.Chance(25))
                {
                    World.Torches++;
                    Note("There's a torch in it too.");
                }
                return true;
            }
            case Cell.Fountain:
                Fountain();
                return true;
            case Cell.Altar:
                Altar();
                return true;
            case Cell.Seal:
                Seal();
                return true;
        }
        return false;
    }

    /// <summary>One of the arms that can't be bought: it goes to whoever can use it and is least well armed.</summary>
    private void Treasure(string id)
    {
        bool weapon = Arms.Weapons.Any(w => w.Id == id);
        string name = weapon ? Arms.Weapon(id).Name : Arms.Armour(id).Name;
        Play(Sfx.Chime);
        Flash = 0.4;
        Note($"In the chest: {name}!");
        var taker = Living.Where(c => Arms.CanUse(c.Class, id))
            .OrderBy(c => weapon ? c.WeaponItem.Damage.Average : c.Defence).FirstOrDefault();
        if (taker is null)
        {
            Say("Treasure", $"The chest holds {name}, but nobody in your company can use it. You leave it where it lies.", () => World.Flags.Remove(Here("chest")));
            return;
        }
        if (weapon) taker.Weapon = id;
        else taker.Armour = id;
        Say("Treasure", $"The chest holds {name}! {taker.Name} takes it up.");
    }

    private void Fountain()
    {
        var p = new Panel { Title = "A fountain" };
        p.Say("Water rises from the stone into a worn basin.");
        p.Add("Drink", () =>
        {
            Pop();
            int kind = (World.Place.Length * 7 + World.Level * 3 + World.X + World.Y) % 4;
            switch (kind)
            {
                case 0:
                    foreach (var c in Living) c.Heal(c.MaxHp);
                    Play(Sfx.Heal);
                    Note("The water is sweet. Your wounds close!");
                    break;
                case 1:
                    foreach (var c in Living) if (c.Status == Status.Poisoned) c.Status = Status.Good;
                    Play(Sfx.Heal);
                    Note("The water burns, then soothes. Poison leaves you.");
                    break;
                case 2:
                    foreach (var c in Living.ToList()) c.Hurt(Rng.Range(1, 6));
                    Play(Sfx.Hurt);
                    Note("Foul water! You retch.");
                    break;
                default:
                    Note("Cold water. Nothing more.");
                    break;
            }
        });
        p.Add("Leave it", Pop);
        p.Back = Pop;
        Push(p);
    }

    internal void Altar()
    {
        var def = Dungeons.Get(World.Place);
        if (!def.Stones.TryGetValue(World.Level, out var v)) return;
        if (World.Stones.Contains(v))
        {
            Note("An empty altar.");
            return;
        }
        var info = Virtues.Get(v);
        Note($"On the altar glows the {info.Stone} Stone. Its guardians rise!");
        int tier = Math.Min(5, def.Tier + World.Level / 2);
        var kinds = Monsters.Meet(tier, false, deep: true).Where(m => m.Tier >= tier - 1).ToList();
        var guard = Rng.Pick(kinds);
        // As many guardians as the party has members (half as many of the deadliest kinds).
        int count = Math.Clamp(World.Party.Count, 2, 6);
        if (guard.Tier >= 4) count = (count + 1) / 2;
        var group = Enumerable.Range(0, count).Select(_ => guard).ToList();
        StartBattle(group, Ground.Dungeon, won: () =>
        {
            World.Stones.Add(v);
            ChangeVirtue(Virtue.Daring, 3, quiet: true);
            Found($"the {info.Stone} Stone", $"The {info.Stone} Stone of {v}, warm in your hand.");
        });
    }

    internal void Seal()
    {
        var (a, b) = Dungeons.SealStones(World.Level);
        if (World.Has($"seal:{World.Level}"))
        {
            if (World.Level == Dungeons.Levels) Codex();
            else Note("The open seal: a way down (Enter).");
            return;
        }
        var sa = Virtues.Get(a);
        var sb = Virtues.Get(b);
        var p = new Panel { Title = "A seal" };
        p.Say($"A great disc of black stone bars the way {(World.Level == Dungeons.Levels ? "on" : "down")}. Two hollows are cut into it: one {sa.Stone.ToLowerInvariant()}, one {sb.Stone.ToLowerInvariant()}.");
        bool have = World.Stones.Contains(a) && World.Stones.Contains(b);
        p.Add($"Set the {sa.Stone} and {sb.Stone} stones in it", () =>
        {
            Pop();
            World.Flags.Add($"seal:{World.Level}");
            Play(Sfx.Chime);
            Flash = 0.6;
            Note("The stones flare, and the seal rolls aside.");
            if (World.Level == Dungeons.Levels) Codex();
        }, have, have ? null : "You don't have both of those stones.");
        p.Add("Leave it", Pop);
        p.Back = Pop;
        Push(p);
    }

    private void UseLadder()
    {
        var lvl = Dungeon!;
        var cell = lvl[World.X, World.Y];
        bool openSeal = cell == Cell.Seal && World.Has($"seal:{World.Level}") && World.Level < Dungeons.Levels;
        if (cell == Cell.Up)
        {
            Play(Sfx.Ladder);
            if (World.Level == 1)
            {
                LeaveDungeon();
                return;
            }
            World.Level--;
            (World.X, World.Y) = Dungeons.Level(World.Place, World.Level).Down;
            Note($"You climb up to level {World.Level}.");
            return;
        }
        if (cell == Cell.Down || openSeal)
        {
            Play(Sfx.Ladder);
            World.Level++;
            (World.X, World.Y) = Dungeons.Level(World.Place, World.Level).Up;
            Note($"You climb down to level {World.Level}.");
            return;
        }
        if (cell == Cell.Seal)
        {
            Seal();
            return;
        }
        Note("There's no ladder here.");
    }

    private void DungeonFight()
    {
        var def = Dungeons.Get(World.Place);
        int tier = Math.Clamp(def.Tier + (World.Level - 1) / 2, 1, 5);
        var kinds = Monsters.Meet(tier, false, deep: true);
        var kind = Rng.Pick(kinds);
        int count = Rng.Range(1, Math.Min(8, World.Party.Count + 2));
        if (kind.Tier >= 4) count = Math.Max(1, (count + 1) / 2);
        StartBattle(Enumerable.Range(0, count).Select(_ => kind).ToList(), Ground.Dungeon);
    }

    // ───────────────────────────── the door of the Undermere ─────────────────────────────

    public static readonly string[] KeyOrder = ["horn", "tome", "taper"];

    public static string KeyName(string id) => id switch { "horn" => "the Brass Horn", "tome" => "the Tome of Hours", _ => "the Everflame Taper" };

    public void UndermereDoor()
    {
        if (World.Has("door"))
        {
            EnterDungeon("undermere");
            return;
        }
        _doorStep = 0;
        Show(() =>
        {
            var p = new Panel { Title = "The door of the Undermere", Back = Pop };
            p.Say("A door of black stone stands in the hillside, with three locks: a mouthpiece of brass, a little lectern, and a socket for a candle. " +
                  (_doorStep > 0 ? $"{_doorStep} of the locks stand open." : "None of them will move."));
            foreach (var k in KeyOrder)
            {
                string key = k;
                p.Add($"Use {KeyName(key)}", () => UseKey(key), World.Keys.Contains(key));
            }
            p.Add("Leave", Pop);
            return p;
        });
    }

    internal void UseKey(string key)
    {
        if (KeyOrder[_doorStep] != key)
        {
            _doorStep = 0;
            Play(Sfx.Bump);
            Note("Nothing happens - and the open locks clatter shut again.");
            return;
        }
        _doorStep++;
        Play(Sfx.Chime);
        Note(key switch
        {
            "horn" => "You sound the horn. The first lock turns.",
            "tome" => "You open the tome on the lectern. Its pages turn, and the second lock turns.",
            _ => "You set the taper in its socket. The flame leaps up.",
        });
        if (_doorStep == 3)
        {
            World.Flags.Add("door");
            Flash = 0.6;
            Pop();
            Say("The door opens", "The black door swings inward on the dark. Somewhere below, water drips.", () => EnterDungeon("undermere"));
        }
    }
}
