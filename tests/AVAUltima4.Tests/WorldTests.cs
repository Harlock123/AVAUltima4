using AVAUltima4.Game;
using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Tests;

/// <summary>The land, the towns and the dungeons are laid out so that the quest can be done.</summary>
public sealed class WorldTests
{
    private static Overworld Land => Overworld.Instance;

    [Fact]
    public void The_land_comes_out_the_same_every_time()
    {
        Assert.Equal(Overworld.Build().Dump(), Overworld.Build().Dump());
    }

    [Fact]
    public void Every_town_shrine_dungeon_and_moongate_can_be_walked_to_from_the_castle_but_the_Undermere_cannot()
    {
        var castle = Land.Site("brightholm");
        var reach = Land.Region((x, y) => Terrain.Walkable(Land[x, y]), castle.X, castle.Y);
        foreach (var s in Land.Sites.Where(s => s.Id != "undermere")) Assert.Contains((s.X, s.Y), reach);
        foreach (var g in Land.Gates) Assert.Contains(g, reach);
        var isle = Land.Site("undermere");
        Assert.DoesNotContain((isle.X, isle.Y), reach);
        Assert.Equal(8, Land.Gates.Count);
        Assert.Equal(8, Land.Sites.Count(s => s.Kind == SiteKind.Shrine));
        Assert.Equal(5, Land.Sites.Count(s => s.Kind == SiteKind.Dungeon));
    }

    [Fact]
    public void The_ship_can_sail_from_its_mooring_to_the_isle_and_to_the_sunken_horn()
    {
        var sea = Land.Region((x, y) => Terrain.Sailable(Land[x, y]), Land.ShipStart.X, Land.ShipStart.Y);
        var isle = Land.Site("undermere");
        // Somewhere beside the isle's shore is open water the ship can reach.
        var shore = Land.Region((x, y) => Terrain.Walkable(Land[x, y]), isle.X, isle.Y);
        Assert.Contains(shore, p => Overworld.Neighbours(p.X, p.Y).Any(sea.Contains));
        Assert.Contains(Land.Horn, sea);
        Assert.Equal(Tile.Deep, Land[Land.Horn.X, Land.Horn.Y]);
        // The ship's mooring is beside Hearthwell.
        var port = Land.Site("hearthwell");
        Assert.Equal(1, Math.Abs(port.X - Land.ShipStart.X) + Math.Abs(port.Y - Land.ShipStart.Y));
    }

    [Fact]
    public void The_taper_is_buried_in_a_marsh_on_the_mainland()
    {
        var castle = Land.Site("brightholm");
        var reach = Land.Region((x, y) => Terrain.Walkable(Land[x, y]), castle.X, castle.Y);
        Assert.Equal(Tile.Swamp, Land[Land.Taper.X, Land.Taper.Y]);
        Assert.Contains(Land.Taper, reach);
    }

    /// <summary>Every square of a town reachable on foot from inside its gate (people standing in the way are walked around).</summary>
    private static HashSet<(int X, int Y)> Reachable(TownMap t)
    {
        var start = (TownMap.GateX, TownMap.GateY - 1);
        var seen = new HashSet<(int, int)> { start };
        var q = new Queue<(int X, int Y)>();
        q.Enqueue(start);
        while (q.Count > 0)
        {
            var (x, y) = q.Dequeue();
            foreach (var d in Enum.GetValues<Dir>())
            {
                var (dx, dy) = d.Step();
                var n = (x + dx, y + dy);
                if (seen.Contains(n) || !t.Inside(n.Item1, n.Item2) || !TownMap.Walkable(t[n.Item1, n.Item2])) continue;
                if (t.NpcAt(n.Item1, n.Item2) is { Still: true }) continue;
                seen.Add(n);
                q.Enqueue(n);
            }
        }
        return seen;
    }

    [Fact]
    public void In_every_town_each_shopkeeper_person_and_hiding_place_can_be_reached()
    {
        foreach (var def in People.Towns)
        {
            var t = TownMap.Fresh(def.Id);
            var reach = Reachable(t);
            foreach (var keeper in t.Npcs.Where(n => n.Shop is not null))
                Assert.True(Enum.GetValues<Dir>().Any(d => reach.Any(p => t.KeeperAcross(p.X, p.Y, d) == keeper)), $"{def.Id}: nobody can reach {keeper.Name}");
            foreach (var n in t.Npcs.Where(n => n.Shop is null && !n.Sheep))
                Assert.True(Overworld.Neighbours(n.X, n.Y).Any(reach.Contains), $"{def.Id}: {n.Name} at {n.X},{n.Y} can't be reached");
            if (def.Virtue is not null)
            {
                Assert.True(t.Rune.X >= 0, $"{def.Id} has no rune");
                Assert.True(reach.Any(p => Math.Max(Math.Abs(p.X - t.Rune.X), Math.Abs(p.Y - t.Rune.Y)) <= 1), $"{def.Id}: the rune can't be reached");
            }
            foreach (var shop in def.Lots.Where(l => l is "weapons" or "armour" or "food" or "reagents" or "healer" or "inn" or "guild" or "ship").Distinct())
                Assert.Contains(t.Npcs, n => n.Shop == shop);
        }
        var glass = TownMap.Fresh("glassmere");
        Assert.Contains(Reachable(glass), p => Math.Max(Math.Abs(p.X - glass.Tome.X), Math.Abs(p.Y - glass.Tome.Y)) <= 1);
    }

