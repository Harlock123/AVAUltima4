using AVAUltima4.Combat;
using AVAUltima4.Game;
using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Tests;

public sealed class GameplayTests
{
    private static Overworld Land => Overworld.Instance;

    private static TownNpc Npc(GameEngine g, string name) => g.Town!.Npcs.First(n => n.Name == name);

    /// <summary>Talks to someone in the current town and says a few words.</summary>
    private static Game.Panel TalkTo(GameEngine g, string name, params string[] words)
    {
        g.Talk(Npc(g, name));
        foreach (var w in words) g.Speak(w);
        return g.Top!;
    }

    private static string Last(Game.Panel p) => p.Text[^1];

    // ───────────────────────────── the seer and the start ─────────────────────────────

    [Fact]
    public void The_seer_asks_seven_questions_and_the_last_virtue_standing_chooses_the_calling()
    {
        var g = new GameEngine(3, Helpers.TempSaves());
        g.StartSeer("Ash");
        g.Press("Enter");
        int asked = 0;
        while (g.SeerQuestion is { } q)
        {
            asked++;
            // Always side with Integrity when it is offered, otherwise the first answer.
            g.Press(q.A == Virtue.Integrity || q.B != Virtue.Integrity ? "1" : "2");
        }
        Assert.Equal(7, asked);
        g.Press("Enter");
        Assert.Equal(Mode.Play, g.Mode);
        Assert.Equal(ClassKind.Paladin, g.World.Leader.Class);
        Assert.Equal("oathgate", g.Town!.Id);
        Assert.Equal(50, g.World[Virtue.Integrity]);
        Assert.Equal("Ash", g.World.Leader.Name);
    }

    [Fact]
    public void A_new_life_begins_inside_the_gate_of_the_path_s_town_with_a_starting_kit()
    {
        var g = Helpers.NewGame(Virtue.Candor);
        Assert.Equal(Where.Town, g.World.Where);
        Assert.Equal("glassmere", g.Town!.Id);
        Assert.Equal(ClassKind.Mage, g.World.Leader.Class);
        Assert.True(g.World.Food > 0 && g.World.Gold > 0);
        Assert.Contains("mend", g.World.Known);
        Assert.DoesNotContain("blaze", g.World.Known);
        // Walking out of the gate puts the party on the map, on the town's square.
        for (int i = 0; i < 2; i++) g.Press("Down");
        Assert.Equal(Where.World, g.World.Where);
        var town = Land.Site("glassmere");
        Assert.Equal((town.X, town.Y), (g.World.X, g.World.Y));
    }

    // ───────────────────────────── talking ─────────────────────────────

    [Fact]
    public void Only_the_first_four_letters_of_a_word_count()
    {
        Assert.True(GameEngine.Matches("MANTRA", "mant"));
        Assert.True(GameEngine.Matches("mantras", "mant"));
        Assert.True(GameEngine.Matches("JOBS", "job") == false);
        Assert.True(GameEngine.Matches("job", "job"));
        Assert.False(GameEngine.Matches("man", "mant"));
    }

