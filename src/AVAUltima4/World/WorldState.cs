using System.Text.Json;
using System.Text.Json.Serialization;
using AVAUltima4.Rules;

namespace AVAUltima4.World;

public enum Where { World, Town, Dungeon }

/// <summary>
/// Everything a saved game remembers: the party and what it carries, the eight virtues, the quest's
/// treasures, where the party is, the ship, the clock (which also turns the moons), and every flag
/// set along the way.
/// </summary>
public sealed class WorldState
{
    public List<Character> Party { get; set; } = [];
    public int Gold { get; set; }
    public int Food { get; set; }
    public int Torches { get; set; }
    public int Gems { get; set; }
    /// <summary>Steps of light left from a torch or a Glow spell.</summary>
    public int Light { get; set; }
    public int[] Reagents { get; set; } = new int[8];
    /// <summary>Mixed doses of each spell, ready to cast.</summary>
    public Dictionary<string, int> Doses { get; set; } = [];
    /// <summary>Recipes in the spellbook.</summary>
    public HashSet<string> Known { get; set; } = [];

    public int[] Virtues { get; set; } = new int[8];
    public HashSet<Virtue> Elevated { get; set; } = [];
    public HashSet<Virtue> Runes { get; set; } = [];
    public HashSet<Virtue> Stones { get; set; } = [];
    public HashSet<string> Keys { get; set; } = [];

    public Where Where { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    /// <summary>Where on the map the party was before going into a town or dungeon.</summary>
    public int WorldX { get; set; }
    public int WorldY { get; set; }
    public string Place { get; set; } = "";
    public int Level { get; set; }
    public Dir Facing { get; set; } = Dir.North;

    public bool HasShip { get; set; }
    public bool Aboard { get; set; }
    public int ShipX { get; set; }
    public int ShipY { get; set; }
    public int Hull { get; set; } = 50;

    /// <summary>Moves made since the start: the clock that turns the moons and empties the larder.</summary>
    public int Turn { get; set; }
    public HashSet<string> Flags { get; set; } = [];
    /// <summary>Flags that last only until the party next comes into a town (gifts to beggars and the like).</summary>
    public HashSet<string> Visit { get; set; } = [];
    public List<string> Notes { get; set; } = [];
    public string LastShrine { get; set; } = "";
    public ulong Seed { get; set; }
    public DateTime Saved { get; set; }

    [JsonIgnore] public Character Leader => Party[0];
    [JsonIgnore] public IEnumerable<Character> Living => Party.Where(c => c.Alive);

    public bool Has(string flag) => Flags.Contains(flag);

    public int this[Virtue v]
    {
        get => Virtues[(int)v];
        set => Virtues[(int)v] = Math.Clamp(value, 0, Rules.Virtues.Max);
    }

    public bool Note(string text)
    {
        if (Notes.Contains(text)) return false;
        Notes.Add(text);
        return true;
    }

    // ───────────── the moons ─────────────

    /// <summary>Steps for the pale moon, Selis, to move on a phase; red Roan is three times slower.</summary>
    public const int SelisPeriod = 12;
    public const int RoanPeriod = 36;

    [JsonIgnore] public int Selis => Turn / SelisPeriod % 8;
    [JsonIgnore] public int Roan => Turn / RoanPeriod % 8;

    public static string PhaseName(int phase) => phase switch
    {
        0 => "new", 1 => "waxing crescent", 2 => "first quarter", 3 => "waxing gibbous",
        4 => "full", 5 => "waning gibbous", 6 => "last quarter", _ => "waning crescent",
    };

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    public static WorldState FromJson(string json) => JsonSerializer.Deserialize<WorldState>(json, Options)
        ?? throw new InvalidDataException("Not a saved game.");
}
