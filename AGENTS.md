# AGENTS.md

Notes for anyone, human or AI agent, changing this code. `README.md` covers the quest, the controls, what the tests
check and downloads; this file covers how the code is put together and the rules that keep it working.

## What this is

A role-playing quest in the style of Origin's 1985 Ultima IV, in C# on .NET 10 and Avalonia 12 (12.1.4). The land of
Merovale, its towns, people, virtues, spells, monsters, pictures and words are all original. Everything is drawn
in code; the sound effects and tunes are synthesised by `tools/make_sounds.py`. One self-contained executable per
platform (Windows, macOS and Linux, x64 and ARM).

## Commands

```bash
dotnet build AVAUltima4.slnx                      # the SDK version is in global.json (10.0.400 or later)
dotnet test AVAUltima4.slnx                       # every test runs headless: no window opens
dotnet run --project src/AVAUltima4               # play it; --no-audio for silence
ULTIMA4_SHOTS=$PWD/Images dotnet test tests/AVAUltima4.Tests --filter ScreenshotTests   # refresh the README's pictures
ULTIMA4_BALANCE=1 dotnet test tests/AVAUltima4.Tests --filter BalanceProbe --logger "console;verbosity=detailed"
python3 tools/make_sounds.py                      # rebuild src/AVAUltima4/Sounds (standard-library Python only)
```

`BalanceProbe` prints fight outcomes per party size, level and creature tier; without `ULTIMA4_BALANCE=1` it
returns at once. `Directory.Build.props` sets `TreatWarningsAsErrors`, nullable reference types, implicit usings
and invariant globalization for every project: a warning fails the build, so keep it at zero.

## Layout

- `src/AVAUltima4/Game/`: the game, with no Avalonia drawing in it.
  - `GameEngine.cs` holds the panel stack, the clock (`Update(dt)`) and the key entry points `Press(name)` and
    `Type(text)`. Partial files split the rest by topic: `.Title` (title, seer, new life, demo), `.Move` (land,
    towns, moongates, ship, roaming creatures), `.Talk`, `.Shops`, `.Magic`, `.Dungeon`, `.Shrine`, `.Battle`
    and `.Party`.
  - `Panel.cs` is a menu or message on the stack. `DemoPilot.cs` plays the attract demo. `SoundManager.cs` and
    `Sfx.cs` play the effects.
- `src/AVAUltima4/World/`: `Overworld` (the land), `TownMap`, `People` (every town, person, line and the seer's
  questions), `Dungeon`, `Terrain` (tiles and `Dir`), `WorldState` (everything a save holds) and `SaveStore`.
- `src/AVAUltima4/Rules/`: virtues, classes, characters, items, monsters, spells, colours and `Rng`.
- `src/AVAUltima4/Combat/`: `Battle` (the tactical battlefield, run by `Tick(dt)`) and `Fighter`.
- `src/AVAUltima4/Views/`: `GameCanvas` (split over partial files) maps keys to names, runs the 16 ms timer and
  draws the land, towns, dungeons, battles and panels. `Sprites.cs` holds the pixel art.
- `src/AVAUltima4/Audio/`: the OpenAL Soft audio engine, shared with the author's other AVA games.
- `tests/AVAUltima4.Tests/`: xUnit v3 with `Avalonia.Headless.XUnit`: world, gameplay, whole-quest, screenshot and
  balance tests. `Helpers.cs` sets up games.

## Rules that keep it working

- **Engine first, drawing second.** Gameplay belongs in `Game/`, `World/`, `Rules/` and `Combat/`, where tests reach
  it without a window. `Views/` only reads the engine and draws it.
- **Keys go in by name.** `GameCanvas.KeyName` turns Avalonia keys into names ("Up", "Enter", "Esc", "A"...), and
  the engine takes only those names through `Press`, with typed text through `Type`. Tests play the game the same
  way, so test new controls through `Press`. Directions map by `Dirs.FromKey`: "Up" is north, and north is
  `dy = -1`.
- **One random source.** All chance comes from the engine's `Rng`, a xorshift generator whose state is saved in
  `WorldState.Seed`, so a loaded game carries on where it left off. Never use `Random` in game logic (only the
  title art uses fixed-seed `Random`s).
- **The land is fixed.** `Overworld.Build` tries seeds from 7 upward and keeps the first land that passes its checks,
  and the tests rely on its towns, shrines and moongates being reachable. A change to the generator changes the
  land for every save, so run the whole test suite after it.
- **Time steps vary.** `GameEngine.Update(dt)` and `Battle.Tick(dt)` take the real frame time (capped at 0.1 s).
  Tests drive them with fixed steps, as `Battle.RunToEnd` does.
- **Audio.** OpenAL Soft through Silk.NET; `AudioDecoders` reads WAV, Ogg (NVorbis) and MP3 (NLayer). `Directory.Build.targets`
  swaps in the official Windows x64/x86 `soft_oal.dll` from `Audio/native/`, which needs no Visual C++ runtime. In
  a virtual machine `OpenAlConfig` writes a larger mixing period to `ALSOFT_CONF`.
- **Tests never touch the player's files.** Saves live in the roaming app-data folder (`AVAUltima4`, five slots as
  `save<n>.json`). Tests use a fresh temporary folder (`Helpers.TempSaves()`) and a silent `SoundManager`.
- **Line endings.** The sources use LF.

## Making a change

- Add or update tests with every gameplay change. `dotnet test` must pass, with no warnings.
- If the change shows on screen, refresh the screenshots with the command above, and look at them.
- Update `README.md` (the quest, controls, the test list) to match.
- New sounds go into `tools/make_sounds.py`, not as hand-edited WAV files.

## CI and releases

`.github/workflows/build.yml` builds and tests every push and pull request with .NET 10. It then publishes
self-contained single-file builds for win-x64/x86/arm64, linux-x64/arm64/arm and osx-x64/arm64, kept as workflow
artifacts. Only pushes to `main` replace the assets of the `latest` pre-release, and a `v*` tag makes a proper
release. After a release it asks the portfolio site to rebuild, if the `SITE_REFRESH_TOKEN` secret is set.
