namespace AVAUltima4.Rules;

/// <summary>The eight virtues of Merovale. Each is born of one, two, all or none of the three principles.</summary>
public enum Virtue { Candor, Mercy, Daring, Fairness, Selflessness, Integrity, Reverence, Modesty }

/// <summary>The three principles the virtues grow from.</summary>
[Flags]
public enum Principle { None = 0, Clarity = 1, Kindness = 2, Courage = 4 }

/// <summary>
/// What a virtue is: the principles it comes from, the mantra chanted at its shrine, the town that
/// keeps it and the calling that town trains, and the colour of its stone.
/// </summary>
public sealed record VirtueInfo(
    Virtue Virtue, Principle Principles, string Mantra, string Town, ClassKind Class, string Stone, uint StoneColour,
    string Meaning, string Riddle);

public static class Virtues
{
    /// <summary>A virtue at this or above, together with its rune and mantra, lets its shrine elevate you.</summary>
    public const int Steadfast = 75;
    public const int Max = 99;

    public static readonly IReadOnlyList<VirtueInfo> All =
    [
        new(Virtue.Candor, Principle.Clarity, "VEL", "glassmere", ClassKind.Mage, "Blue", 0xFF3070E0,
            "Candor is speaking and living the truth, even when a lie would be easier.",
            "What virtue is born of Clarity alone?"),
        new(Virtue.Mercy, Principle.Kindness, "ORA", "hearthwell", ClassKind.Bard, "Yellow", 0xFFF0D040,
            "Mercy is kindness to those who suffer, and to those who are beaten.",
            "What virtue is born of Kindness alone?"),
        new(Virtue.Daring, Principle.Courage, "KUR", "ironcrag", ClassKind.Fighter, "Red", 0xFFD03030,
            "Daring is facing what must be faced, and not running from it.",
            "What virtue is born of Courage alone?"),
        new(Virtue.Fairness, Principle.Clarity | Principle.Kindness, "SEN", "evenholt", ClassKind.Druid, "Green", 0xFF40B050,
            "Fairness is giving every creature its due: no more harm than it deserves, and no less help.",
            "What virtue is born of Clarity and Kindness together?"),
        new(Virtue.Selflessness, Principle.Kindness | Principle.Courage, "TAH", "tallowford", ClassKind.Tinker, "Orange", 0xFFF08020,
            "Selflessness is giving of yourself, your time, your coin and your blood, so that others may have.",
            "What virtue is born of Kindness and Courage together?"),
        new(Virtue.Integrity, Principle.Clarity | Principle.Courage, "DUN", "oathgate", ClassKind.Paladin, "Purple", 0xFF9040C0,
            "Integrity is keeping your word and finishing what you begin, though no one is watching.",
            "What virtue is born of Clarity and Courage together?"),
        new(Virtue.Reverence, Principle.Clarity | Principle.Kindness | Principle.Courage, "ELO", "starfall", ClassKind.Ranger, "White", 0xFFF0F0F0,
            "Reverence is the quiet in which all three principles are heard at once.",
            "What virtue is born of all three principles?"),
        new(Virtue.Modesty, Principle.None, "HIR", "meekbrook", ClassKind.Shepherd, "Black", 0xFF303038,
            "Modesty is seeing yourself truly: no greater and no smaller than you are.",
            "What virtue needs none of the principles, yet holds them all up?"),
    ];

    public static VirtueInfo Get(Virtue v) => All[(int)v];

    public static string Name(Virtue v) => v.ToString();

    /// <summary>The principles, in the order the Codex asks after them.</summary>
    public static readonly (Principle Principle, string Riddle)[] Principles =
    [
        (Principle.Clarity, "What principle sees things as they are?"),
        (Principle.Kindness, "What principle wishes all things well?"),
        (Principle.Courage, "What principle acts when it is hard to act?"),
    ];

    /// <summary>The last answer the Codex asks for: what all eight virtues make together.</summary>
    public const string LastWord = "WHOLENESS";

    /// <summary>A word for how far along a virtue is, as the seer and the shrines describe it.</summary>
    public static string Standing(int value) => value switch
    {
        >= Max => "unshakeable",
        >= Steadfast => "steadfast",
        >= 60 => "growing",
        >= 40 => "wavering",
        >= 20 => "faint",
        _ => "lost",
    };
}
