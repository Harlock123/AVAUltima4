using AVAUltima4.Combat;
using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Game;

/// <summary>A band of creatures moving about the open land (or the sea), visible before they arrive.</summary>
public sealed class Roamer
{
    public int X { get; set; }
    public int Y { get; set; }
    public required List<Monster> Group { get; init; }
    public bool Peaceful => !Group[0].Evil && Group[0].Tier == 0;
    public bool Sea => Group[0].Is(Traits.Sea);
    public string Sprite => Group[0].Sprite;
    public string Tint => Group[0].Tint;
}

public sealed partial class GameEngine
{
    public List<Roamer> Roamers { get; } = [];
    /// <summary>A command waiting for a direction: "talk", "fire" or "attack".</summary>
    public string? Pending { get; private set; }

    public Where Where => World.Where;
    public DungeonLevel? Dungeon => World.Where == Where.Dungeon ? Dungeons.Level(World.Place, World.Level) : null;
    public (int X, int Y) OpenGate => Land.Gates[World.Selis];

    // ───────────────────────────── keys while exploring ─────────────────────────────

    private void PlayKey(string key)
    {
        if (Pending is { } pending)
        {
            Pending = null;
            if (Dirs.FromKey(key) is { } pd)
            {
                switch (pending)
                {
                    case "talk": TalkToward(pd); break;
                    case "fire": FireCannon(pd); break;
                }
            }
            else Note("Never mind.");
            return;
        }
        if (World.Where == Where.Dungeon)
        {
            DungeonKey(key);
            return;
        }
        if (Dirs.FromKey(key) is { } d)
        {
            Walk(d);
            return;
        }
        CommonKey(key);
    }

    /// <summary>The commands that work anywhere out of a fight.</summary>
    private void CommonKey(string key)
    {
        switch (key)
        {
            case "Enter" or "E": EnterHere(); break;
            case "S": Search(); break;
            case "M": MixMenu(); break;
            case "C": CastMenu(); break;
            case "V" or "Z": PartyMenu(); break;
            case "I": ItemsPanel(); break;
            case "J": Journal(); break;
            case "G": PeerAtGem(); break;
            case "L": LightTorch(); break;
            case "H": Help(); break;
            case "Esc": GameMenu(); break;
            case "Space": Note("You wait."); AfterTurn(); break;
            case "T":
                if (World.Where != Where.Town) { Note("There's nobody here to talk to."); break; }
                Pending = "talk";
                Note("Talk - which way?");
                break;
            case "F":
                if (!World.Aboard) { Note("You have no cannon."); break; }
                Pending = "fire";
                Note("Fire - which way?");
                break;
        }
    }

    // ───────────────────────────── walking ─────────────────────────────

    public void Walk(Dir d)
    {
        var (dx, dy) = d.Step();
        int nx = World.X + dx, ny = World.Y + dy;
        World.Facing = d;
        if (World.Where == Where.Town)
        {
            WalkTown(nx, ny, d);
            return;
        }
        var tile = Land[nx, ny];
        if (World.Aboard)
        {
            if (Terrain.Sailable(tile))
            {
                World.X = World.ShipX = nx;
                World.Y = World.ShipY = ny;
                Play(Sfx.Splash);
                AfterTurn();
                return;
            }
            if (Terrain.Walkable(tile))
            {
                World.Aboard = false;
                World.X = nx;
                World.Y = ny;
                Note("You go ashore.");
                Play(Sfx.Step);
                AfterTurn();
                return;
            }
            Bump();
            return;
        }
        if (World.HasShip && (nx, ny) == (World.ShipX, World.ShipY))
        {
            World.Aboard = true;
            World.X = nx;
            World.Y = ny;
            Note("You board the ship.");
            Play(Sfx.Splash);
            AfterTurn();
            return;
        }
        if (!Terrain.Walkable(tile))
        {
            Bump();
            return;
        }
        if (Roamers.FirstOrDefault(r => r.X == nx && r.Y == ny) is { } met)
        {
            Engage(met);
            return;
        }
        World.X = nx;
        World.Y = ny;
        Play(Sfx.Step);
        if (Terrain.Slow(tile)) World.Turn++;
        AfterTurn();
        if (Mode != Mode.Play || Top is not null) return;
        if (OpenGate == (World.X, World.Y)) TakeGate();
        else if (tile == Tile.Swamp && !World.Aboard)
            foreach (var c in Living.Where(c => c.Status == Status.Good && Rng.Chance(12)).ToList())
            {
                c.Status = Status.Poisoned;
                Note($"{c.Name} is poisoned by the swamp!");
                Play(Sfx.Hurt);
            }
        if (Terrain.IsSite(tile) && Land.SiteAt(World.X, World.Y) is { } site) Note($"{site.Name} (Enter to go in)");
    }

