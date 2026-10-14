using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Game;

public sealed partial class GameEngine
{
    public void PartyMenu()
    {
        Show(() =>
        {
            var p = new Panel { Title = "Your company", Kind = PanelKind.Big, Back = Pop };
            for (int i = 0; i < World.Party.Count; i++)
            {
                int at = i;
                var c = World.Party[i];
                p.Add($"{c.Name,-10} L{c.Level} {c.Title,-9} {Math.Max(0, c.Hp),3}/{c.MaxHp,-3} {(c.Alive ? c.Status == Status.Poisoned ? "poisoned" : "" : "dead")}", () => ViewMember(at));
            }
            p.Add("Your virtues", Virtuesheet);
            p.Add("Done", Pop);
            return p;
        });
    }

    public void ViewMember(int index)
    {
        int at = index;
        var p = new Panel { Title = World.Party[at].Name, Kind = PanelKind.Sheet, Who = World.Party[at], Back = Pop };
        p.Turn = d =>
        {
            at = (at + d + World.Party.Count) % World.Party.Count;
            p.Who = World.Party[at];
            p.Title = World.Party[at].Name;
        };
        if (index > 0)
            p.Add("Part company", () =>
            {
                var c = World.Party[at];
                if (at == 0) return;
                World.Party.Remove(c);
                World.Flags.Remove("joined:" + c.Name);
                Note($"{c.Name} leaves the company, and goes home.");
                Pop();
                Pop();
            }, note: "They will go back to their town, and might join you again there.");
        p.Add("Done", Pop);
        Push(p);
    }

    private void Virtuesheet()
    {
        var p = new Panel { Title = "The eight virtues", Kind = PanelKind.Sheet, Back = Pop };
        p.Add("Done", Pop);
        Push(p);
    }

    public void ItemsPanel()
    {
        var w = World;
        var p = new Panel { Title = "What you carry", Kind = PanelKind.Big, Back = Pop };
        p.Say($"Gold {w.Gold}   Food {w.Food}   Torches {w.Torches}   Gems {w.Gems}{(w.Light > 0 ? $"   Light {w.Light}" : "")}");
        p.Say("Reagents: " + string.Join(", ", Enum.GetValues<Reagent>().Select(r => $"{r} {w.Reagents[(int)r]}")));
        var doses = w.Doses.Where(d => d.Value > 0).Select(d => $"{Spell.Get(d.Key).Name} {d.Value}").ToList();
        p.Say("Mixed: " + (doses.Count == 0 ? "nothing" : string.Join(", ", doses)));
        p.Say("Runes: " + (w.Runes.Count == 0 ? "none" : string.Join(", ", w.Runes.OrderBy(v => v))));
        p.Say("Stones: " + (w.Stones.Count == 0 ? "none" : string.Join(", ", w.Stones.OrderBy(v => v).Select(v => Virtues.Get(v).Stone))));
        p.Say("Keys: " + (w.Keys.Count == 0 ? "none" : string.Join(", ", KeyOrder.Where(w.Keys.Contains).Select(KeyName))));
        p.Say(w.HasShip ? $"Your ship is {(w.Aboard ? "under you" : $"moored at {w.ShipX},{w.ShipY}")}." : "You have no ship.");
        p.Add("Done", Pop);
        Push(p);
    }

    public void Journal()
    {
        var p = new Panel { Title = "Journal", Kind = PanelKind.Big, Back = Pop };
        if (World.Notes.Count == 0) p.Say("Nothing written yet. Clues people give you are written down here.");
        foreach (var n in Enumerable.Reverse(World.Notes)) p.Add(n, null);
        p.Add("Close", Pop);
        Push(p);
    }

    public void GameMenu()
    {
        var p = new Panel { Title = "Menu", Back = Pop };
        p.Add("Back to the game", Pop);
        p.Add("Save the journey", SaveMenu, !InDemo);
        p.Add("Load a journey", LoadMenu, Saves.Any());
        p.Add("How to play", Help);
        p.Add("Quit to the title", ShowTitle);
        Push(p);
    }

    private void SaveMenu()
    {
        Show(() =>
        {
            var p = new Panel { Title = "Save", Back = Pop };
            for (int s = 1; s <= SaveStore.Slots; s++)
            {
                int slot = s;
                p.Add(Saves.Describe(slot), () =>
                {
                    World.Seed = Rng.State;
                    Saves.Save(slot, World);
                    Note($"Saved in slot {slot}.");
                    CloseAll();
                });
            }
            p.Add("Cancel", Pop);
            return p;
        });
    }

    public void LoadMenu()
    {
        Show(() =>
        {
            var p = new Panel { Title = "Load", Back = Pop };
            for (int s = 1; s <= SaveStore.Slots; s++)
            {
                int slot = s;
                p.Add(Saves.Describe(slot), () => Load(slot), Saves.Load(slot) is not null);
            }
            p.Add("Cancel", Pop);
            return p;
        });
    }

    public bool Load(int slot)
    {
        var w = Saves.Load(slot);
        if (w is null) return false;
        World = w;
        Rng = new Rng(w.Seed);
        Demo = null;
        Battle = null;
        CloseAll();
        Roamers.Clear();
        Log.Clear();
        Mode = Mode.Play;
        Play(Sfx.MusicStop);
        if (w.Where == Where.Town)
        {
            Town = TownMap.Fresh(w.Place);
            Town.Npcs.RemoveAll(n => n.Def is { Kind: NpcKind.Companion } c && w.Has("joined:" + c.Name));
            Play(Sfx.TownMusic);
        }
        else
        {
            Town = null;
            Play(w.Where == Where.Dungeon ? Sfx.DungeonMusic : Sfx.WorldMusic);
        }
        Note("Your journey continues.");
        return true;
    }
}
