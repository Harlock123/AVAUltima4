namespace AVAUltima4.Rules;

/// <summary>The eight callings, one trained in each virtue's town.</summary>
public enum ClassKind { Mage, Bard, Fighter, Druid, Tinker, Paladin, Ranger, Shepherd }

/// <summary>
/// A calling: its starting attributes, how much magic it holds (mana per point of intellect, in
/// tenths), and the arms and armour it may use.
/// </summary>
public sealed record ClassInfo(
    ClassKind Kind, string Name, int Str, int Dex, int Int, int ManaTenths, string[] Weapons, string[] Armour, string Blurb)
{
    public static readonly IReadOnlyList<ClassInfo> All =
    [
        new(ClassKind.Mage, "Mage", 12, 18, 23, 20, ["staff", "dagger", "sling"], ["cloth"],
            "Frail in a fight, but holds more magic than any other calling."),
        new(ClassKind.Bard, "Bard", 15, 22, 15, 10, ["staff", "dagger", "sling", "club", "bow"], ["cloth", "leather"],
            "Quick of hand and quick with a bow; a little magic."),
        new(ClassKind.Fighter, "Fighter", 22, 17, 12, 0, ["staff", "dagger", "sling", "club", "mace", "axe", "sword", "bow", "crossbow", "halberd", "starblade"],
            ["cloth", "leather", "chain", "plate", "runechain", "starplate"],
            "Any weapon, any armour, and no magic at all."),
        new(ClassKind.Druid, "Druid", 14, 16, 21, 15, ["staff", "dagger", "sling", "mace"], ["cloth", "leather"],
            "A healer and a caster, at home in the wild places."),
        new(ClassKind.Tinker, "Tinker", 20, 15, 15, 5, ["staff", "dagger", "sling", "club", "mace", "axe", "crossbow"], ["cloth", "leather", "chain"],
            "Strong and handy; heavy tools make heavy weapons."),
        new(ClassKind.Paladin, "Paladin", 20, 16, 16, 10, ["staff", "dagger", "club", "mace", "axe", "sword", "crossbow", "halberd", "starblade"],
            ["cloth", "leather", "chain", "plate", "runechain", "starplate"],
            "A knight sworn to the virtues, with some healing magic."),
        new(ClassKind.Ranger, "Ranger", 16, 20, 17, 10, ["staff", "dagger", "sling", "sword", "bow", "crossbow"], ["cloth", "leather", "chain", "runechain"],
            "A scout and an archer, who knows the wilderness."),
        new(ClassKind.Shepherd, "Shepherd", 15, 15, 15, 0, ["staff", "dagger", "sling"], ["cloth"],
            "No great fighter and no caster: a humble start."),
    ];

    public static ClassInfo Get(ClassKind k) => All[(int)k];

    public static string NameOf(ClassKind k) => Get(k).Name;
}
