using AVAUltima4.Rules;

namespace AVAUltima4.World;

/// <summary>A yes-or-no question someone asks, and what each answer says and does to your virtues.</summary>
public sealed record Question(string Text, string YesReply, (Virtue V, int D)[] Yes, string NoReply, (Virtue V, int D)[] No);

/// <summary>
/// Something a person will talk about when you say a word that starts like one of its keys (only
/// the first four letters count, as they always have). A reply may write down a clue in your
/// journal, teach you a spell's recipe, or ask you a question.
/// </summary>
public sealed record Topic(string Keys, string Reply, bool Note = false, string? Teach = null, Question? Ask = null, string? Flag = null);

public enum NpcKind { Talker, Beggar, Guard, Companion, Queen, Child }

/// <summary>Someone who lives in a town. <see cref="Lot"/> is the part of town they keep to (-1 for the square).</summary>
public sealed record NpcDef(
    string Town, string Name, string Sprite, int Lot, NpcKind Kind, string Look, string Job, Topic[] Topics,
    ClassKind? Joins = null, int JoinLevel = 1, string? JoinWeapon = null, string? JoinArmour = null)
{
    public string Id => Town + ":" + Name;
}

/// <summary>A town's shape and trade: what fills each of its seven lots, and where its rune hides.</summary>
public sealed record TownDef(
    string Id, string Name, Virtue? Virtue, string[] Lots, string RuneObject, int RuneLot, string Arrival,
    string[] Weapons, string[] Armours, Reagent[] Reagents, Dictionary<string, string> Keepers);