    private void Bump()
    {
        Bumped = 0.15;
        Play(Sfx.Bump);
    }

    private void WalkTown(int nx, int ny, Dir d)
    {
        var t = Town!;
        if (nx <= 0 || ny <= 0 || nx >= TownMap.Size - 1 || ny >= TownMap.Size - 1)
        {
            LeaveTown();
            return;
        }
        if (t.NpcAt(nx, ny) is { } npc)
        {
            Talk(npc);
            return;
        }
        if (t.KeeperAcross(World.X, World.Y, d) is { } keeper)
        {
            OpenShop(keeper);
            return;
        }
        if (!TownMap.Walkable(t[nx, ny]))
        {
            Bump();
            return;
        }
        if (t[nx, ny] == TownTile.Door) Play(Sfx.Door);
        else Play(Sfx.Step);
        World.X = nx;
        World.Y = ny;
        AfterTurn();
    }

    /// <summary>The clock moves on a step: moons, food, poison, light, and anything roaming the land.</summary>
    public void AfterTurn()
    {
        World.Turn++;
        if (World.Turn % 10 == 0)
        {
            int eat = Living.Count();
            World.Food -= eat;
            if (World.Food < 0)
            {
                World.Food = 0;
                Note("Starving!");
                Play(Sfx.Hurt);
                foreach (var c in Living.ToList()) c.Hurt(2);
            }
        }
        foreach (var c in Living.Where(c => c.Status == Status.Poisoned))
            if (c.Hp > 1) c.Hp--;
        if (World.Turn % 5 == 0)
            foreach (var c in Living) c.Mp = Math.Min(c.MaxMp, c.Mp + 1);
        if (World.Where == Where.Dungeon && World.Light > 0)
        {
            World.Light--;
            if (World.Light == 0) Note("Your light goes out.");
        }
        if (World.Where == Where.Town) Town!.Wander(Rng, World.X, World.Y);
        if (World.Where == Where.World) MoveRoamers();
        if (!Living.Any()) PartyLost();
    }

    // ───────────────────────────── towns ─────────────────────────────

    public void EnterTown(string id)
    {
        Town = TownMap.Fresh(id);
        Town.Npcs.RemoveAll(n => n.Def is { Kind: NpcKind.Companion } c && World.Has("joined:" + c.Name));
        World.Where = Where.Town;
        World.Place = id;
        World.X = TownMap.GateX;
        World.Y = TownMap.GateY - 1;
        World.Facing = Dir.North;
        World.Visit.Clear();
        Roamers.Clear();
        Play(Sfx.MusicStop);
        Play(Sfx.TownMusic);
        Note(Town.Def.Arrival);
    }

    public void LeaveTown()
    {
        World.Where = Where.World;
        World.X = World.WorldX;
        World.Y = World.WorldY;
        Note($"You leave {Town?.Name}.");
        Town = null;
        Play(Sfx.MusicStop);
        Play(Sfx.WorldMusic);
    }

    // ───────────────────────────── going into places ─────────────────────────────

    public void EnterHere()
    {
        if (World.Where == Where.Dungeon)
        {
            UseLadder();
            return;
        }
        if (World.Where == Where.Town)
        {
            Note("There's nothing to enter here.");
            return;
        }
        if (World.Aboard)
        {
            Note("Go ashore first.");
            return;
        }
        var site = Land.SiteAt(World.X, World.Y);
        if (site is null)
        {
            Note("There's nothing to enter here.");
            return;
        }
        World.WorldX = site.X;
        World.WorldY = site.Y;
        switch (site.Kind)
        {
            case SiteKind.Town or SiteKind.Village or SiteKind.Castle: EnterTown(site.Id); break;
            case SiteKind.Shrine: Shrine(site.Virtue!.Value); break;
            case SiteKind.Dungeon:
                if (site.Id == "undermere") UndermereDoor();
                else EnterDungeon(site.Id);
                break;
        }
    }

    // ───────────────────────────── moongates ─────────────────────────────

