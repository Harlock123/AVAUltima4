using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Game;

public sealed partial class GameEngine
{
    /// <summary>The reagents picked out for mixing by hand.</summary>
    public HashSet<Reagent> Picked { get; } = [];

    public void MixMenu()
    {
        Show(() =>
        {
            var p = new Panel { Title = "Mix reagents", Kind = PanelKind.Big, Back = Pop };
            p.Say("A dose of a spell is mixed from one of each reagent in its recipe. Mix the recipes you know, or try a mixture by hand.");
            foreach (var s in Spell.All.Where(s => World.Known.Contains(s.Id)))
            {
                var spell = s;
                bool can = spell.Recipe.All(r => World.Reagents[(int)r] > 0);
                p.Add($"{spell.Name,-9} ({World.Doses.GetValueOrDefault(spell.Id)} mixed)  {spell.RecipeText}", () => Mix(spell.Recipe), can,
                    can ? spell.About + $" Costs {spell.Mana} mana to cast." : "You're out of something it needs.");
            }
            p.Add("Mix by hand...", MixByHand);
            p.Add("Done", Pop);
            return p;
        });
    }

    private void MixByHand()
    {
        Picked.Clear();
        Show(() =>
        {
            var p = new Panel { Title = "Mix by hand", Kind = PanelKind.Big, Back = Pop };
            p.Say("Pick out reagents, then mix. A right mixture makes a dose, and you'll remember its recipe; a wrong one is wasted.");
            foreach (var r in Enum.GetValues<Reagent>())
            {
                var reagent = r;
                p.Add($"[{(Picked.Contains(reagent) ? "x" : " ")}] {reagent,-10} ({World.Reagents[(int)reagent]})", () =>
                {
                    if (!Picked.Remove(reagent)) Picked.Add(reagent);
                }, World.Reagents[(int)reagent] > 0 || Picked.Contains(reagent));
            }
            p.Add("Mix them", () => { Mix([.. Picked]); Picked.Clear(); }, Picked.Count > 0);
            p.Add("Done", Pop);
            return p;
        });
    }

    /// <summary>Mixes one dose from a set of reagents, using one of each.</summary>
    public Spell? Mix(IReadOnlyCollection<Reagent> reagents)
    {
        if (reagents.Count == 0 || reagents.Any(r => World.Reagents[(int)r] <= 0)) return null;
        foreach (var r in reagents) World.Reagents[(int)r]--;
        Play(Sfx.Mix);
        var spell = Spell.FromRecipe(reagents);
        if (spell is null)
        {
            Note("The mixture smokes and turns to dust.");
            return null;
        }
        World.Doses[spell.Id] = World.Doses.GetValueOrDefault(spell.Id) + 1;
        if (World.Known.Add(spell.Id)) Note($"You have found the recipe for {spell.Name}!");
        Note($"You mix a dose of {spell.Name}.");
        return spell;
    }

    // ───────────────────────────── casting out of a fight ─────────────────────────────

    public void CastMenu()
    {
        Show(() =>
        {
            var p = new Panel { Title = "Cast", Kind = PanelKind.Big, Back = Pop };
            var spells = Spell.All.Where(s => s.Use.HasFlag(SpellUse.Field) && World.Doses.GetValueOrDefault(s.Id) > 0).ToList();
            if (spells.Count == 0) p.Say("You have no spells mixed that work here. M mixes reagents.");
            foreach (var s in spells)
            {
                var spell = s;
                p.Add($"{spell.Name,-9} x{World.Doses[spell.Id]}  ({spell.Mana} mana)", () => PickCaster(spell), note: spell.About);
            }
            p.Add("Done", Pop);
            return p;
        });
    }

    private void PickCaster(Spell s)
    {
        var p = new Panel { Title = $"Who casts {s.Name}?", Kind = PanelKind.Big, Back = Pop };
        foreach (var c in World.Party)
        {
            var who = c;
            bool can = who.Alive && who.Mp >= s.Mana;
            p.Add($"{who.Name,-10} {who.Mp}/{who.MaxMp} mana", () =>
            {
                Pop();
                if (s.Target == SpellTarget.Ally) PickAlly(s, who);
                else CastField(s, who, null);
            }, can, can ? null : who.Alive ? "Not enough mana." : "Dead.");
        }
        p.Add("Nobody", Pop);
        Push(p);
    }

    private void PickAlly(Spell s, Character caster)
    {
        var p = new Panel { Title = $"{s.Name} on whom?", Kind = PanelKind.Big, Back = Pop };
        foreach (var c in World.Party)
        {
            var who = c;
            p.Add($"{who.Name,-10} {Math.Max(0, who.Hp)}/{who.MaxHp}{(who.Status == Status.Poisoned ? " poisoned" : !who.Alive ? " dead" : "")}", () =>
            {
                Pop();
                CastField(s, caster, who);
            });
        }
        p.Add("Nobody", Pop);
        Push(p);
    }

    /// <summary>Casts a spell outside a fight. Returns false (and spends nothing) if it can't work here.</summary>
    public bool CastField(Spell s, Character caster, Character? target)
    {
        bool dungeon = World.Where == Where.Dungeon;
        string? why = s.Id switch
        {
            "rise" or "return" when !dungeon => "That only works underground.",
            "wayfare" when World.Where != Where.World || World.Aboard => "That only works out in the open land.",
            "rekindle" when target is { Alive: true } => $"{target.Name} isn't dead.",
            "mend" when target is { Alive: false } => "Too late for mending.",
            _ => null,
        };
        if (why is not null)
        {
            Note(why);
            Play(Sfx.Bump);
            return false;
        }
        if (caster.Mp < s.Mana || World.Doses.GetValueOrDefault(s.Id) <= 0) return false;
        caster.Mp -= s.Mana;
        World.Doses[s.Id]--;
        Play(Sfx.Spell);
        Flash = 0.25;
        Note($"{caster.Name} casts {s.Name}.");
        switch (s.Id)
        {
            case "mend":
                target!.Heal(20 + Rng.Range(2, 16));
                Play(Sfx.Heal);
                break;
            case "purge":
                if (target!.Status == Status.Poisoned) target.Status = Status.Good;
                Play(Sfx.Heal);
                break;
            case "restore":
                foreach (var c in Living) c.Heal(20 + Rng.Range(2, 16));
                Play(Sfx.Heal);
                break;
            case "rekindle":
                target!.Revive();
                Play(Sfx.Heal);
                Note($"{target.Name} breathes again!");
                break;
            case "glow":
                World.Light = Math.Max(World.Light, 200);
                Note("A soft light fills the air around you.");
                break;
            case "rise":
                if (World.Level == 1) LeaveDungeon();
                else
                {
                    World.Level--;
                    (World.X, World.Y) = Dungeons.Level(World.Place, World.Level).Down;
                    Note($"You rise to level {World.Level}.");
                }
                break;
            case "return":
                LeaveDungeon();
                break;
            case "wayfare":
            {
                var castle = Land.Site("brightholm");
                World.X = castle.X;
                World.Y = castle.Y;
                Roamers.Clear();
                Note("The world folds, and you stand before Castle Brightholm.");
                break;
            }
        }
        return true;
    }
}