public static class People
{
    // Lots, in order: top-left, top-middle, top-right, middle-left, middle-right, bottom-left, bottom-right.
    public static readonly IReadOnlyList<TownDef> Towns =
    [
        new("brightholm", "Castle Brightholm", null, ["hall", "hall", "hall", "healer", "weapons", "inn", "reagents"], "", -1,
            "The banners of Queen Isaura hang over the gate of Castle Brightholm. The throne hall lies at the north of the keep.",
            ["staff", "dagger", "sling", "club", "mace", "sword", "bow"], ["cloth", "leather", "chain"],
            [Reagent.Thistle, Reagent.Salt, Reagent.Moonpetal, Reagent.Amber],
            new() { ["healer"] = "Healer Anwen", ["weapons"] = "Armsmaster Dunstan", ["inn"] = "Innkeeper Bel", ["reagents"] = "Apothecary Wynn" }),
        new("glassmere", "Glassmere", Virtue.Candor, ["reagents", "library", "healer", "inn", "weapons", "house", "garden"], "statue", 6,
            "Glassmere, the city of Candor, stands beside a still lake. Its mages say a lie cannot be spoken here without the water rippling.",
            ["staff", "dagger", "sling"], [],
            [Reagent.Cinder, Reagent.Salt, Reagent.Moonpetal, Reagent.Amber, Reagent.Starmoss],
            new() { ["reagents"] = "Alchemist Fenn", ["healer"] = "Healer Orla", ["inn"] = "Innkeeper Maud", ["weapons"] = "Smith Hale" }),
        new("hearthwell", "Hearthwell", Virtue.Mercy, ["inn", "food", "healer", "ship", "armour", "house", "garden"], "well", 6,
            "Hearthwell, the town of Mercy, smells of the sea. Its harbour has sheltered every ship that ever asked.",
            [], ["cloth", "leather"],
            [],
            new() { ["inn"] = "Innkeeper Rosa", ["food"] = "Grocer Tobin", ["healer"] = "Healer Cael", ["ship"] = "Shipwright Marrow", ["armour"] = "Tailor Jessa" }),
        new("ironcrag", "Ironcrag", Virtue.Daring, ["weapons", "armour", "inn", "healer", "house", "house", "graves"], "grave", 6,
            "Ironcrag, the town of Daring, is built of the mountain's own stone. Its fighters train in the yards from dawn.",
            ["dagger", "club", "mace", "axe", "sword", "crossbow", "halberd"], ["leather", "chain", "plate"],
            [],
            new() { ["weapons"] = "Smith Varga", ["armour"] = "Armourer Brock", ["inn"] = "Innkeeper Hild", ["healer"] = "Healer Joss" }),
        new("evenholt", "Evenholt", Virtue.Fairness, ["reagents", "food", "healer", "hall", "inn", "garden", "house"], "tree", 5,
            "Evenholt, the town of Fairness, grows among the trees. Its judges hear every case under the open sky.",
            [], [],
            [Reagent.Thistle, Reagent.Moonpetal, Reagent.Nightcap, Reagent.Bonedust],
            new() { ["reagents"] = "Herbalist Nell", ["food"] = "Grocer Abner", ["healer"] = "Healer Ivy", ["inn"] = "Innkeeper Corra" }),
        new("tallowford", "Tallowford", Virtue.Selflessness, ["guild", "food", "healer", "house", "house", "weapons", "garden"], "fountain", 6,
            "Tallowford, the town of Selflessness, is poor, and lit by its own candles. Its people share what little they have.",
            ["staff", "club", "axe", "crossbow"], [],
            [],
            new() { ["guild"] = "Chandler Pell's daughter Ada", ["food"] = "Grocer Meg", ["healer"] = "Healer Simeon", ["weapons"] = "Tinsmith Rook" }),
        new("oathgate", "Oathgate", Virtue.Integrity, ["armour", "weapons", "inn", "healer", "house", "guild", "graves"], "statue", 6,
            "Oathgate, the town of Integrity, is guarded day and night by paladins who have never broken a word.",
            ["mace", "sword", "halberd", "crossbow"], ["chain", "plate"],
            [],
            new() { ["armour"] = "Armourer Petra", ["weapons"] = "Smith Osric", ["inn"] = "Innkeeper Gil", ["healer"] = "Healer Sorrel", ["guild"] = "Factor Lune" }),
        new("starfall", "Starfall", Virtue.Reverence, ["reagents", "inn", "healer", "observatory", "guild", "garden", "house"], "tree", 5,
            "Starfall, the town of Reverence, keeps watch on the sky. Its lamps are lit late, and nobody here talks loudly.",
            [], [],
            [Reagent.Cinder, Reagent.Bonedust, Reagent.Nightcap, Reagent.Starmoss, Reagent.Thistle],
            new() { ["reagents"] = "Gatherer Ysolt", ["inn"] = "Innkeeper Dov", ["healer"] = "Healer Lira", ["guild"] = "Factor Amos" }),
        new("meekbrook", "Meekbrook", Virtue.Modesty, ["food", "healer", "house", "pasture", "pasture", "house", "garden"], "well", 6,
            "Meekbrook, the village of Modesty, is a handful of cottages and a great many sheep.",
            [], [],
            [Reagent.Thistle, Reagent.Moonpetal],
            new() { ["food"] = "Farmer Tess", ["healer"] = "Healer Abel" }),
    ];

    public static TownDef Town(string id) => Towns.First(t => t.Id == id);

    private static readonly (Virtue, int)[] None = [];

    private static Question Ask(string text, string yes, (Virtue, int)[] yv, string no, (Virtue, int)[] nv) => new(text, yes, yv, no, nv);

