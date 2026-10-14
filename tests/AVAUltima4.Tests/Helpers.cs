using AVAUltima4.Game;
using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Tests;

internal static class Helpers
{
    /// <summary>A folder for saved games that isn't the user's own.</summary>
    public static SaveStore TempSaves() =>
        new(Path.Combine(Path.GetTempPath(), "avaultima4-tests-" + Guid.NewGuid().ToString("N")));

    /// <summary>A new game, past the seer, standing inside the gate of the path's town.</summary>
    public static GameEngine NewGame(Virtue path = Virtue.Daring, int seed = 1)
    {
        var g = new GameEngine(seed, TempSaves());
        g.Begin("Ash", path);
        g.CloseAll();
        return g;
    }

    /// <summary>Puts the party on the open land at a square.</summary>
    public static void OnLand(GameEngine g, int x, int y)
    {
        g.CloseAll();
        if (g.World.Where == Where.Town) g.LeaveTown();
        g.World.Where = Where.World;
        g.World.X = x;
        g.World.Y = y;
        g.Roamers.Clear();
    }

    /// <summary>Walks the party in town to a square next to a spot (searching or talking range).</summary>
    public static void StandBeside(GameEngine g, (int X, int Y) spot)
    {
        var t = g.Town!;
        foreach (var (dx, dy) in new[] { (0, 1), (1, 0), (-1, 0), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1) })
        {
            int x = spot.X + dx, y = spot.Y + dy;
            if (TownMap.Walkable(t[x, y]) && t.NpcAt(x, y) is null)
            {
                g.World.X = x;
                g.World.Y = y;
                return;
            }
        }
        throw new InvalidOperationException($"Nowhere to stand beside {spot}");
    }

    /// <summary>Adds a strong companion party for fights.</summary>
    public static void Strong(GameEngine g, int level = 5)
    {
        g.World.Party.Clear();
        g.World.Party.Add(Character.Companion("Ash", ClassKind.Fighter, level, "sword", "plate"));
        g.World.Party.Add(Character.Companion("Brenna", ClassKind.Fighter, level, "axe", "chain"));
        g.World.Party.Add(Character.Companion("Sela", ClassKind.Ranger, level, "bow", "chain"));
        g.World.Party.Add(Character.Companion("Roderic", ClassKind.Paladin, level, "sword", "plate"));
    }
}