    internal void TakeGate()
    {
        var to = Land.Gates[World.Roan];
        Flash = 0.5;
        Play(Sfx.Gate);
        if (to == (World.X, World.Y))
        {
            Note("The moongate shimmers around you, and lets you go where you stand.");
            return;
        }
        World.X = to.X;
        World.Y = to.Y;
        Roamers.Clear();
        Note($"The moongate takes you! You step out at {to.X},{to.Y}.");
    }

    // ───────────────────────────── searching ─────────────────────────────

    public void Search()
    {
        AfterTurn();
        switch (World.Where)
        {
            case Where.World:
                if ((World.X, World.Y) == Land.Horn && !World.Keys.Contains("horn"))
                {
                    World.Keys.Add("horn");
                    Found("the Brass Horn", "Hauled up from the sea bed: a horn of green-crusted brass. One of the three keys.");
                    return;
                }
                if ((World.X, World.Y) == Land.Taper && !World.Keys.Contains("taper"))
                {
                    World.Keys.Add("taper");
                    Found("the Everflame Taper", "Under the marsh mud, a taper that is still burning. One of the three keys.");
                    return;
                }
                Note("You search, and find nothing.");
                return;
            case Where.Town:
                SearchTown();
                return;
            default:
                Note("You search the walls, and find nothing.");
                return;
        }
    }

    private bool Near((int X, int Y) p) => Math.Max(Math.Abs(p.X - World.X), Math.Abs(p.Y - World.Y)) <= 1;

    private void SearchTown()
    {
        var t = Town!;
        var v = t.Def.Virtue;
        if (v is { } virtue && t.Rune.X >= 0 && Near(t.Rune) && !World.Runes.Contains(virtue))
        {
            World.Runes.Add(virtue);
            Found($"the Rune of {virtue}", $"A small stone tablet carved with the sign of {virtue}. Its shrine will open to you now.");
            return;
        }
        if (t.Tome.X >= 0 && Near(t.Tome) && !World.Keys.Contains("tome"))
        {
            World.Keys.Add("tome");
            Found("the Tome of Hours", "A heavy book whose pages turn by themselves, very slowly. One of the three keys.");
            return;
        }
        var pa = t.PurseArea;
        if (pa.X0 >= 0 && World.X >= pa.X0 && World.X <= pa.X1 && World.Y >= pa.Y0 && World.Y <= pa.Y1 && !World.Has("purse"))
        {
            World.Flags.Add("purse");
            Note("You find a purse heavy with coins, lying in the grass.");
            Play(Sfx.Coin);
            Say("A purse", "Somebody's whole takings, by the weight of it. Wenna the fishwife was looking for a purse like this.");
            return;
        }
        foreach (var chest in t.Chests)
        {
            if (!Near(chest) || World.Has($"chest:{t.Id}:{chest.X},{chest.Y}")) continue;
            var p = new Panel { Title = "A chest" };
            p.Say("A chest at the foot of someone's bed. It isn't yours.");
            p.Add("Open it and take what's inside", () =>
            {
                Pop();
                World.Flags.Add($"chest:{t.Id}:{chest.X},{chest.Y}");
                int gold = Rng.Range(15, 50);
                World.Gold += gold;
                Note($"You take {gold} gold that isn't yours.");
                Play(Sfx.Coin);
                ChangeVirtue(Virtue.Candor, -3);
                ChangeVirtue(Virtue.Integrity, -3);
            });
            p.Add("Leave it alone", Pop);
            p.Back = Pop;
            Push(p);
            return;
        }
        Note("You search, and find nothing.");
    }

    /// <summary>A treasure of the quest is found: a little more Integrity, the first time.</summary>
    private void Found(string what, string text)
    {
        Play(Sfx.Chime);
        Flash = 0.4;
        Note($"You found {what}!");
        World.Note($"Found {what}.");
        ChangeVirtue(Virtue.Integrity, 5, quiet: true);
        Say("Found!", text);
    }

    // ───────────────────────────── virtue ─────────────────────────────

    /// <summary>A virtue grows or suffers. The party hears about it.</summary>
    public void ChangeVirtue(Virtue v, int delta, bool quiet = false)
    {
        int before = World[v];
        World[v] = before + delta;
        int after = World[v];
        if (after == before) return;
        if (delta > 0)
        {
            Note($"Your {v} grows.");
            if (!quiet) Play(Sfx.Chime);
            if (before < Virtues.Steadfast && after >= Virtues.Steadfast) Note($"You are steadfast in {v}! Go to its shrine.");
        }
        else
        {
            Note($"Your {v} suffers.");
            if (!quiet) Play(Sfx.Shame);
        }
    }