    /// <summary>
    /// Everyone you can talk to. Replies may name places on the map with {horn}, {taper}, {isle},
    /// and so on: they are filled in from the land itself.
    /// </summary>
    public static readonly IReadOnlyList<NpcDef> All =
    [
        // ───────────── Castle Brightholm ─────────────
        new("brightholm", "Queen Isaura", "queen", 0, NpcKind.Queen, "a tall woman in a plain silver circlet",
            "I keep Merovale, as well as anyone can. And I am waiting for someone to become what this land needs.",
            [
                new("exem|quest|need", "Merovale needs an Exemplar: one who lives all eight virtues, so that others can see it can be done. " +
                    "Become steadfast in each virtue, and be raised at each of its eight shrines. Then go down into the Undermere and read the Codex of Echoes.", true),
                new("virt", "Candor, Mercy, Daring, Fairness, Selflessness, Integrity, Reverence and Modesty. Each grows from the three PRINCIPLES.", true),
                new("prin", "Clarity, Kindness and Courage. Candor is Clarity alone; Mercy, Kindness alone; Daring, Courage alone. " +
                    "Fairness is Clarity with Kindness; Selflessness, Kindness with Courage; Integrity, Clarity with Courage. Reverence holds all three, and Modesty none.", true),
                new("code|undermere", "The Codex of Echoes lies at the bottom of the Undermere, on its isle in the south-east sea. Nobody unready has come back from it.", true),
                new("shri", "There is a shrine near every virtue's town. Take its RUNE there, chant its MANTRA, and if your virtue is steadfast, you will be raised.", true),
                new("rune|mant", "Each town keeps its own rune hidden, and its own mantra. Ask the people of each town; they will tell an honest seeker."),
                new("help|stuck|next", "{progress}"),
                new("heal", "Rest a moment. (The Queen lays her hand on each of you, and your wounds close.)", Flag: "queenheal"),
                new("whol|word", "{lastword}"),
                new("bles|pray", "Then be still a moment with me. (You stand in silence together, and feel your Reverence deepen.)", Flag: "bless"),
            ]),
        new("brightholm", "Master Corvin", "sage", 0, NpcKind.Talker, "an old court sage with ink on his fingers",
            "I advise Her Majesty, and keep the castle's books.",
            [
                new("seal|stone", "The Undermere has four seals, one on each level. Each seal asks for two of the eight stones, and they go in the order of the virtues. " +
                    "Candor and Mercy open the first; Daring and Fairness the second; Selflessness and Integrity the third; Reverence and Modesty the last.", true),
                new("rekin|raise|dead", "Rekindle brings back the dead. Mix thistle, moonpetal, starmoss, bonedust and nightcap - all five.", true, Teach: "rekindle"),
                new("way|home|cast", "Wayfare carries you here from anywhere in the open land: starmoss, nightcap and cinder.", true, Teach: "wayfare"),
                new("book|mix|spell", "Spells are mixed from reagents, one of each in the recipe, and then cast with mana. Your spellbook lists every recipe you have learned."),
            ]),
        new("brightholm", "Herald Pim", "noble", 1, NpcKind.Talker, "a herald in a bright tabard",
            "I cry the news of the kingdom, and the deeds of its heroes!",
            [
                new("deed|news|cry", "Every day, someone new comes to the castle hoping to be the Exemplar.",
                    Ask: Ask("Shall I cry your own great deeds through the castle?",
                        "Very well! (He shouts your name. Some of the courtiers roll their eyes.)", [(Virtue.Modesty, -5)],
                        "Hm. Most people say yes. I think better of you for it.", [(Virtue.Modesty, 5)])),
            ]),
        new("brightholm", "Sergeant Mab", "guard", 2, NpcKind.Guard, "a guard in the Queen's colours",
            "I keep the Queen safe.",
            [
                new("moon|gate", "The moongates stand in a ring around the castle. Ask the stargazers of Starfall how they work."),
                new("dung", "Hollowroot is nearest, {dir:hollowroot}. It's the place to learn your trade.", true),
            ]),
        new("brightholm", "Old Fen", "beggar", -1, NpcKind.Beggar, "a ragged man by the fountain", "Begging, sir or madam. What else is there?", []),

        // ───────────── Glassmere: Candor ─────────────
        new("glassmere", "Ilsabet", "sage", 1, NpcKind.Talker, "a scholar surrounded by books",
            "I keep the library of Glassmere, and I tell the truth for a living. It doesn't pay well.",
            [
                new("mant", "The mantra of Candor is VEL. Chant it at the shrine, {dir:shrine-candor}.", true),
                new("tome|book|key", "The Tome of Hours is one of the THREE KEYS. It sits on a shelf in this very library - the shelf in the library's north-west corner. Search there, if you have need of it.", true),
                new("key|three", "The Brass Horn, the Tome of Hours and the Everflame Taper. They open the Undermere, used in that order."),
                new("lie|truth", "Here is a question I ask everyone who comes to my library.",
                    Ask: Ask("Have you ever told a lie?",
                        "So has everyone. You are the first in a month to say so.", [(Virtue.Candor, 5)],
                        "The lake ripples... I don't believe you.", [(Virtue.Candor, -5)])),
            ]),
        new("glassmere", "Thessaly", "mage", 3, NpcKind.Talker, "a mage reading by the window",
            "I study the magic that carries us from place to place.",
            [
                new("rise|up|clim", "Rise lifts you one level up out of a dungeon: starmoss and amber.", true, Teach: "rise"),
                new("retu|out|esca", "Return takes you out of a dungeon all at once: starmoss, bonedust and amber.", true, Teach: "return"),
                new("rune", "Our rune? A child told me she saw it hidden beside the statue in the garden, at the south-east of town.", true),
            ]),
        new("glassmere", "Mariel", "mage", -1, NpcKind.Companion, "a young mage with a bright eye",
            "I'm a mage. I've read every book in Glassmere, and now I want to see what they were about.",
            [new("cand", "Candor is the hardest virtue. The truth has a way of costing you something.")],
            ClassKind.Mage, 2, "staff", "cloth"),
        new("glassmere", "Tib", "child", 6, NpcKind.Child, "a small girl with muddy knees",
            "I find things!",
            [new("find|thing|stat", "There's a rune hidden by the statue! I saw it shine once. Search right next to it.", true)]),
        new("glassmere", "Arno", "beggar", -1, NpcKind.Beggar, "a blind man with a cup", "I beg. My eyes went years ago.", []),

        // ───────────── Hearthwell: Mercy ─────────────
        new("hearthwell", "Brother Tam", "sage", 0, NpcKind.Talker, "a gentle monk mending a net",
            "I look after the sick and the shipwrecked.",
            [
                new("mant", "The mantra of Mercy is ORA. The shrine is {dir:shrine-mercy}.", true),
                new("rune", "The rune of Mercy is kept beside the old well in the garden, in the south-east corner of town.", true),
                new("pira|hang", "The town has taken a pirate. They mean to hang him in the morning.",
                    Ask: Ask("Should he hang?",
                        "Perhaps. But he is someone's son.", [(Virtue.Mercy, -3)],
                        "I think so too. He'll work off his crimes on the docks instead.", [(Virtue.Mercy, 5)])),
            ]),
        new("hearthwell", "Old Neddy", "man", -1, NpcKind.Talker, "an old fisherman smoking a pipe",
            "Fishing. Sixty years of it.",
            [
                new("horn|brass|sea", "Long ago a ship went down far out to the south-west, carrying a brass horn. I've marked the spot since I was a boy: {horn}. " +
                    "Sail there and search the water.", true),
                new("ship|boat", "Marrow the shipwright has a boat for sale. Can't sail without one, can you?"),
                new("isle|under", "There's an isle out in the south-east sea where no fish swim. I don't go near it."),
            ]),
        new("hearthwell", "Lark", "bard", 4, NpcKind.Companion, "a bard tuning a lute",
            "I'm a bard. I sing for my supper, and sometimes for nothing at all.",
            [
                new("hush|slee|song", "A lullaby, you want? Hush is a spell that sends foes to sleep: nightcap and moonpetal.", true, Teach: "hush"),
                new("merc", "Mercy is a song you play for someone who can't pay you."),
            ],
            ClassKind.Bard, 2, "sling", "leather"),
        new("hearthwell", "Wenna", "woman", 5, NpcKind.Talker, "a fishwife, looking worried",
            "I sell fish. Or I did, until I lost my purse with all my takings.",
            [
                new("purs|lost", "I dropped it somewhere in the garden, I'm sure of it. If you find it, bring it to me, won't you?", true),
            ]),
        new("hearthwell", "Gisela", "beggar", -1, NpcKind.Beggar, "a widow in black", "I have nothing since my husband drowned.", []),

        // ───────────── Ironcrag: Daring ─────────────
        new("ironcrag", "Gerrit", "fighter", 4, NpcKind.Talker, "a grizzled old soldier",
            "I train the young ones in the yards. Daring isn't the same as not being afraid, you know.",
            [
                new("mant", "The mantra of Daring is KUR. The shrine is {dir:shrine-daring}.", true),
                new("rune", "The rune lies by a grave in the graveyard, the south-east corner of town. The grave of the first of us who didn't run.", true),
                new("drag|stra|save", "Here's a question for you.",
                    Ask: Ask("Would you stand against a dragon to save a stranger?",
                        "Good. Mean it.", [(Virtue.Daring, 5)],
                        "Then you've a long way to go.", [(Virtue.Daring, -3)])),
                new("ston|red", "The Red Stone glows on an altar in the Cinder Vaults, {dir:cindervault}, on its second level. The Orange Stone is on the fourth.", true),
            ]),
        new("ironcrag", "Brenna", "fighter", 5, NpcKind.Companion, "a broad-shouldered woman with an axe",
            "I fight. Point me at something.",
            [
                new("dari", "Running away is easy. It's the coming back that's hard."),
                new("chain|mail|armo|mirr", "My uncle swore there's mail with runes on it in a chest on the third level of the Mirrored Mine. He never went back for it.", true),
            ],
            ClassKind.Fighter, 3, "axe", "chain"),
        new("ironcrag", "Magister Ulf", "mage", 2, NpcKind.Talker, "a war-mage with singed eyebrows",
            "I teach the fighters a little fire.",
            [
                new("blaz|fire", "Blaze burns every foe at once: cinder, amber and bonedust. Mind your eyebrows.", true, Teach: "blaze"),
                new("star|blade|sword", "A blade forged from a fallen star lies in a chest on the fourth level of the Cinder Vaults. Only a fighter or a paladin could lift it.", true),
            ]),
        new("ironcrag", "Hob", "beggar", -1, NpcKind.Beggar, "a soldier with one leg", "I fought for the Queen. Now I sit here.", []),

        // ───────────── Evenholt: Fairness ─────────────
        new("evenholt", "Moira", "druid", 6, NpcKind.Talker, "a druid with leaves in her hair",
            "I tend the old trees.",
            [
                new("mant", "The mantra of Fairness is SEN. The shrine is {dir:shrine-fairness}.", true),
                new("rune|tree", "Our rune sleeps among the roots of the great oak in the grove, at the south-west of town.", true),
                new("bani|dead|undead", "Banish puts the dead back in the ground: salt, bonedust and starmoss.", true, Teach: "banish"),
            ]),
        new("evenholt", "Judge Ambrose", "noble", 3, NpcKind.Talker, "a judge in a grey robe",
            "I judge disputes in Evenholt.",
            [
                new("case|disp|judg", "Today a woman stole bread to feed her children. The law says she must lose her hand.",
                    Ask: Ask("Should the law be followed?",
                        "Harsh. Is that all she is due?", [(Virtue.Fairness, -3), (Virtue.Mercy, -2)],
                        "No. She will work for the baker until it is paid. That is fair to both.", [(Virtue.Fairness, 5)])),
                new("calf|farm", "Two farmers claim the same calf. The rich one owns it, but the poor one needs it more.",
                    Ask: Ask("Should the poor farmer have it?",
                        "It is not his. Kindness must not cost a man what is his.", [(Virtue.Fairness, -3)],
                        "Right: the calf goes to its owner, and the town will help the other. Each gets their due.", [(Virtue.Fairness, 5)])),
            ]),
        new("evenholt", "Hermit Crane", "man", -1, NpcKind.Talker, "a hermit who smells of marsh",
            "I live out in the marsh. I came in for salt.",
            [new("tape|cand|flam|key", "Something burns under the marsh mud and never goes out. I found it at {taper}, and buried it again. Search there.", true)]),
        new("evenholt", "Thorne", "druid", 4, NpcKind.Companion, "a quiet druid with a staff",
            "I'm a druid. I listen to what the land has to say.",
            [new("fair", "Fairness is giving each thing its due. Even the wolf.")],
            ClassKind.Druid, 2, "staff", "leather"),
        new("evenholt", "Pell", "beggar", -1, NpcKind.Beggar, "a boy with no shoes", "My family lost the farm.", []),

        // ───────────── Tallowford: Selflessness ─────────────
        new("tallowford", "Chandler Pell", "man", 3, NpcKind.Talker, "a candlemaker with wax-burned hands",
            "I make the candles. They light the town and they're the only thing we have to sell.",
            [
                new("mant", "The mantra of Selflessness is TAH. The shrine is {dir:shrine-selflessness}.", true),
                new("rune", "The rune of Selflessness is in the fountain at the south-east of town. Search beside it.", true),
                new("rest|heal", "The healer taught me Restore, to heal everyone at once: thistle, moonpetal and starmoss.", true, Teach: "restore"),
            ]),
        new("tallowford", "Widow Moss", "woman", 4, NpcKind.Talker, "a thin woman with three thin children",
            "We're hungry, that's all. Everyone in Tallowford is a little hungry.",
            [new("hung|food|chil", "The children haven't eaten since yesterday.",
                Ask: Ask("Will you give them ten portions of your food?",
                    "(The children eat. Their mother can't speak for a moment.)", [(Virtue.Selflessness, 4)],
                    "No. I understand. Times are hard.", None), Flag: "feedfamily")]),
        new("tallowford", "Garrick", "tinker", 5, NpcKind.Companion, "a tinker with tools on his belt",
            "I mend pots, and I can mend a wall, or a cart, or a head if it comes to it.",
            [new("self", "If you've two of something, someone has none.")],
            ClassKind.Tinker, 2, "club", "leather"),
        new("tallowford", "Ivo", "beggar", -1, NpcKind.Beggar, "an old man shivering", "I'm cold. That's all.", []),

        // ───────────── Oathgate: Integrity ─────────────
        new("oathgate", "Squire Edda", "paladin", 4, NpcKind.Talker, "a squire polishing a helm",
            "I serve Sir Roderic, and one day I'll take my own oath.",
            [
                new("mant", "The mantra of Integrity is DUN. The shrine is {dir:shrine-integrity}.", true),
                new("rune", "The rune is kept by the statue of the first paladin, in the graveyard at the south-east of town.", true),
                new("purs|keep|found", "Here's what I ask everyone.",
                    Ask: Ask("If you found a purse with nobody about, would you keep it?",
                        "Then who would ever know you? Not even you.", [(Virtue.Integrity, -3)],
                        "Good. Neither would I.", [(Virtue.Integrity, 5)])),
            ]),
        new("oathgate", "Sir Roderic", "paladin", 0, NpcKind.Companion, "a paladin in white",
            "I am a paladin of Oathgate, sworn to the virtues until I die.",
            [
                new("ward|prot", "Ward is a paladin's prayer: salt and amber, and your foes find it hard to land a blow.", true, Teach: "ward"),
                new("isle|under", "The Undermere lies on an isle at {isle}, out in the south-east sea. You'll need a ship.", true),
            ],
            ClassKind.Paladin, 3, "sword", "chain"),
        new("oathgate", "Captain Vane", "guard", 1, NpcKind.Guard, "a captain of the gate guard",
            "I keep the gate.",
            [
                new("ston|purp|whit", "Saltdeep, {dir:saltdeep}, holds the Purple Stone on its second level and the White Stone on its fourth.", true),
                new("plat|armo|star", "There is plate in Saltdeep that shines in the dark, in a chest on the third level. Starplate, the old songs call it.", true),
            ]),
        new("oathgate", "Sim", "beggar", -1, NpcKind.Beggar, "a man in a torn cloak", "I lost everything at dice. I won't lie to you about it.", []),

        // ───────────── Starfall: Reverence ─────────────
        new("starfall", "Quillan", "sage", 3, NpcKind.Talker, "an astronomer at a brass telescope",
            "I watch the sky. Somebody ought to.",
            [
                new("mant", "The mantra of Reverence is ELO. The shrine lies {dir:shrine-reverence}.", true),
                new("moon|gate", "Two moons: pale Selis and red Roan. When Selis is in a phase, the moongate of that phase opens - they stand in a ring about the castle, " +
                    "the first due north and the rest clockwise. Step in, and Roan's phase decides which gate you come out of.", true),
                new("stil|quie", "Here's a thing I wonder about everyone.",
                    Ask: Ask("Do you ever just stop, and be still?",
                        "Good. The sky does nothing else, and it's beautiful.", [(Virtue.Reverence, 5)],
                        "Try it. Tonight.", [(Virtue.Reverence, -2)])),
                new("key|horn|tome|tape|order", "The three keys are used at the Undermere's door in this order: horn, then tome, then taper.", true),
            ]),
        new("starfall", "Gatherer Ysolt's boy", "child", 5, NpcKind.Child, "a boy chasing moths",
            "Moths!",
            [new("rune|tree|moth", "There's a rune under the big tree in the garden! The moths like it.", true)]),
        new("starfall", "Sela", "ranger", 6, NpcKind.Companion, "a ranger with a longbow",
            "I'm a ranger. I keep the roads safe, more or less.",
            [new("reve", "Out in the wild, at night, you can feel all three principles at once.")],
            ClassKind.Ranger, 3, "bow", "leather"),
        new("starfall", "Dace", "beggar", -1, NpcKind.Beggar, "a woman who lost her sight to the sun", "I looked too long at the sky.", []),

        // ───────────── Meekbrook: Modesty ─────────────
        new("meekbrook", "Old Hob", "shepherd", 3, NpcKind.Talker, "an old shepherd leaning on a crook",
            "I mind the sheep. It's not much, but the sheep think it's a great deal.",
            [
                new("mant", "The mantra of Modesty is HIR. The shrine is a short walk {dir:shrine-modesty}.", true),
                new("rune", "There's a rune at the bottom of the old well in the garden, the south-east corner. Search beside the well.", true),
                new("flock|help|sheep", "The flock wants moving to the far meadow.",
                    Ask: Ask("Will you help me move them? It takes all day, and nobody will thank you.",
                        "(You spend the day walking with the sheep. Nobody thanks you. It feels good anyway.)", [(Virtue.Modesty, 5)],
                        "No matter.", None), Flag: "flock"),
                new("grea|hero", "A lot of heroes come through here.",
                    Ask: Ask("Are you the greatest of them?",
                        "Oh? (The old man smiles politely and goes back to his sheep.)", [(Virtue.Modesty, -5)],
                        "Ha! A good answer.", [(Virtue.Modesty, 5)])),
            ]),
        new("meekbrook", "Wim", "shepherd", 6, NpcKind.Companion, "a young shepherd with a sling",
            "Just a shepherd. I'm good with a sling, mind.",
            [new("mode", "My gran says you're no bigger than you are, and no smaller either.")],
            ClassKind.Shepherd, 1, "sling", "cloth"),
        new("meekbrook", "Tessa", "woman", 2, NpcKind.Talker, "a farmer's wife",
            "I keep the house.",
            [
                new("ston|yell|gree|holl", "My man went down into Hollowroot once. He said the Yellow Stone is on the second level, and the Green on the fourth.", true),
                new("mirr|blue|blac", "The Mirrored Mine, {dir:mirrormine}: the Blue Stone on its second level, the Black on its fourth. So the tinkers say.", true),
            ]),
        new("meekbrook", "Nan", "beggar", -1, NpcKind.Beggar, "an old woman by the well", "I'm old and I'm poor. There it is.", []),
    ];

