namespace AVAUltima4.Rules;

/// <summary>The eight reagents a spell is mixed from.</summary>
public enum Reagent { Cinder, Thistle, Salt, Moonpetal, Bonedust, Amber, Nightcap, Starmoss }

public enum SpellTarget
{
    /// <summary>A line of squares from the caster: the first foe on it.</summary>
    Line,
    /// <summary>Every foe on the field.</summary>
    Foes,
    /// <summary>One of the party, chosen.</summary>
    Ally,
    /// <summary>The whole party.</summary>
    Party,
    /// <summary>No target: the spell works on the world.</summary>
    World,
}

[Flags]
public enum SpellUse { Battle = 1, Field = 2, Both = 3 }

/// <summary>
/// A spell. Casting one takes a mixed dose of it and some mana; a dose is mixed from its reagents,
/// one of each. <see cref="Taught"/> spells are in the spellbook from the start; the others have to
/// be learned from people, or found by mixing the right reagents by luck.
/// </summary>
public sealed record Spell(string Id, string Name, int Mana, Reagent[] Recipe, SpellTarget Target, SpellUse Use, bool Taught, string About)
{
    public static readonly IReadOnlyList<Spell> All =
    [
        new("mend", "Mend", 5, [Reagent.Thistle, Reagent.Moonpetal], SpellTarget.Ally, SpellUse.Both, true, "Heals one companion's wounds."),
        new("purge", "Purge", 5, [Reagent.Thistle, Reagent.Salt], SpellTarget.Ally, SpellUse.Both, true, "Draws poison out of the blood."),
        new("spark", "Spark", 5, [Reagent.Cinder, Reagent.Amber], SpellTarget.Line, SpellUse.Battle, true, "A bolt of fire at the first foe in a line."),
        new("glow", "Glow", 3, [Reagent.Cinder, Reagent.Salt], SpellTarget.World, SpellUse.Field, true, "Light for a long while underground."),
        new("hush", "Hush", 10, [Reagent.Nightcap, Reagent.Moonpetal], SpellTarget.Foes, SpellUse.Battle, false, "Lulls the living foes to sleep."),
        new("ward", "Ward", 8, [Reagent.Salt, Reagent.Amber], SpellTarget.Party, SpellUse.Battle, false, "The party is harder to hit for the rest of the fight."),
        new("blaze", "Blaze", 20, [Reagent.Cinder, Reagent.Amber, Reagent.Bonedust], SpellTarget.Foes, SpellUse.Battle, false, "Fire falls on every foe."),
        new("banish", "Banish", 15, [Reagent.Salt, Reagent.Bonedust, Reagent.Starmoss], SpellTarget.Foes, SpellUse.Battle, false, "Sends the restless dead back to their rest."),
        new("restore", "Restore", 20, [Reagent.Thistle, Reagent.Moonpetal, Reagent.Starmoss], SpellTarget.Party, SpellUse.Both, false, "Heals the whole party."),
        new("rise", "Rise", 10, [Reagent.Starmoss, Reagent.Amber], SpellTarget.World, SpellUse.Field, false, "Lifts the party one level nearer the surface."),
        new("return", "Return", 15, [Reagent.Starmoss, Reagent.Bonedust, Reagent.Amber], SpellTarget.World, SpellUse.Field, false, "Carries the party out of any dungeon."),
        new("rekindle", "Rekindle", 40, [Reagent.Thistle, Reagent.Moonpetal, Reagent.Starmoss, Reagent.Bonedust, Reagent.Nightcap], SpellTarget.Ally, SpellUse.Field, false, "Brings a fallen companion back to life."),
        new("wayfare", "Wayfare", 25, [Reagent.Starmoss, Reagent.Nightcap, Reagent.Cinder], SpellTarget.World, SpellUse.Field, false, "Carries the party to the gates of Castle Brightholm."),
    ];

    public static Spell Get(string id) => All.First(s => s.Id == id);

    /// <summary>The spell a set of reagents makes, if any.</summary>
    public static Spell? FromRecipe(IEnumerable<Reagent> reagents)
    {
        var set = reagents.Distinct().OrderBy(r => r).ToList();
        return All.FirstOrDefault(s => s.Recipe.OrderBy(r => r).SequenceEqual(set));
    }

    public string RecipeText => string.Join(" + ", Recipe);
}
