namespace AVAUltima4.Rules;

/// <summary>
/// A weapon: its damage, its reach in squares (0 for a hand weapon, 1 for a halberd's extra square,
/// more for missiles), and its price (0 for the arms that can't be bought).
/// </summary>
public sealed record Weapon(string Id, string Name, Dice Damage, int Range, int Price, string About);

public sealed record Armour(string Id, string Name, int Defence, int Price, string About);

public static class Arms
{
    public static readonly IReadOnlyList<Weapon> Weapons =
    [
        new("hands", "Bare hands", new Dice(1, 3), 0, 0, "Better than nothing, barely."),
        new("staff", "Staff", new Dice(1, 6), 0, 20, "A stout length of ash."),
        new("dagger", "Dagger", new Dice(1, 5, 1), 0, 25, "Short and sharp."),
        new("sling", "Sling", new Dice(1, 6), 4, 40, "Stones, thrown hard, four squares."),
        new("club", "Club", new Dice(1, 8), 0, 45, "Heavy wood bound with iron."),
        new("mace", "Mace", new Dice(2, 5), 0, 120, "A flanged iron head on a haft."),
        new("axe", "Axe", new Dice(2, 6), 0, 190, "A woodsman's axe, made for war."),
        new("sword", "Sword", new Dice(2, 7), 0, 260, "Good steel, well balanced."),
        new("bow", "Bow", new Dice(2, 5), 5, 300, "Arrows, five squares."),
        new("crossbow", "Crossbow", new Dice(2, 7), 5, 520, "Bolts, five squares, and hard-hitting."),
        new("halberd", "Halberd", new Dice(3, 6), 1, 640, "Strikes a foe two squares away."),
        new("starblade", "Starblade", new Dice(4, 7), 0, 0, "Forged from a fallen star. Not for sale anywhere."),
    ];

    public static readonly IReadOnlyList<Armour> Armours =
    [
        new("none", "No armour", 0, 0, ""),
        new("cloth", "Cloth", 1, 40, "Padded cloth."),
        new("leather", "Leather", 2, 100, "Boiled leather."),
        new("chain", "Chain mail", 3, 320, "Rings of iron."),
        new("plate", "Plate", 4, 650, "Heavy and strong."),
        new("runechain", "Runechain", 5, 0, "Chain mail marked with old runes. Not for sale."),
        new("starplate", "Starplate", 6, 0, "Plate that shines faintly in the dark. Not for sale."),
    ];

    public static Weapon Weapon(string id) => Weapons.First(w => w.Id == id);
    public static Armour Armour(string id) => Armours.First(a => a.Id == id);

    public static bool CanUse(ClassKind k, string id) =>
        id is "hands" or "none" || ClassInfo.Get(k).Weapons.Contains(id) || ClassInfo.Get(k).Armour.Contains(id);
}