    // ───────────────────────────── the roaming things of the land ─────────────────────────────

    /// <summary>
    /// How dangerous the land is here: worse the further from the castle, but never more than the
    /// party's leader is ready for - a new adventurer meets only the weakest things, and a tier more
    /// opens up with every two levels.
    /// </summary>
    public int Tier()
    {
        var castle = Land.Site("brightholm");
        int d = Overworld.Dist(World.X, World.Y, castle.X, castle.Y);
        int t = d < 16 ? 2 : d < 32 ? 3 : 4;
        return Math.Clamp(Math.Min(t, 1 + (World.Leader.Level - 1) / 2), 1, 4);
    }

    private void MoveRoamers()
    {
        foreach (var r in Roamers.ToList())
        {
            int d = Overworld.Dist(r.X, r.Y, World.X, World.Y);
            if (d > 16)
            {
                Roamers.Remove(r);
                continue;
            }
            (int X, int Y) step;
            if (!r.Peaceful && d <= 9 && Rng.Chance(75))
            {
                int sx = Math.Sign(World.X - r.X), sy = Math.Sign(World.Y - r.Y);
                step = Math.Abs(World.X - r.X) > Math.Abs(World.Y - r.Y) || (sy == 0) ? (r.X + sx, r.Y) : (r.X, r.Y + sy);
                if (!CanRoam(r, step.X, step.Y)) step = sx != 0 && step.Y != r.Y ? (r.X + sx, r.Y) : (r.X, r.Y + sy);
            }
            else
            {
                var (dx, dy) = ((Dir)Rng.Next(4)).Step();
                step = (r.X + dx, r.Y + dy);
            }
            if ((step.X, step.Y) == (World.X, World.Y))
            {
                if (!r.Peaceful)
                {
                    Engage(r);
                    return;
                }
                continue;
            }
            if (CanRoam(r, step.X, step.Y)) (r.X, r.Y) = step;
        }
        // Something new comes into view, now and then.
        if (Roamers.Count < 3 && Rng.Chance(World.Aboard ? 5 : 7)) Spawn();
    }

    private bool CanRoam(Roamer r, int x, int y)
    {
        var t = Land[x, y];
        if (Terrain.IsSite(t) || Land.Gates.Contains((x, y)) || Roamers.Any(o => o != r && o.X == x && o.Y == y)) return false;
        if (World.HasShip && (x, y) == (World.ShipX, World.ShipY) && !World.Aboard) return false;
        return r.Sea ? Terrain.Sailable(t) : Terrain.Walkable(t);
    }

    private void Spawn()
    {
        bool sea = World.Aboard;
        int tier = Tier();
        List<Monster> kinds;
        if (!sea && Rng.Chance(15)) kinds = [Monsters.Get(Rng.Chance(50) ? "pilgrim" : "merchant")];
        else kinds = Monsters.Meet(sea ? Math.Max(3, tier) : tier, sea, deep: false);
        if (kinds.Count == 0) return;
        var kind = Rng.Pick(kinds);
        int count = kind.Tier == 0 ? Rng.Range(1, 3) : Rng.Range(1, Math.Min(8, World.Party.Count + 2));
        if (kind.Tier >= 4) count = Math.Max(1, count / 2);
        var group = Enumerable.Range(0, count).Select(_ => kind).ToList();
        // Sometimes a mixed band: archers with the brigands, a shaman with the goblins.
        if (kind.Id is "brigand" && tier >= 2 && Rng.Chance(50)) group.Add(Monsters.Get("archer"));
        if (kind.Id is "goblin" or "orc" && tier >= 3 && Rng.Chance(50)) group.Add(Monsters.Get("shaman"));
        var r = new Roamer { Group = group };
        for (int tries = 0; tries < 30; tries++)
        {
            double ang = Rng.Next(360) * Math.PI / 180;
            int dist = Rng.Range(5, 7);
            int x = World.X + (int)Math.Round(Math.Cos(ang) * dist), y = World.Y + (int)Math.Round(Math.Sin(ang) * dist);
            if (!CanRoam(r, x, y)) continue;
            r.X = x;
            r.Y = y;
            Roamers.Add(r);
            return;
        }
    }

