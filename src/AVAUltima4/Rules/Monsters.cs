namespace AVAUltima4.Rules;

[Flags]
public enum Traits
{
    None = 0,
    /// <summary>Wicked by nature. Killing it is no stain on anyone.</summary>
    Evil = 1,
    Undead = 2,
    Poison = 4,
    /// <summary>Its blows or spells can put a fighter to sleep.</summary>
    Sleep = 8,
    /// <summary>Lives in the water: met only at sea.</summary>
    Sea = 16,
    /// <summary>Met only under the earth.</summary>
    Deep = 32,
    /// <summary>Breathes fire on a line of squares.</summary>
    Fire = 64,
    /// <summary>Heals its friends.</summary>
    Healer = 128,
    /// <summary>Never runs, however badly hurt.</summary>
    Fearless = 256,
}

/// <summary>
/// A kind of creature. <see cref="Tier"/> says how deep in the dungeons or how far into the game it
/// turns up; <see cref="Range"/> is how far it can shoot (0 for none).
/// </summary>
public sealed record Monster(
    string Id, string Name, string Plural, string Sprite, string Tint, int Tier, Dice Hp, int Defence, int Skill,
    Dice Damage, int Range, int Xp, Dice Gold, Traits Traits, string About)
{
    public bool Is(Traits t) => (Traits & t) != 0;
    public bool Evil => Is(Traits.Evil);
}