    [Fact]
    public void People_answer_their_name_job_and_topics_and_write_clues_in_the_journal()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        var p = TalkTo(g, "Gerrit", "name");
        Assert.Contains("Gerrit", Last(p));
        Assert.Equal("Gerrit", p.Title);
        g.Speak("MANTRA");
        Assert.Contains("KUR", Last(p));
        Assert.Contains(g.World.Notes, n => n.Contains("KUR"));
        g.Speak("weather");
        Assert.Contains("can't help", Last(p));
        g.Speak("bye");
        Assert.Null(g.Top);
    }

    [Fact]
    public void A_question_is_asked_once_and_the_answer_moves_a_virtue()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        int before = g.World[Virtue.Daring];
        var p = TalkTo(g, "Gerrit", "dragon");
        Assert.Contains("dragon", Last(p));
        g.Speak("maybe");
        Assert.Contains("Yes or no", Last(p));
        g.Speak("yes");
        Assert.Equal(before + 5, g.World[Virtue.Daring]);
        g.Speak("dragon");
        Assert.Contains("already", Last(p));
        Assert.Equal(before + 5, g.World[Virtue.Daring]);
    }

    [Fact]
    public void Boasting_costs_Modesty_and_humility_earns_it()
    {
        var g = Helpers.NewGame(Virtue.Modesty);
        int before = g.World[Virtue.Modesty];
        TalkTo(g, "Old Hob", "hero", "yes");
        Assert.Equal(before - 5, g.World[Virtue.Modesty]);
        var h = Helpers.NewGame(Virtue.Modesty);
        TalkTo(h, "Old Hob", "hero", "no");
        Assert.Equal(before + 5, h.World[Virtue.Modesty]);
    }

    [Fact]
    public void Giving_to_a_beggar_earns_Mercy_once_per_visit()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        int mercy = g.World[Virtue.Mercy], gold = g.World.Gold;
        TalkTo(g, "Hob", "give", "yes", "give", "yes");
        Assert.Equal(mercy + 3, g.World[Virtue.Mercy]);
        Assert.Equal(gold - 10, g.World.Gold);
        g.CloseAll();
        g.LeaveTown();
        g.EnterTown("ironcrag");
        TalkTo(g, "Hob", "alms", "yes");
        Assert.Equal(mercy + 6, g.World[Virtue.Mercy]);
    }

    [Fact]
    public void A_companion_joins_only_someone_who_shows_their_town_s_virtue()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        g.World[Virtue.Daring] = 30;
        var p = TalkTo(g, "Brenna", "join");
        Assert.Contains("more Daring", Last(p));
        Assert.Single(g.World.Party);
        g.World[Virtue.Daring] = 45;
        g.Speak("join");
        Assert.Equal(2, g.World.Party.Count);
        Assert.Equal("Brenna", g.World.Party[1].Name);
        Assert.Equal(ClassKind.Fighter, g.World.Party[1].Class);
        Assert.DoesNotContain(g.Town!.Npcs, n => n.Name == "Brenna");
        g.CloseAll();
        g.LeaveTown();
        g.EnterTown("ironcrag");
        Assert.DoesNotContain(g.Town!.Npcs, n => n.Name == "Brenna");
    }

    [Fact]
    public void The_Queen_raises_those_who_have_earned_it_and_heals_on_request()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        g.CloseAll();
        g.LeaveTown();
        g.EnterTown("brightholm");
        g.World.Leader.Xp = 450;
        g.World.Leader.Hp = 5;
        var p = TalkTo(g, "Queen Isaura");
        Assert.Equal(4, g.World.Leader.Level);
        Assert.Equal(g.World.Leader.MaxHp, g.World.Leader.Hp);
        g.World.Leader.Hp = 3;
        g.Speak("heal");
        Assert.Equal(g.World.Leader.MaxHp, g.World.Leader.Hp);
        g.Speak("word");
        Assert.DoesNotContain(Virtues.LastWord, Last(p));
        foreach (var v in Enum.GetValues<Virtue>()) g.World.Elevated.Add(v);
        g.Speak("wholeness");
        Assert.Contains(Virtues.LastWord, Last(p));
    }

    // ───────────────────────────── searching and the treasures of the quest ─────────────────────────────

    [Fact]
    public void Searching_beside_the_hiding_place_finds_the_town_s_rune()
    {
        var g = Helpers.NewGame(Virtue.Mercy);
        int integrity = g.World[Virtue.Integrity];
        g.Press("S");
        Assert.Empty(g.World.Runes);
        Helpers.StandBeside(g, g.Town!.Rune);
        g.Press("S");
        Assert.Contains(Virtue.Mercy, g.World.Runes);
        Assert.Equal(integrity + 5, g.World[Virtue.Integrity]);
        g.CloseAll();
        g.Press("S");
        Assert.Single(g.World.Runes);
    }

    [Fact]
    public void Taking_from_a_stranger_s_chest_costs_Candor_and_Integrity()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        var chest = g.Town!.Chests[0];
        Helpers.StandBeside(g, chest);
        int candor = g.World[Virtue.Candor], integrity = g.World[Virtue.Integrity], gold = g.World.Gold;
        g.Press("S");
        g.Press("1");
        Assert.True(g.World.Gold > gold);
        Assert.Equal(candor - 3, g.World[Virtue.Candor]);
        Assert.Equal(integrity - 3, g.World[Virtue.Integrity]);
        // The chest is empty now.
        g.Press("S");
        Assert.Null(g.Top);
    }

    [Fact]
    public void The_tome_is_on_the_library_shelf_in_Glassmere()
    {
        var g = Helpers.NewGame(Virtue.Candor);
        Helpers.StandBeside(g, g.Town!.Tome);
        g.Press("S");
        Assert.Contains("tome", g.World.Keys);
    }

    [Fact]
    public void A_lost_purse_returned_earns_Integrity_and_kept_costs_Candor()
    {
        foreach (bool give in new[] { true, false })
        {
            var g = Helpers.NewGame(Virtue.Mercy);
            var area = g.Town!.PurseArea;
            // In a corner of the garden, away from the well where the rune hides.
            g.World.X = area.X0;
            g.World.Y = area.Y1;
            while (!TownMap.Walkable(g.Town[g.World.X, g.World.Y]) || g.Town.NpcAt(g.World.X, g.World.Y) is not null) g.World.X++;
            g.Press("S");
            Assert.True(g.World.Has("purse"));
            g.CloseAll();
            int integrity = g.World[Virtue.Integrity], candor = g.World[Virtue.Candor];
            TalkTo(g, "Wenna", "purse", give ? "yes" : "no");
            if (give) Assert.Equal(integrity + 5, g.World[Virtue.Integrity]);
            else Assert.Equal(candor - 5, g.World[Virtue.Candor]);
        }
    }

    [Fact]
    public void The_horn_is_found_from_a_ship_and_the_taper_in_the_marsh()
    {
        var g = Helpers.NewGame(Virtue.Mercy);
        Helpers.OnLand(g, Land.Taper.X, Land.Taper.Y);
        g.Press("S");
        Assert.Contains("taper", g.World.Keys);
        g.CloseAll();
        g.World.HasShip = true;
        g.World.Aboard = true;
        (g.World.X, g.World.Y) = (g.World.ShipX, g.World.ShipY) = Land.Horn;
        g.Press("S");
        Assert.Contains("horn", g.World.Keys);
    }

    // ───────────────────────────── shops ─────────────────────────────

    [Fact]
    public void Buying_a_weapon_trades_in_the_old_one()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        g.World.Gold = 500;
        g.OpenShop(g.Town!.Npcs.First(n => n.Shop == "weapons"));
        int sword = g.Top!.Options.FindIndex(o => o.Label.StartsWith("Sword"));
        g.Top.Selected = sword;
        g.Press("Enter");
        g.Press("1");
        Assert.Equal("sword", g.World.Leader.Weapon);
        Assert.Equal(500 - (260 - 10), g.World.Gold);
    }

    [Fact]
    public void A_mage_can_t_buy_a_sword()
    {
        var g = Helpers.NewGame(Virtue.Candor);
        Assert.False(Arms.CanUse(ClassKind.Mage, "sword"));
        Assert.True(Arms.CanUse(ClassKind.Mage, "staff"));
        Assert.True(Arms.CanUse(ClassKind.Fighter, "plate"));
        Assert.False(Arms.CanUse(ClassKind.Bard, "plate"));
    }

    [Fact]
    public void A_merchant_who_forgets_to_charge_is_a_test_of_Candor()
    {
        bool tested = false;
        for (int seed = 1; seed < 40 && !tested; seed++)
        {
            var g = Helpers.NewGame(Virtue.Candor, seed);
            g.OpenShop(g.Town!.Npcs.First(n => n.Shop == "reagents"));
            int candor = g.World[Virtue.Candor];
            for (int i = 0; i < 5 && !tested; i++)
            {
                int gold = g.World.Gold;
                g.Press("1");
                if (g.Top!.Title == "Alchemist Fenn" && g.Top.Options.Any(o => o.Label == "Pay anyway"))
                {
                    tested = true;
                    g.Press("1");
                    Assert.Equal(candor + 4, g.World[Virtue.Candor]);
                    Assert.True(g.World.Gold < gold);
                }
            }
        }
        Assert.True(tested);
    }

    [Fact]
    public void Giving_blood_at_a_healer_earns_Selflessness_once_a_visit()
    {
        var g = Helpers.NewGame(Virtue.Selflessness);
        int before = g.World[Virtue.Selflessness];
        g.OpenShop(g.Town!.Npcs.First(n => n.Shop == "healer"));
        g.Top!.Selected = g.Top.Options.FindIndex(o => o.Label == "Give blood");
        g.Press("Enter");
        Assert.Equal(before + 4, g.World[Virtue.Selflessness]);
        Assert.True(g.World.Leader.Hp < g.World.Leader.MaxHp);
        Assert.False(g.Top!.Options.First(o => o.Label == "Give blood").Enabled);
    }

    [Fact]
    public void The_ship_is_bought_in_Hearthwell_and_sails_only_on_water()
    {
        var g = Helpers.NewGame(Virtue.Mercy);
        g.World.Gold = 1000;
        g.OpenShop(g.Town!.Npcs.First(n => n.Shop == "ship"));
        g.Press("1");
        Assert.True(g.World.HasShip);
        Assert.Equal(Land.ShipStart, (g.World.ShipX, g.World.ShipY));
        g.CloseAll();
        g.LeaveTown();
        // Step onto the ship from the town's square.
        var port = Land.Site("hearthwell");
        var d = Enum.GetValues<Dir>().First(d => (port.X + d.Step().X, port.Y + d.Step().Y) == Land.ShipStart);
        g.Walk(d);
        Assert.True(g.World.Aboard);
        // Sail out to sea a little way, then back ashore.
        var sea = Enum.GetValues<Dir>().First(d => Terrain.Sailable(Land[g.World.X + d.Step().X, g.World.Y + d.Step().Y]));
        g.Walk(sea);
        Assert.True(g.World.Aboard);
        Assert.True(Terrain.Sailable(Land[g.World.X, g.World.Y]));
        Assert.Equal((g.World.X, g.World.Y), (g.World.ShipX, g.World.ShipY));
    }

    // ───────────────────────────── the moongates ─────────────────────────────

    [Fact]
    public void The_open_moongate_follows_the_pale_moon_and_the_red_moon_picks_where_it_leads()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        g.World.Turn = WorldState.SelisPeriod * 2 + WorldState.RoanPeriod * 5;
        Assert.Equal(Land.Gates[g.World.Selis], g.OpenGate);
        int roan = g.World.Roan;
        var gate = g.OpenGate;
        Helpers.OnLand(g, gate.X, gate.Y);
        g.TakeGate();
        Assert.Equal(Land.Gates[roan], (g.World.X, g.World.Y));
    }

    // ───────────────────────────── shrines ─────────────────────────────

    [Fact]
    public void A_shrine_opens_only_with_its_rune_and_raises_only_the_steadfast()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        var s = Land.Site("shrine-daring");
        Helpers.OnLand(g, s.X, s.Y);
        g.Press("Enter");
        Assert.Null(g.AtShrine);
        g.CloseAll();

        g.World.Runes.Add(Virtue.Daring);
        g.World[Virtue.Daring] = 60;
        int reverence = g.World[Virtue.Reverence];
        g.Press("Enter");
        Assert.Equal(Virtue.Daring, g.AtShrine);
        g.Chant("ORA");
        Assert.Empty(g.World.Elevated);
        Assert.Equal(reverence, g.World[Virtue.Reverence]);
        g.CloseAll();
        g.Press("Enter");
        g.Chant("kur");
        Assert.Empty(g.World.Elevated);
        Assert.Equal(reverence + 3, g.World[Virtue.Reverence]);
        Assert.Contains("growing", g.Top!.Text[0]);
        // Meditating again at the same shrine doesn't add more.
        g.CloseAll();
        g.Press("Enter");
        g.Chant("KUR");
        Assert.Equal(reverence + 3, g.World[Virtue.Reverence]);

        g.World[Virtue.Daring] = Virtues.Steadfast;
        g.CloseAll();
        g.Press("Enter");
        g.Chant("KUR");
        Assert.Contains(Virtue.Daring, g.World.Elevated);
    }

    // ───────────────────────────── magic ─────────────────────────────

    [Fact]
    public void Mixing_a_known_recipe_makes_a_dose_and_uses_one_of_each_reagent()
    {
        var g = Helpers.NewGame(Virtue.Candor);
        int doses = g.World.Doses.GetValueOrDefault("mend");
        int thistle = g.World.Reagents[(int)Reagent.Thistle];
        g.Mix([Reagent.Thistle, Reagent.Moonpetal]);
        Assert.Equal(doses + 1, g.World.Doses["mend"]);
        Assert.Equal(thistle - 1, g.World.Reagents[(int)Reagent.Thistle]);
    }

    [Fact]
    public void Mixing_by_hand_can_discover_a_recipe_or_waste_the_reagents()
    {
        var g = Helpers.NewGame(Virtue.Candor);
        g.World.Reagents[(int)Reagent.Nightcap] = 2;
        Assert.DoesNotContain("hush", g.World.Known);
        Assert.Equal("hush", g.Mix([Reagent.Nightcap, Reagent.Moonpetal])?.Id);
        Assert.Contains("hush", g.World.Known);
        Assert.Null(g.Mix([Reagent.Nightcap, Reagent.Cinder]));
        Assert.Equal(0, g.World.Reagents[(int)Reagent.Nightcap]);
    }

    [Fact]
    public void Spells_cast_in_the_field_heal_carry_and_light()
    {
        var g = Helpers.NewGame(Virtue.Candor);
        var mage = g.World.Leader;
        mage.Hp = 5;
        Assert.True(g.CastField(Spell.Get("mend"), mage, mage));
        Assert.True(mage.Hp > 5);

        g.World.Doses["wayfare"] = 1;
        g.World.Doses["return"] = 1;
        Helpers.OnLand(g, 20, 40);
        Assert.False(g.CastField(Spell.Get("return"), mage, null));
        Assert.True(g.CastField(Spell.Get("wayfare"), mage, null));
        var castle = Land.Site("brightholm");
        Assert.Equal((castle.X, castle.Y), (g.World.X, g.World.Y));
        mage.Mp = mage.MaxMp;
        g.World.WorldX = Land.Site("hollowroot").X;
        g.World.WorldY = Land.Site("hollowroot").Y;
        g.EnterDungeon("hollowroot");
        g.World.Level = 3;
        Assert.True(g.CastField(Spell.Get("return"), mage, null));
        Assert.Equal(Where.World, g.World.Where);
    }

    // ───────────────────────────── fighting ─────────────────────────────

    [Fact]
    public void A_strong_party_beats_a_band_of_goblins_and_gains_Daring_gold_and_experience()
    {
        var g = Helpers.NewGame(Virtue.Daring, 5);
        Helpers.Strong(g, 3);
        Helpers.OnLand(g, 50, 50);
        int daring = g.World[Virtue.Daring], gold = g.World.Gold;
        g.StartBattle([.. Enumerable.Repeat(Monsters.Get("goblin"), 4)], Ground.Grass);
        Assert.Equal(Outcome.Won, g.Battle!.RunToEnd());
        g.Update(0.1);
        Assert.Equal(Mode.Play, g.Mode);
        Assert.Equal(daring + 1, g.World[Virtue.Daring]);
        Assert.True(g.World.Gold > gold);
        Assert.True(g.World.Party.Sum(c => c.Xp) > 4 * Character.XpFor(3));
    }

    [Fact]
    public void Walking_off_the_field_runs_away_and_costs_Daring()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        Helpers.OnLand(g, 50, 50);
        int daring = g.World[Virtue.Daring];
        g.StartBattle([Monsters.Get("orc")], Ground.Grass);
        var b = g.Battle!;
        b.Instant = true;
        for (int i = 0; i < 40 && g.Mode == Mode.Battle; i++)
        {
            if (b.WaitingForPlayer) g.Press("Down");
            g.Update(0.05);
        }
        Assert.Equal(Mode.Play, g.Mode);
        Assert.True(b.Outcome is Outcome.Fled);
        Assert.Equal(daring - 2, g.World[Virtue.Daring]);
    }

    [Fact]
    public void Striking_a_creature_that_runs_away_costs_Mercy()
    {
        var world = new WorldState();
        var party = new List<Character> { Character.Companion("Ash", ClassKind.Fighter, 6, "sword", "plate") };
        var b = new Battle(party, [Monsters.Get("wolf")], Ground.Grass, world, new Rng(9)) { Instant = true };
        var wolf = b.Foes.First();
        wolf.Fleeing = true;
        b.Hurt(wolf, 1, 0, b.Party.First());
        Assert.Equal(1, b.StruckFleeing);
        // A wicked thing that runs is fair game.
        var b2 = new Battle(party, [Monsters.Get("orc")], Ground.Grass, world, new Rng(9)) { Instant = true };
        var orc = b2.Foes.First();
        orc.Fleeing = true;
        b2.Hurt(orc, 1, 0, b2.Party.First());
        Assert.Equal(0, b2.StruckFleeing);
    }

    [Fact]
    public void Harmless_travellers_greeted_are_a_small_act_of_Fairness_and_attacked_a_great_wrong()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        Helpers.OnLand(g, 50, 50);
        int fairness = g.World[Virtue.Fairness];
        g.Engage(new Roamer { X = 51, Y = 50, Group = [Monsters.Get("pilgrim")] });
        g.Press("1");
        Assert.Equal(fairness + 2, g.World[Virtue.Fairness]);
        g.Engage(new Roamer { X = 51, Y = 50, Group = [Monsters.Get("pilgrim")] });
        g.Press("2");
        Assert.Equal(fairness + 2 - 5, g.World[Virtue.Fairness]);
        Assert.Equal(Mode.Battle, g.Mode);
    }

    [Fact]
    public void Creatures_on_the_land_come_toward_the_party_and_start_a_fight()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        var castle = Land.Site("brightholm");
        Helpers.OnLand(g, castle.X, castle.Y + 1);
        g.Roamers.Add(new Roamer { X = castle.X, Y = castle.Y + 4, Group = [Monsters.Get("orc")] });
        for (int i = 0; i < 20 && g.Mode == Mode.Play; i++) g.Press("Space");
        Assert.Equal(Mode.Battle, g.Mode);
    }

    [Fact]
    public void Hush_puts_living_foes_to_sleep_but_not_the_dead()
    {
        var world = new WorldState();
        world.Doses["hush"] = 2;
        var mage = Character.Companion("Mariel", ClassKind.Mage, 5, "staff", "cloth");
        var b = new Battle([mage], [Monsters.Get("rat"), Monsters.Get("rat"), Monsters.Get("skeleton")], Ground.Grass, world, new Rng(2)) { Instant = true };
        Assert.True(b.Cast("hush"));
        Assert.True(b.Foes.Count(f => f.Asleep > 0) >= 1);
        Assert.Equal(0, b.Foes.First(f => f.Undead).Asleep);
        Assert.Equal(1, world.Doses["hush"]);
    }

    [Fact]
    public void A_lone_new_fighter_usually_beats_a_couple_of_rats_and_a_seasoned_company_survives_the_deep()
    {
        int wins = 0;
        for (int seed = 0; seed < 40; seed++)
        {
            var b = new Battle([Character.Make("Ash", ClassKind.Fighter)], [Monsters.Get("rat"), Monsters.Get("rat")], Ground.Grass, new WorldState(), new Rng((ulong)seed + 1));
            if (b.RunToEnd() == Outcome.Won) wins++;
        }
        Assert.InRange(wins, 28, 40);
        int deep = 0;
        for (int seed = 0; seed < 30; seed++)
        {
            var party = new List<Character>
            {
                Character.Companion("A", ClassKind.Fighter, 7, "starblade", "starplate"),
                Character.Companion("B", ClassKind.Paladin, 7, "halberd", "plate"),
                Character.Companion("C", ClassKind.Ranger, 7, "crossbow", "runechain"),
                Character.Companion("D", ClassKind.Fighter, 7, "sword", "plate"),
                Character.Companion("E", ClassKind.Tinker, 6, "axe", "chain"),
                Character.Companion("F", ClassKind.Bard, 6, "bow", "leather"),
            };
            var foes = Enumerable.Repeat(Monsters.Get("knight"), 3).ToList();
            var b = new Battle(party, foes, Ground.Dungeon, new WorldState(), new Rng((ulong)seed + 100));
            if (b.RunToEnd() == Outcome.Won) deep++;
        }
        Assert.InRange(deep, 18, 30);
    }

    // ───────────────────────────── dungeons ─────────────────────────────

    [Fact]
    public void Ladders_go_down_and_up_and_out_into_the_daylight()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        var site = Land.Site("hollowroot");
        Helpers.OnLand(g, site.X, site.Y);
        g.Press("Enter");
        Assert.Equal(Where.Dungeon, g.World.Where);
        var l1 = g.Dungeon!;
        (g.World.X, g.World.Y) = l1.Down;
        g.Press("Enter");
        Assert.Equal(2, g.World.Level);
        Assert.Equal(g.Dungeon!.Up, (g.World.X, g.World.Y));
        g.Press("Enter");
        Assert.Equal(1, g.World.Level);
        (g.World.X, g.World.Y) = l1.Up;
        g.Press("Enter");
        Assert.Equal(Where.World, g.World.Where);
        Assert.Equal((site.X, site.Y), (g.World.X, g.World.Y));
    }

    [Fact]
    public void Walking_the_passages_turns_and_steps_and_bumps_the_walls()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        g.EnterDungeon("hollowroot");
        var facing = g.World.Facing;
        g.Press("Right");
        Assert.Equal(facing.Right(), g.World.Facing);
        g.Press("Left");
        var lvl = g.Dungeon!;
        var (x, y) = (g.World.X, g.World.Y);
        g.Press("Up");
        Assert.NotEqual((x, y), (g.World.X, g.World.Y));
        // Face a wall and walk into it: nothing moves.
        g.World.Facing = Enum.GetValues<Dir>().First(d => !DungeonLevel.Open(lvl[g.World.X + d.Step().X, g.World.Y + d.Step().Y]));
        var at = (g.World.X, g.World.Y);
        g.Press("Up");
        Assert.True(g.Mode == Mode.Battle || at == (g.World.X, g.World.Y));
    }

    [Fact]
    public void The_altar_s_guardians_must_be_beaten_for_its_stone()
    {
        var g = Helpers.NewGame(Virtue.Daring, 4);
        Helpers.Strong(g, 7);
        g.EnterDungeon("hollowroot");
        g.World.Level = 2;
        (g.World.X, g.World.Y) = g.Dungeon!.Altar;
        g.Altar();
        Assert.Equal(Mode.Battle, g.Mode);
        Assert.Equal(Outcome.Won, g.Battle!.RunToEnd());
        g.Update(0.1);
        Assert.Contains(Virtue.Mercy, g.World.Stones);
        g.CloseAll();
        g.Altar();
        Assert.Equal(Mode.Play, g.Mode);
    }

    [Fact]
    public void Chests_traps_and_fountains_do_their_work_once()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        Helpers.Strong(g);
        g.EnterDungeon("mirrormine");
        bool chest = false;
        for (int level = 1; level <= Dungeons.Levels && !chest; level++)
        {
            g.World.Level = level;
            var lvl = g.Dungeon!;
            for (int y = 0; y < DungeonLevel.Size && !chest; y++)
            for (int x = 0; x < DungeonLevel.Size && !chest; x++)
            {
                if (lvl[x, y] != Cell.Chest) continue;
                // Step onto it from an open neighbour.
                var from = Enum.GetValues<Dir>().First(d => DungeonLevel.Open(lvl[x - d.Step().X, y - d.Step().Y]));
                (g.World.X, g.World.Y) = (x - from.Step().X, y - from.Step().Y);
                int gold = g.World.Gold;
                g.DungeonStep(from);
                if (g.Mode == Mode.Battle) return;
                Assert.True(g.World.Gold > gold);
                gold = g.World.Gold;
                g.DungeonStep(from.Back());
                if (g.Mode == Mode.Battle) return;
                g.DungeonStep(from);
                Assert.True(g.Mode == Mode.Battle || g.World.Gold == gold);
                chest = true;
            }
        }
        Assert.True(chest);
    }

    [Fact]
    public void The_arms_no_smith_sells_lie_in_deep_chests()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        Helpers.Strong(g);
        foreach (var d in Dungeons.All.Where(d => d.Treasure is not null))
        foreach (var (level, item) in d.Treasure!)
        {
            g.EnterDungeon(d.Id);
            g.World.Level = level;
            var lvl = g.Dungeon!;
            var chest = lvl.Chests().First();
            var from = Enum.GetValues<Dir>().First(dir => DungeonLevel.Open(lvl[chest.X - dir.Step().X, chest.Y - dir.Step().Y]));
            (g.World.X, g.World.Y) = (chest.X - from.Step().X, chest.Y - from.Step().Y);
            int traps = 0;
            while (g.Mode == Mode.Play && (g.World.X, g.World.Y) != chest && traps++ < 5)
            {
                g.CloseAll();
                g.DungeonStep(from);
            }
            if (g.Mode == Mode.Battle)
            {
                g.Battle!.RunToEnd();
                g.Update(0.1);
                g.CloseAll();
                (g.World.X, g.World.Y) = (chest.X - from.Step().X, chest.Y - from.Step().Y);
                g.DungeonStep(from);
            }
            Assert.Contains(g.World.Party, c => c.Weapon == item || c.Armour == item);
            g.CloseAll();
        }
    }

    // ───────────────────────────── the Undermere ─────────────────────────────

    [Fact]
    public void The_door_of_the_Undermere_opens_to_the_keys_in_their_order()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        foreach (var k in GameEngine.KeyOrder) g.World.Keys.Add(k);
        var isle = Land.Site("undermere");
        Helpers.OnLand(g, isle.X, isle.Y);
        g.Press("Enter");
        g.UseKey("tome");
        g.UseKey("horn");
        g.UseKey("taper");
        Assert.False(g.World.Has("door"));
        g.UseKey("horn");
        g.UseKey("tome");
        g.UseKey("taper");
        Assert.True(g.World.Has("door"));
        g.Press("Enter");
        Assert.Equal(Where.Dungeon, g.World.Where);
        Assert.Equal("undermere", g.World.Place);
    }

    [Fact]
    public void Each_seal_wants_its_two_stones()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        g.EnterDungeon("undermere");
        (g.World.X, g.World.Y) = g.Dungeon!.Down;
        g.Seal();
        Assert.False(g.Top!.Options[0].Enabled);
        g.CloseAll();
        g.World.Stones.Add(Virtue.Candor);
        g.World.Stones.Add(Virtue.Mercy);
        g.Seal();
        g.Press("1");
        Assert.True(g.World.Has("seal:1"));
        g.CloseAll();
        g.Press("Enter");
        Assert.Equal(2, g.World.Level);
    }

    [Fact]
    public void The_Codex_asks_its_questions_and_casts_out_a_wrong_answer()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        g.EnterDungeon("undermere");
        g.World.Level = 4;
        g.World.WorldX = Land.Site("undermere").X;
        g.World.WorldY = Land.Site("undermere").Y;
        g.Codex();
        Assert.Equal(0, g.CodexStep);
        g.CloseAll();
        foreach (var v in Enum.GetValues<Virtue>()) g.World.Elevated.Add(v);
        g.Codex();
        g.Press("Enter");
        g.AnswerCodex("Candor");
        g.AnswerCodex("Mercy");
        Assert.Equal(2, g.CodexStep);
        g.AnswerCodex("Valour");
        Assert.Equal(Where.World, g.World.Where);
        Assert.Equal(0, g.CodexStep);

        g.CloseAll();
        g.Codex();
        g.Press("Enter");
        foreach (var v in Virtues.All) g.AnswerCodex(v.Virtue.ToString().ToLowerInvariant());
        foreach (var p in Virtues.Principles) g.AnswerCodex(p.Principle.ToString());
        g.AnswerCodex("Wholeness");
        Assert.Equal(Mode.Ending, g.Mode);
        Assert.True(g.World.Has("exemplar"));
    }

    [Fact]
    public void The_whole_quest_can_be_done_with_what_people_tell_you()
    {
        var g = Helpers.NewGame(Virtue.Daring, 11);
        var mantras = new Dictionary<Virtue, string>();
        foreach (var info in Virtues.All)
        {
            // The rune, from its hiding place in the town.
            g.CloseAll();
            g.World.WorldX = Land.Site(info.Town).X;
            g.World.WorldY = Land.Site(info.Town).Y;
            g.EnterTown(info.Town);
            Helpers.StandBeside(g, g.Town!.Rune);
            g.Press("S");
            Assert.Contains(info.Virtue, g.World.Runes);
            // The mantra, from whoever in town knows it.
            g.CloseAll();
            var teller = People.In(info.Town).First(n => n.Topics.Any(t => t.Keys.StartsWith("mant")));
            var p = TalkTo(g, teller.Name, "mantra");
            var m = System.Text.RegularExpressions.Regex.Match(Last(p), $"mantra of {info.Virtue} is ([A-Z]+)");
            Assert.True(m.Success, Last(p));
            mantras[info.Virtue] = m.Groups[1].Value;
            g.CloseAll();
        }
        Helpers.Strong(g, 7);
        foreach (var c in g.World.Party) { c.Weapon = "starblade"; c.Armour = "starplate"; }
        // The eight stones, from the altars of the four dungeons.
        foreach (var d in Dungeons.All.Where(d => d.Stones.Count > 0))
        foreach (var (level, v) in d.Stones)
        {
            g.EnterDungeon(d.Id);
            g.World.Level = level;
            (g.World.X, g.World.Y) = g.Dungeon!.Altar;
            foreach (var c in g.World.Party) { c.Revive(); c.Hp = c.MaxHp; }
            g.Altar();
            var guardians = Battle.Describe(g.Battle!.Foes.Select(f => f.Kind!).ToList());
            Assert.True(g.Battle.RunToEnd() == Outcome.Won, $"{d.Name} level {level}: lost to {guardians}");
            g.Update(0.1);
            g.CloseAll();
            Assert.Contains(v, g.World.Stones);
        }
        // The three keys.
        g.World.WorldX = Land.Site("glassmere").X;
        g.World.WorldY = Land.Site("glassmere").Y;
        g.EnterTown("glassmere");
        Helpers.StandBeside(g, g.Town!.Tome);
        g.Press("S");
        g.CloseAll();
        Helpers.OnLand(g, Land.Taper.X, Land.Taper.Y);
        g.Press("S");
        g.CloseAll();
        g.World.HasShip = true;
        g.World.Aboard = true;
        (g.World.X, g.World.Y) = (g.World.ShipX, g.World.ShipY) = Land.Horn;
        g.Press("S");
        g.CloseAll();
        Assert.Equal(3, g.World.Keys.Count);
        // Steadfast in every virtue, and raised at every shrine with the mantra people gave.
        g.World.Aboard = false;
        foreach (var info in Virtues.All)
        {
            g.World[info.Virtue] = Virtues.Steadfast;
            var shrine = Land.Site("shrine-" + info.Virtue.ToString().ToLowerInvariant());
            Helpers.OnLand(g, shrine.X, shrine.Y);
            g.Press("Enter");
            g.Chant(mantras[info.Virtue]);
            Assert.Contains(info.Virtue, g.World.Elevated);
        }
        // The Queen's last word.
        g.CloseAll();
        g.EnterTown("brightholm");
        var q = TalkTo(g, "Queen Isaura", "word");
        var word = System.Text.RegularExpressions.Regex.Match(Last(q), "together is ([A-Z]+)").Groups[1].Value;
        Assert.Equal(Virtues.LastWord, word);
        g.CloseAll();
        // Down through the door and the four seals, to the Codex.
        var isle = Land.Site("undermere");
        Helpers.OnLand(g, isle.X, isle.Y);
        g.World.WorldX = isle.X;
        g.World.WorldY = isle.Y;
        g.Press("Enter");
        foreach (var k in GameEngine.KeyOrder) g.UseKey(k);
        g.Press("Enter");
        Assert.Equal("undermere", g.World.Place);
        for (int level = 1; level <= Dungeons.Levels; level++)
        {
            Assert.Equal(level, g.World.Level);
            (g.World.X, g.World.Y) = g.Dungeon!.Down;
            g.Seal();
            g.Press("1");
            if (level < Dungeons.Levels)
            {
                g.CloseAll();
                g.Press("Enter");
            }
        }
        g.Press("Enter");
        foreach (var v in Virtues.All) g.AnswerCodex(v.Virtue.ToString());
        foreach (var pr in Virtues.Principles) g.AnswerCodex(pr.Principle.ToString());
        g.AnswerCodex(word);
        Assert.Equal(Mode.Ending, g.Mode);
    }

    // ───────────────────────────── time, death and saving ─────────────────────────────

    [Fact]
    public void Walking_eats_food_and_hunger_hurts()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        Helpers.OnLand(g, 50, 50);
        g.World.Food = 3;
        int hp = g.World.Leader.Hp;
        for (int i = 0; i < 40; i++) g.AfterTurn();
        Assert.Equal(0, g.World.Food);
        Assert.True(g.World.Leader.Hp < hp);
    }

    [Fact]
    public void When_everyone_falls_the_party_wakes_in_the_castle_poorer()
    {
        var g = Helpers.NewGame(Virtue.Daring);
        Helpers.OnLand(g, 50, 50);
        g.World.Gold = 200;
        g.StartBattle([.. Enumerable.Repeat(Monsters.Get("knight"), 4)], Ground.Grass);
        Assert.Equal(Outcome.Lost, g.Battle!.RunToEnd());
        g.Update(0.1);
        Assert.Equal(Mode.Play, g.Mode);
        Assert.Equal("brightholm", g.Town!.Id);
        Assert.Equal(100, g.World.Gold);
        Assert.True(g.World.Leader.Alive);
    }

    [Fact]
    public void A_journey_saves_and_loads()
    {
        var g = Helpers.NewGame(Virtue.Fairness);
        g.World.Runes.Add(Virtue.Fairness);
        g.World[Virtue.Mercy] = 61;
        g.World.Doses["mend"] = 7;
        g.World.Notes.Add("a clue");
        g.Saves.Save(2, g.World);
        var h = new GameEngine(9, g.Saves);
        Assert.True(h.Load(2));
        Assert.Equal(Mode.Play, h.Mode);
        Assert.Equal("evenholt", h.Town!.Id);
        Assert.Contains(Virtue.Fairness, h.World.Runes);
        Assert.Equal(61, h.World[Virtue.Mercy]);
        Assert.Equal(7, h.World.Doses["mend"]);
        Assert.Equal(ClassKind.Druid, h.World.Leader.Class);
        Assert.Contains("a clue", h.World.Notes);
        Assert.StartsWith("Slot 2: Ash", h.Saves.Describe(2));
    }

    [Fact]
    public void The_demo_wanders_the_land_and_fights_and_any_key_ends_it()
    {
        var g = new GameEngine(5, Helpers.TempSaves());
        g.Update(GameEngine.DemoAfter + 0.1);
        Assert.True(g.InDemo);
        var start = (g.World.X, g.World.Y);
        bool fought = false, moved = false;
        for (int i = 0; i < 60 * 60 && g.InDemo; i++)
        {
            g.Update(1 / 60.0);
            if (g.Mode == Mode.Battle) fought = true;
            if ((g.World.X, g.World.Y) != start) moved = true;
        }
        Assert.True(moved);
        Assert.True(fought);
        if (!g.InDemo) g.Update(GameEngine.DemoAfter + 0.1);
        g.Press("Space");
        Assert.False(g.InDemo);
        Assert.Equal(Mode.Title, g.Mode);
    }
}