    /// <summary>The party meets a band on the road: a fight, or for harmless folk, a choice.</summary>
    internal void Engage(Roamer r)
    {
        Roamers.Remove(r);
        if (r.Peaceful)
        {
            var who = r.Group[0];
            var p = new Panel { Title = who.Plural, Picture = who.Sprite, PictureTint = Colours.Parse(who.Tint) };
            p.Say(who.Id == "pilgrim" ? "Pilgrims on their way to a shrine. They bow, and step off the road to let you pass." : "A peddler and a mule, loaded with pots. The peddler raises a hand in greeting.");
            p.Add("Greet them and go on your way", () =>
            {
                Pop();
                Note("You exchange greetings and go on.");
                ChangeVirtue(Virtue.Fairness, 2);
            });
            p.Add("Attack them", () =>
            {
                Pop();
                Note("You fall on harmless travellers!");
                ChangeVirtue(Virtue.Fairness, -5);
                ChangeVirtue(Virtue.Mercy, -2);
                StartBattle(r.Group, GroundHere());
            });
            Push(p);
            return;
        }
        StartBattle(r.Group, GroundHere());
    }

    public Ground GroundHere()
    {
        if (World.Where == Where.Dungeon) return Ground.Dungeon;
        if (World.Aboard) return Ground.Sea;
        return Land[World.X, World.Y] switch
        {
            Tile.Forest => Ground.Forest,
            Tile.Hills => Ground.Hills,
            Tile.Swamp => Ground.Swamp,
            Tile.Sand => Ground.Sand,
            _ => Ground.Grass,
        };
    }

    // ───────────────────────────── the ship ─────────────────────────────

    private void FireCannon(Dir d)
    {
        var (dx, dy) = d.Step();
        Play(Sfx.Cannon);
        AfterTurn();
        for (int i = 1; i <= 4; i++)
        {
            int x = World.X + dx * i, y = World.Y + dy * i;
            if (Roamers.FirstOrDefault(r => r.X == x && r.Y == y) is { } r)
            {
                Roamers.Remove(r);
                Play(Sfx.Kill);
                Note($"The cannon smashes the {(r.Group.Count == 1 ? r.Group[0].Name : r.Group[0].Plural)}!");
                if (!r.Group[0].Evil) ChangeVirtue(Virtue.Fairness, -3);
                else World.Leader.Xp += r.Group.Sum(m => m.Xp) / 2;
                return;
            }
            if (!Terrain.Sailable(Land[x, y]) && Land[x, y] is Tile.Mountains or Tile.Forest) break;
        }
        Note("The cannon booms over empty water.");
    }

    // ───────────────────────────── torches and gems ─────────────────────────────

    private void LightTorch()
    {
        if (World.Torches <= 0)
        {
            Note("You have no torches.");
            return;
        }
        World.Torches--;
        World.Light = Math.Max(World.Light, 120);
        Play(Sfx.Fire);
        Note("You light a torch.");
    }

    private void PeerAtGem()
    {
        if (World.Gems <= 0)
        {
            Note("You have no gems to peer into.");
            return;
        }
        World.Gems--;
        Play(Sfx.Spell);
        var p = new Panel { Title = World.Where == Where.Dungeon ? "The gem shows this level" : "The gem shows the land", Kind = PanelKind.Gem, Back = Pop };
        p.Add("Close", Pop);
        Push(p);
    }

    // ───────────────────────────── when everyone falls ─────────────────────────────

    private void PartyLost()
    {
        if (InDemo)
        {
            EndDemo();
            return;
        }
        Mode = Mode.Play;
        Battle = null;
        CloseAll();
        World.Gold /= 2;
        World.Aboard = false;
        foreach (var c in World.Party)
        {
            c.Status = Status.Good;
            c.Hp = c.MaxHp;
            c.Mp = c.MaxMp;
        }
        var castle = Land.Site("brightholm");
        World.WorldX = castle.X;
        World.WorldY = castle.Y;
        EnterTown("brightholm");
        World.Y = 6;
        World.X = 15;
        Say("Darkness", "All goes dark... and then there is light, and a voice. \"Rest. You are in Castle Brightholm; my people found you.\" " +
            "Queen Isaura stands over you. Your wounds are healed, but half your gold is gone.");
    }
}
