namespace AVAUltima4.World;

/// <summary>What one square of the land of Merovale is.</summary>
public enum Tile : byte
{
    Deep, Water, Shallows, Sand, Grass, Brush, Forest, Hills, Mountains, Swamp,
    Town, Castle, Shrine, Dungeon, Village,
}

public static class Terrain
{
    /// <summary>Can be walked on.</summary>
    public static bool Walkable(Tile t) => t is not (Tile.Deep or Tile.Water or Tile.Shallows or Tile.Mountains);

    /// <summary>Can be sailed on.</summary>
    public static bool Sailable(Tile t) => t is Tile.Deep or Tile.Water or Tile.Shallows;

    public static bool IsWater(Tile t) => t is Tile.Deep or Tile.Water or Tile.Shallows;

    /// <summary>Hard going: each step through it takes two turns.</summary>
    public static bool Slow(Tile t) => t is Tile.Forest or Tile.Hills or Tile.Swamp;

    /// <summary>Hides what is beyond it (a forest or a range of hills, seen from outside).</summary>
    public static bool BlocksSight(Tile t) => t is Tile.Forest or Tile.Mountains;

    public static bool IsSite(Tile t) => t is Tile.Town or Tile.Castle or Tile.Shrine or Tile.Dungeon or Tile.Village;

    public static string Name(Tile t) => t switch
    {
        Tile.Deep => "deep water",
        Tile.Water => "open water",
        Tile.Shallows => "shallows",
        Tile.Sand => "sand",
        Tile.Grass => "grassland",
        Tile.Brush => "scrub",
        Tile.Forest => "forest",
        Tile.Hills => "hills",
        Tile.Mountains => "mountains",
        Tile.Swamp => "swamp",
        Tile.Town => "a town",
        Tile.Castle => "a castle",
        Tile.Shrine => "a shrine",
        Tile.Dungeon => "a dungeon",
        Tile.Village => "a village",
        _ => "",
    };
}

/// <summary>A compass direction on the map.</summary>
public enum Dir { North, East, South, West }

public static class Dirs
{
    public static (int X, int Y) Step(this Dir d) => d switch
    {
        Dir.North => (0, -1),
        Dir.East => (1, 0),
        Dir.South => (0, 1),
        _ => (-1, 0),
    };

    public static Dir Right(this Dir d) => (Dir)(((int)d + 1) % 4);
    public static Dir Left(this Dir d) => (Dir)(((int)d + 3) % 4);
    public static Dir Back(this Dir d) => (Dir)(((int)d + 2) % 4);

    public static Dir? FromKey(string key) => key switch
    {
        "Up" => Dir.North,
        "Right" => Dir.East,
        "Down" => Dir.South,
        "Left" => Dir.West,
        _ => null,
    };
}