    public static IEnumerable<NpcDef> In(string town) => All.Where(n => n.Town == town);

    /// <summary>The 28 questions of the seer, one for every pair of virtues: the text, and the two answers (the first virtue's answer first).</summary>
    public static readonly IReadOnlyList<(Virtue A, Virtue B, string Text, string AnswerA, string AnswerB)> Seer =
    [
        (Virtue.Candor, Virtue.Mercy, "A dying soldier asks whether his side won the battle. It lost.", "Tell him the truth.", "Tell him it won."),
        (Virtue.Candor, Virtue.Daring, "You saw your captain take a bribe. Telling anyone will cost you your post, and nobody else will speak up either way.", "Report it.", "Say nothing, and keep your post to fight another day."),
        (Virtue.Candor, Virtue.Fairness, "A friend asks you, honestly, whether their poems are good. They aren't, but a contest judge gave them a prize they earned on merit.", "Tell them they aren't good.", "Tell them the prize was fairly won."),
        (Virtue.Candor, Virtue.Selflessness, "You can win a sick child's family a fortune by lying to a rich lord about the child's father.", "Refuse to lie.", "Lie, and give them the money."),
        (Virtue.Candor, Virtue.Integrity, "You swore to keep a friend's secret. A judge asks you about it under oath.", "Tell the judge the truth.", "Keep your word to your friend."),
        (Virtue.Candor, Virtue.Reverence, "During a holy rite you notice the priest has made a mistake that spoils it.", "Say so at once.", "Keep the rite's stillness, and tell the priest later."),
        (Virtue.Candor, Virtue.Modesty, "You are praised for a deed. In truth you did it better than anyone knows.", "Tell them exactly what you did.", "Let it pass."),
        (Virtue.Mercy, Virtue.Daring, "A wounded enemy begs for help in the middle of a battle your side is losing.", "Stop and tend him.", "Fight on, for your side."),
        (Virtue.Mercy, Virtue.Fairness, "A thief who stole from you, and was caught, is poor and sick.", "Let him go.", "Let the court decide his due."),
        (Virtue.Mercy, Virtue.Selflessness, "A lame dog follows you home. Keeping it means you will go hungry this winter.", "Keep the dog.", "Give your food to the town's poor instead."),
        (Virtue.Mercy, Virtue.Integrity, "You promised to deliver a letter by dawn. On the road, a traveller lies hurt.", "Stay and help the traveller.", "Keep your promise and ride on."),
        (Virtue.Mercy, Virtue.Reverence, "A grieving stranger interrupts your prayer at a shrine.", "Comfort them.", "Finish your prayer first."),
        (Virtue.Mercy, Virtue.Modesty, "Someone weaker than you is mocked for a mistake you made.", "Defend them, though they'll think you a hero.", "Quietly own the mistake, and say no more."),
        (Virtue.Daring, Virtue.Fairness, "A bully twice your size is beating a man who, you learn, cheated him.", "Step in and stop it.", "Let them settle what is between them."),
        (Virtue.Daring, Virtue.Selflessness, "A burning house: you can rush in for the child inside, or hold the bucket line that keeps the fire from the next house.", "Rush in.", "Hold the line."),
        (Virtue.Daring, Virtue.Integrity, "You promised your mother you would not go to war. War comes.", "Go and fight.", "Keep your promise."),
        (Virtue.Daring, Virtue.Reverence, "Raiders are coming. The elders are still praying and the town waits for them.", "Ring the alarm now.", "Wait for the prayer to end."),
        (Virtue.Daring, Virtue.Modesty, "The knights ask for a volunteer to lead a charge. You are the best rider there.", "Volunteer.", "Wait to be chosen."),
        (Virtue.Fairness, Virtue.Selflessness, "You and a stranger find a purse. It's yours by right of finding it first.", "Split it evenly with them.", "Give it all to them; they need it more."),
        (Virtue.Fairness, Virtue.Integrity, "You promised to vote for a friend for mayor. A better candidate has come forward.", "Vote for the better one.", "Keep your promise."),
        (Virtue.Fairness, Virtue.Reverence, "Two pilgrims quarrel over who goes first into a shrine.", "Settle it between them.", "Pray, and let them sort it out."),
        (Virtue.Fairness, Virtue.Modesty, "You are asked to judge a contest in which you know you are the best entrant.", "Judge it, and put yourself first, truly.", "Step aside from it."),
        (Virtue.Selflessness, Virtue.Integrity, "You promised your last coins to a merchant. A beggar asks for them.", "Give them to the beggar.", "Pay the merchant as you promised."),
        (Virtue.Selflessness, Virtue.Reverence, "You meant to spend the day in quiet at a shrine. A neighbour needs help with the harvest.", "Help with the harvest.", "Keep your day at the shrine."),
        (Virtue.Selflessness, Virtue.Modesty, "You can give a great gift to the town, but only if your name is carved on it.", "Give it, with your name.", "Give something smaller, unnamed."),
        (Virtue.Integrity, Virtue.Reverence, "You swore to meet a friend at noon. At noon the temple bell rings for the dead.", "Meet your friend.", "Go to the temple."),
        (Virtue.Integrity, Virtue.Modesty, "You are asked to swear you are the best smith in town, so a lord will buy your work. You believe it is so.", "Swear it.", "Say only that your work is good."),
        (Virtue.Reverence, Virtue.Modesty, "You see a sign at a shrine meant, you think, only for you.", "Kneel and give thanks.", "Think nothing of it; it was probably meant for everyone."),
    ];
}