public static class Monsters
{
    public static readonly IReadOnlyList<Monster> All =
    [
        // Creatures of the wild: not wicked, only hungry. They run when hurt.
        new("rat", "Giant Rat", "Giant Rats", "rat", "#8a7058", 1, new Dice(2, 6), 0, 2, new Dice(1, 4), 0, 6, new Dice(0, 1), Traits.None, "Big as a dog, and bold."),
        new("snake", "Adder", "Adders", "snake", "#60a040", 1, new Dice(2, 6), 1, 4, new Dice(1, 4), 0, 8, new Dice(0, 1), Traits.Poison, "A bite that burns."),
        new("bat", "Cave Bat", "Cave Bats", "bat", "#705880", 1, new Dice(2, 5), 1, 6, new Dice(1, 4), 0, 6, new Dice(0, 1), Traits.Deep, "Shrieking wings in the dark."),
        new("wolf", "Grey Wolf", "Grey Wolves", "wolf", "#9090a0", 2, new Dice(3, 7), 1, 6, new Dice(1, 7), 0, 14, new Dice(0, 1), Traits.None, "Hunts in packs."),
        new("spider", "Great Spider", "Great Spiders", "spider", "#504060", 2, new Dice(3, 8), 2, 5, new Dice(1, 6), 0, 18, new Dice(0, 1), Traits.Poison, "Its bite carries poison."),
        new("slime", "Ooze", "Oozes", "slime", "#50c070", 2, new Dice(3, 8), 0, 2, new Dice(1, 6), 0, 12, new Dice(0, 1), Traits.Deep | Traits.Fearless, "Mindless, and hard to kill."),
        new("wisp", "Marsh Wisp", "Marsh Wisps", "wisp", "#a0e0ff", 3, new Dice(3, 8), 4, 8, new Dice(1, 6), 3, 30, new Dice(0, 1), Traits.Sleep, "A light that lulls travellers to sleep."),
        new("bear", "Cave Bear", "Cave Bears", "bear", "#7a5030", 3, new Dice(5, 8), 2, 7, new Dice(2, 6), 0, 32, new Dice(0, 1), Traits.None, "Huge, and angry to be woken."),

        // Wicked things.
        new("brigand", "Brigand", "Brigands", "brigand", "#a04030", 1, new Dice(2, 8), 1, 5, new Dice(1, 6), 0, 10, new Dice(2, 8), Traits.Evil, "A cutthroat of the roads."),
        new("goblin", "Goblin", "Goblins", "goblin", "#70a030", 1, new Dice(2, 7), 1, 4, new Dice(1, 6), 0, 9, new Dice(1, 10), Traits.Evil, "Small, cruel and many."),
        new("archer", "Outlaw Archer", "Outlaw Archers", "archer", "#506030", 2, new Dice(3, 7), 1, 7, new Dice(1, 6), 5, 16, new Dice(2, 10), Traits.Evil, "Shoots from cover."),
        new("skeleton", "Skeleton", "Skeletons", "skeleton", "#e0dcc8", 2, new Dice(3, 7), 2, 5, new Dice(1, 8), 0, 16, new Dice(1, 6), Traits.Evil | Traits.Undead | Traits.Fearless, "Old bones that will not rest."),
        new("orc", "Orc", "Orcs", "orc", "#5a8040", 2, new Dice(3, 9), 2, 6, new Dice(1, 8), 0, 18, new Dice(2, 10), Traits.Evil, "Brutal, and well armed."),
        new("pirate", "Pirate", "Pirates", "pirate", "#c05040", 3, new Dice(4, 8), 2, 8, new Dice(2, 5), 0, 26, new Dice(4, 12), Traits.Evil | Traits.Sea, "They board from a black-sailed ship."),
        new("serpent", "Sea Serpent", "Sea Serpents", "seaserpent", "#3080a0", 3, new Dice(6, 8), 3, 8, new Dice(2, 6), 3, 40, new Dice(0, 1), Traits.Evil | Traits.Sea | Traits.Fire, "Breathes boiling spray."),
        new("ghoul", "Ghoul", "Ghouls", "ghoul", "#8a9a70", 3, new Dice(4, 8), 2, 7, new Dice(2, 5), 0, 30, new Dice(1, 10), Traits.Evil | Traits.Undead | Traits.Sleep | Traits.Fearless, "Its touch numbs and sends to sleep."),
        new("troll", "Troll", "Trolls", "troll", "#4a7050", 3, new Dice(6, 8), 3, 7, new Dice(2, 7), 0, 40, new Dice(3, 10), Traits.Evil, "Huge and hungry."),
        new("shaman", "Goblin Shaman", "Goblin Shamans", "shaman", "#b080c0", 3, new Dice(3, 8), 1, 6, new Dice(1, 6), 4, 34, new Dice(3, 10), Traits.Evil | Traits.Healer, "Heals its kin and hurls hexes."),
        new("wraith", "Wraith", "Wraiths", "wraith", "#8090c0", 4, new Dice(6, 8), 4, 9, new Dice(2, 7), 0, 60, new Dice(2, 12), Traits.Evil | Traits.Undead | Traits.Deep | Traits.Fearless, "A cold shape that drinks warmth."),
        new("golem", "Stone Golem", "Stone Golems", "golem", "#9a9080", 4, new Dice(9, 8), 5, 7, new Dice(3, 6), 0, 70, new Dice(0, 1), Traits.Evil | Traits.Deep | Traits.Fearless, "Slow, heavy, and nearly unbreakable."),
        new("drake", "Fire Drake", "Fire Drakes", "drake", "#e06030", 4, new Dice(8, 8), 4, 9, new Dice(2, 8), 4, 80, new Dice(5, 20), Traits.Evil | Traits.Fire, "A young dragon, already deadly."),
        new("knight", "Dread Knight", "Dread Knights", "knight", "#504868", 5, new Dice(10, 8), 6, 11, new Dice(3, 7), 0, 120, new Dice(6, 20), Traits.Evil | Traits.Deep | Traits.Fearless, "Armour with nothing living inside."),
        new("lich", "Lich", "Liches", "lich", "#c0a0e0", 5, new Dice(9, 8), 4, 10, new Dice(2, 8), 5, 140, new Dice(8, 20), Traits.Evil | Traits.Undead | Traits.Deep | Traits.Fire | Traits.Fearless, "A dead sorcerer who keeps casting."),
        new("wyrm", "Elder Wyrm", "Elder Wyrms", "drake", "#a02040", 5, new Dice(14, 8), 6, 12, new Dice(4, 7), 5, 200, new Dice(10, 30), Traits.Evil | Traits.Deep | Traits.Fire | Traits.Fearless, "Ancient, and the end of many heroes."),

        // Harmless folk: attacking them is a crime.
        new("pilgrim", "Pilgrim", "Pilgrims", "pilgrim", "#c8b890", 0, new Dice(2, 6), 0, 1, new Dice(1, 2), 0, 0, new Dice(1, 6), Traits.None, "Travellers on their way to a shrine."),
        new("merchant", "Peddler", "Peddlers", "peddler", "#c09040", 0, new Dice(2, 6), 0, 1, new Dice(1, 2), 0, 0, new Dice(4, 10), Traits.None, "A peddler and their mule."),
    ];

    public static Monster Get(string id) => All.First(m => m.Id == id);

    /// <summary>The kinds of thing that might be met at a tier, on land, at sea or in the deep.</summary>
    public static List<Monster> Meet(int tier, bool sea, bool deep) =>
        All.Where(m => m.Tier >= 1 && m.Tier <= tier && m.Tier >= tier - 2 && m.Is(Traits.Sea) == sea && (deep || !m.Is(Traits.Deep)) && (!deep || !m.Is(Traits.Sea)))
            .ToList();
}