    [Fact]
    public void Every_person_written_down_is_placed_in_their_town()
    {
        foreach (var def in People.All)
            Assert.Contains(TownMap.Fresh(def.Town).Npcs, n => n.Def == def);
        // One companion in each virtue's town.
        foreach (var v in Virtues.All)
            Assert.Single(People.In(v.Town), n => n.Kind == NpcKind.Companion);
    }

    [Fact]
    public void Every_dungeon_level_joins_its_ladders_altars_and_seals()
    {
        foreach (var def in Dungeons.All)
        for (int level = 1; level <= Dungeons.Levels; level++)
        {
            var l = Dungeons.Level(def.Id, level);
            var reach = l.Distances(l.Up);
            if (level < Dungeons.Levels || def.Id == "undermere")
            {
                Assert.Contains(l.Down, reach.Keys);
                Assert.Equal(def.Id == "undermere" ? Cell.Seal : Cell.Down, l[l.Down.X, l.Down.Y]);
            }
            if (level > 1) Assert.Equal(Dungeons.Level(def.Id, level - 1).Down, l.Up);
            if (def.Stones.ContainsKey(level))
            {
                Assert.Contains(l.Altar, reach.Keys);
                Assert.Equal(Cell.Altar, l[l.Altar.X, l.Altar.Y]);
            }
        }
        // Each of the eight stones rests on exactly one altar.
        var stones = Dungeons.All.SelectMany(d => d.Stones.Values).ToList();
        Assert.Equal(8, stones.Distinct().Count());
        Assert.Equal(8, stones.Count);
    }

    [Fact]
    public void Every_clue_fills_in_from_the_land()
    {
        var g = Helpers.NewGame();
        foreach (var npc in People.All)
        foreach (var t in npc.Topics)
        {
            g.World.WorldX = Land.Site(npc.Town).X;
            g.World.WorldY = Land.Site(npc.Town).Y;
            var text = g.Fill(t.Reply);
            Assert.DoesNotContain("{", text);
        }
        Assert.Contains($"{Land.Horn.X},{Land.Horn.Y}", g.Fill("{horn}"));
    }

    [Fact]
    public void The_directions_people_give_point_the_right_way()
    {
        Assert.StartsWith("north of here", GameEngine.Direction(10, 10, 10, 5));
        Assert.StartsWith("south-east", GameEngine.Direction(10, 10, 15, 15));
        Assert.StartsWith("far to the west", GameEngine.Direction(60, 10, 10, 10));
    }

    [Fact]
    public void The_seer_has_one_question_for_every_pair_of_virtues()
    {
        Assert.Equal(28, People.Seer.Count);
        var pairs = People.Seer.Select(q => (Math.Min((int)q.A, (int)q.B), Math.Max((int)q.A, (int)q.B))).ToHashSet();
        Assert.Equal(28, pairs.Count);
    }

    [Fact]
    public void Every_sprite_is_sixteen_by_sixteen_and_every_figure_has_one()
    {
        foreach (var (name, rows) in Views.Sprites.All)
        {
            Assert.True(rows.Length == 16, name);
            Assert.All(rows, r => Assert.True(r.Length == 16, name));
        }
        foreach (var m in Monsters.All) Assert.True(Views.Sprites.All.ContainsKey(m.Sprite), m.Sprite);
        foreach (var n in People.All) Assert.True(Views.Sprites.All.ContainsKey(n.Sprite), n.Sprite);
        foreach (var c in Enum.GetValues<ClassKind>()) Assert.True(Views.Sprites.All.ContainsKey(c.ToString().ToLowerInvariant()));
    }

    [Fact]
    public void Every_spell_has_its_own_recipe()
    {
        var recipes = Spell.All.Select(s => string.Join(",", s.Recipe.OrderBy(r => r))).ToList();
        Assert.Equal(recipes.Count, recipes.Distinct().Count());
        foreach (var s in Spell.All) Assert.Equal(s, Spell.FromRecipe(s.Recipe.Reverse()));
    }
}
