# AVAUltima4

[![Build](https://github.com/Harlock123/AVAUltima4/actions/workflows/build.yml/badge.svg)](https://github.com/Harlock123/AVAUltima4/actions/workflows/build.yml)

A role-playing quest in the spirit of Origin's 1985 classic **Ultima IV: Quest of the Avatar**, built
with .NET 10 and Avalonia UI. Nobody needs you to kill a dark lord. The land of **Merovale** needs an
**Exemplar**: someone who lives all eight virtues, so that everyone else can see it can be done.

You become one by what you do, not by what you say. You pay the merchant who forgets to charge you,
give to the beggar, spare the wolf that runs, and give blood at the healer. You stand and fight, and
you don't boast. The people of Merovale notice, and so do the shrines.

The land, towns, people, virtues, mantras, spells, monsters, words and pictures are all original.
This is the *style* of the classic, not its contents.

## The quest

1. **The seer**: a new life begins at a crossroads in the rain. The Seer of the Crossroads asks seven
   either-or questions.
   - **The questions**: each one pits two virtues against each other.
   - **The result**: the virtue left standing chooses your calling and your home town.
2. **The eight virtues** each grow from the three principles: **Clarity**, **Kindness** and **Courage**.
   - **One principle**: Candor (Clarity), Mercy (Kindness) and Daring (Courage).
   - **Two principles**: Fairness (Clarity and Kindness), Selflessness (Kindness and Courage) and
     Integrity (Clarity and Courage).
   - **All three, or none**: Reverence holds all three, and Modesty none.
3. **Becoming steadfast**: every virtue rises and falls with your deeds.
   - **Raised**: when a virtue is steadfast, take its **rune** to its **shrine** and chant its
     **mantra**. You are raised in it.
   - **Not yet**: until then, the shrine gives you counsel.
4. **The treasures**:
   - **The eight runes**: hidden in the towns.
   - **The eight stones**: guarded on the altars of the four dungeons.
   - **The three keys**: the Brass Horn, the Tome of Hours and the Everflame Taper.
   - **Finding them**: nobody hands them to you. People mention them, and you search where they say.
5. **The Undermere**: on its isle in the south-eastern sea, reached only by ship. Its door takes the
   three keys in the right order, and each of its four seals takes two stones.
6. **The Codex of Echoes**: at the bottom it waits for someone raised in all eight virtues. Answer
   its questions, and Merovale has its Exemplar.

## How your virtues grow, and suffer

| Virtue | Grows when you... | Suffers when you... |
|---|---|---|
| **Candor** | pay a merchant who forgot to charge you; return what you find; answer truly | walk off unpaid; take from strangers' chests; lie |
| **Mercy** | give to beggars; let a beaten creature run | strike a creature as it flees; attack harmless folk |
| **Daring** | stand and win against wicked things; win an altar's guardians | run from a fight |
| **Fairness** | greet harmless travellers in peace; judge a case fairly; spare what runs | attack pilgrims or peddlers |
| **Selflessness** | give blood at a healer; feed the hungry | |
| **Integrity** | find the quest's treasures; keep your word; give back a lost purse | steal; keep what isn't yours |
| **Reverence** | meditate at the shrines; ask the Queen's blessing; take time to be still | |
| **Modesty** | do humble work nobody thanks you for; refuse praise | boast |

A virtue reaches **steadfast** at 75. The **V** screen shows where each one stands.

## Features

- **Talking by typing**: walk into someone and type a word. Everyone answers **NAME**, **JOB** and
  **BYE**, and each has their own topics; ask about anything they mention.
  - **Four letters**: only the first four letters of a word count.
  - **Clues**: anything that matters is written in your **journal**.
  - **Tests**: some people ask you yes-or-no questions, and your answers move your virtues.
- **An island continent** grown from a fixed seed, so it's the same Merovale for everyone.
  - **The land**: grassland, scrub, forests, hills, mountain ridges, marshes, coasts and the sea.
  - **The places**: a castle, eight towns, eight shrines, five dungeons and eight moongates.
  - **Directions**: clues name real map coordinates, and directions like "north-east of here, not
    far" are worked out from the land itself.
- **Two moons**:
  - **Selis**: the pale moon. Its phase decides which moongate stands open.
  - **Roan**: the red moon. Its phase decides where that gate leads.
- **Towns** with shops:
  - **Trade**: arms and armour (with a trade-in), food, reagents, and the guild's torches and
    seeing-gems.
  - **Help**: healers and inns, and the shipwright of Hearthwell.
  - **Townsfolk**: they wander about their business. In each virtue's town one of them will **JOIN**
    you, if you show that town's virtue.
- **Queen Isaura** in Castle Brightholm:
  - **Levels**: she raises anyone who has earned a new level.
  - **Advice**: she heals you, and ask her for **HELP** and she tells you what to do next.
- **Magic from reagents**: thirteen spells. To cast one, you need a dose mixed from its reagents,
  and mana.
  - **Recipes**: four are in your spellbook from the start. People teach you the rest, or you can
    discover them by mixing by hand.
- **Tactical battles** on an eleven-by-eleven field, shaped by where the fight broke out: forest,
  hills, marsh, a dungeon hall, or the deck of your ship.
  - **Your turn**: each of the party steps, strikes, shoots along a line or casts.
  - **Running**: walking off the edge of the field runs away.
  - **Creatures**: those that aren't evil run when badly hurt.
  - **The computer**: press **Q** and it fights for you.
- **Creatures you can see coming**, roaming the land and the sea. Fire the ship's cannon at sea
  monsters before they reach you.
- **First-person dungeons**, four levels deep:
  - **The way down**: ladders join the levels.
  - **What you find**: fountains, chests, hidden traps, and the altar rooms.
  - **Light**: take torches, or the dark closes in.
  - **Treasure**: the finest arms in Merovale are in no shop; they lie in deep chests.
- **Danger that grows with you**: a new adventurer meets only the weakest things. Worse comes as the
  company rises in level and wanders further from the castle.
- **Saved journeys** in five slots, and a journal of everything you've learned.
- **Sound**: a tune each for the title, the land, the towns, the dungeons, the sea and battle, plus
  an effect for every step, blow, spell, moongate and stirring of a virtue, all mixed by OpenAL.
- **Attract mode**: leave the title alone for 30 seconds and a company sets out across the land.
- **Any window size**: the screen scales to fit, letterboxed. **Ctrl+S** saves a screenshot.
- **One self-contained executable** per platform: Windows, macOS and Linux on Intel/AMD and ARM.

## Screenshots

| The title | The Seer of the Crossroads | Ironcrag |
|:---------:|:-------------------------:|:--------:|
| ![The title](Images/AVAUltima4-title.png) | ![The seer](Images/AVAUltima4-seer.png) | ![Ironcrag](Images/AVAUltima4-town.png) |
| Both moons up over Castle Brightholm. | Seven questions, two virtues at a time. | The town of Daring. |

| Talking | The smith | Merovale |
|:-------:|:---------:|:--------:|
| ![Talking](Images/AVAUltima4-talk.png) | ![The smith](Images/AVAUltima4-shop.png) | ![Merovale](Images/AVAUltima4-land.png) |
| Gerrit gives the mantra, and a question. | Steel, with a trade-in. | Orcs coming, and a pilgrim in the hills. |

| A seeing-gem | A moongate | At sea |
|:------------:|:----------:|:------:|
| ![A seeing-gem](Images/AVAUltima4-gem.png) | ![A moongate](Images/AVAUltima4-moongate.png) | ![At sea](Images/AVAUltima4-sea.png) |
| The whole land, and the Undermere's isle. | Open while pale Selis is in its phase. | Off the isle of the Undermere. |

| Battle in the forest | Spark | Hollowroot |
|:--------------------:|:-----:|:----------:|
| ![Battle](Images/AVAUltima4-battle.png) | ![Spark](Images/AVAUltima4-battle-shot.png) | ![Hollowroot](Images/AVAUltima4-dungeon.png) |
| Orcs, goblins, an archer and a shaman. | The computer fighting, with magic. | A torch-lit passage. |

| An altar | The Shrine of Daring | Raised |
|:--------:|:--------------------:|:------:|
| ![An altar](Images/AVAUltima4-altar.png) | ![A shrine](Images/AVAUltima4-shrine.png) | ![Raised](Images/AVAUltima4-raised.png) |
| The Yellow Stone, and its guardians to come. | The rune opens the door. | Steadfast, the right mantra, and the flame rises. |

| The eight virtues | A character | The Codex of Echoes |
|:-----------------:|:-----------:|:-------------------:|
| ![The virtues](Images/AVAUltima4-virtues.png) | ![A character](Images/AVAUltima4-sheet.png) | ![The Codex](Images/AVAUltima4-codex.png) |
| Where each stands; the mark is steadfast. | Ash the fighter. | "Answer, and be known." |

The tests make these screenshots by playing the game in a real (headless) window; see
[Tests](#tests).

## Download and run

The [Releases page](https://github.com/Harlock123/AVAUltima4/releases/tag/latest) has ready-to-run builds
for Windows, macOS and Linux, on both Intel/AMD and ARM. **Latest build** is rebuilt on every push to
`main`. Each download is one self-contained executable, so there's no .NET to install.

| Platform | Archive |
|---|---|
| Windows | `AVAUltima4-win-x64.zip`, `AVAUltima4-win-x86.zip`, `AVAUltima4-win-arm64.zip` |
| macOS | `AVAUltima4-osx-arm64.tar.gz` (Apple silicon), `AVAUltima4-osx-x64.tar.gz` (Intel) |
| Linux | `AVAUltima4-linux-x64.tar.gz`, `AVAUltima4-linux-arm64.tar.gz`, `AVAUltima4-linux-arm.tar.gz` |

```bash
tar -xzf AVAUltima4-linux-x64.tar.gz     # or linux-arm64, linux-arm, osx-arm64, osx-x64
./AVAUltima4
```

On Windows, unzip the archive and run `AVAUltima4.exe`. On macOS, if Gatekeeper refuses to open it,
run `xattr -d com.apple.quarantine AVAUltima4` once. To play without sound, run it with `--no-audio`.

**Sound** comes through OpenAL Soft, which is built in for every platform. In a virtual machine the
game detects the VM and uses a larger audio buffer, so the sound doesn't break up.

## Controls

| On the land and in towns | |
|---|---|
| **Arrow keys** | Walk. Walk into people to talk, and into a shop counter to trade |
| **Enter** | Go into a town, shrine or dungeon; climb a ladder |
| **S** | Search here (and the squares around you) |
| **T** then an arrow | Talk to someone beside you |
| **M** / **C** | Mix reagents / cast a spell |
| **V** / **I** / **J** | The company and your virtues / what you carry / the journal |
| **G** | Peer into a seeing-gem |
| **L** | Light a torch |
| **F** then an arrow | Fire the ship's cannon |
| **Space** | Wait a turn |
| **Esc** | The menu: save, load, help, quit |

| In a dungeon | |
|---|---|
| **Up** / **Down** | Step forward / back |
| **Left** / **Right** | Turn |

| In a fight | |
|---|---|
| **Arrow keys** | Step; into a foe to strike it; off the edge to run |
| **A** then an arrow | Attack along a line (bows and slings reach far, halberds two squares) |
| **C** | Cast |
| **Space** | Wait |
| **Q** | Let the computer fight (**Q** again to take back command) |

| Talking | |
|---|---|
| Type a word, then **Enter** | Ask about it |
| **Enter** on nothing, or **BYE** | Leave |

**F2** or **Ctrl+M** mutes the sound. **Ctrl+S** saves a screenshot in a `Screenshots` folder beside
the program.

## Tests

```bash
dotnet test AVAUltima4.slnx
```

The engine has no UI and takes its keys by name, so the tests play it exactly as a person does. They
check:

- **The land**: it comes out the same every time.
  - **Walking**: every town, shrine, dungeon and moongate can be walked to from the castle, and the
    Undermere can't.
  - **Sailing**: the ship can sail from its mooring to the isle and to the sunken horn.
- **The towns**: every shopkeeper, person and hiding place can be reached. Every clue fills in from
  the land, and points the right way.
- **The dungeons**: every level joins up its ladders, altars and seals. Each stone rests on exactly
  one altar.
- **The seer**: seven questions, one for every pair of virtues, and the virtue left standing chooses
  the calling.
- **Talking**:
  - **Matching**: the four-letter rule, and topics and the journal.
  - **Questions**: each asked once, and the answer moves a virtue.
  - **People**: beggars, companions who join only the virtuous, and the Queen raising levels.
- **Every virtue's deeds**:
  - **Coin and blood**: the forgetful merchant, a stranger's chest, the lost purse, giving blood.
  - **The road**: greeting travellers or attacking them, striking fleeing creatures, running from a
    fight.
- **The treasures**: runes, the tome, the taper, the horn from the ship, the stones from their
  guardians, and the arms in deep chests.
- **Shrines**: closed without the rune; the wrong mantra does nothing. They raise only the
  steadfast.
- **Magic**: mixing known recipes, discovering others by hand, and casting in the field.
- **Fights**: winning, running, sleep, and the balance between a lone newcomer and a seasoned
  company.
- **The whole quest**, played to the end:
  - **The clues**: runes found where people say, each shrine's mantra taken from what its town's
    people say.
  - **The descent**: the stones won and the keys gathered, all eight shrines, the Queen's last word,
    the door and the four seals.
  - **The end**: the Codex.
- **The machine**: saving and loading, hunger, defeat, and the attract demo.

The screenshot tests draw the real screen in a headless window. To refresh the README's pictures:

```bash
ULTIMA4_SHOTS=$PWD/Images dotnet test tests/AVAUltima4.Tests --filter ScreenshotTests
```

To see how fights go for each size and level of party against each tier of creature (for tuning):

```bash
ULTIMA4_BALANCE=1 dotnet test tests/AVAUltima4.Tests --filter BalanceProbe --logger "console;verbosity=detailed"
```

## Building

```bash
dotnet build AVAUltima4.slnx
dotnet run --project src/AVAUltima4
```

You'll need the .NET 10 SDK. `tools/make_sounds.py` makes the sounds and needs only standard-library
Python: `python3 tools/make_sounds.py` rewrites `src/AVAUltima4/Sounds`.

## Project structure

```
AVAUltima4/
├── AVAUltima4.slnx
├── src/AVAUltima4/
│   ├── Rules/                     # Virtues, callings, characters, arms, monsters, spells, the RNG
│   ├── World/
│   │   ├── Overworld.cs           # The land of Merovale, grown from a fixed seed
│   │   ├── TownMap.cs             # A town laid out from its lots: shops, houses, gardens
│   │   ├── People.cs              # Every town, everyone in it, what they say, and the seer's questions
│   │   ├── Dungeon.cs             # Maze levels, ladders, altars, chests and the Undermere's seals
│   │   ├── WorldState.cs          # A saved journey: party, virtues, treasures, moons, flags
│   │   └── SaveStore.cs
│   ├── Combat/                    # The tactical battlefield
│   ├── Game/
│   │   ├── GameEngine.cs          # Panels and keys
│   │   ├── GameEngine.Title.cs    # The title, the seer, a new life, the demo
│   │   ├── GameEngine.Move.cs     # The land, towns, moongates, the ship, roaming creatures
│   │   ├── GameEngine.Talk.cs     # Conversations, questions, beggars, companions, the Queen
│   │   ├── GameEngine.Shops.cs    # Every kind of shop, healer and inn
│   │   ├── GameEngine.Magic.cs    # Mixing and casting
│   │   ├── GameEngine.Dungeon.cs  # Dungeons, altars, the door and seals of the Undermere
│   │   ├── GameEngine.Shrine.cs   # Meditation, and the Codex
│   │   ├── GameEngine.Battle.cs, GameEngine.Party.cs, DemoPilot.cs
│   │   └── SoundManager.cs
│   ├── Audio/                     # The OpenAL engine (from the author's AVABand)
│   ├── Views/                     # The land, towns, dungeons, battles, panels and the pixel art
│   └── Sounds/                    # The synthesised effects and tunes
├── tests/AVAUltima4.Tests/        # World, gameplay, whole-quest and screenshot tests
├── tools/make_sounds.py
└── Images/                        # Screenshots
```

## About and licence

AVAUltima4 © 2026 Lonnie Watson, under the [MIT License](LICENSE).

*Ultima IV: Quest of the Avatar* © 1985 Origin Systems; *Ultima* is a trademark of Electronic Arts.
The names belong to their current owners. AVAUltima4 is an independent, unofficial game in its
style. Its land, people, virtues, mantras, spells, quest, graphics, sounds, tunes and code are its
own, and nothing is taken from that game. No rights holder is affiliated with or endorses it.
