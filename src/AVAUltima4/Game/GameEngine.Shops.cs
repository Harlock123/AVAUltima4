using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Game;

public sealed partial class GameEngine
{
    public static readonly int[] ReagentPrice = [6, 4, 5, 6, 8, 7, 9, 10];
    public const int ShipPrice = 600;

    /// <summary>The shopkeeper the party is trading with.</summary>
    public TownNpc? Keeper { get; private set; }

    public void OpenShop(TownNpc keeper)
    {
        Keeper = keeper;
        switch (keeper.Shop)
        {
            case "weapons": ArmsShop(keeper, weapons: true); break;
            case "armour": ArmsShop(keeper, weapons: false); break;
            case "food": FoodShop(keeper); break;
            case "reagents": ReagentShop(keeper); break;
            case "healer": Healer(keeper); break;
            case "inn": Inn(keeper); break;
            case "guild": Guild(keeper); break;
            case "ship": Shipwright(keeper); break;
        }
    }

    private Panel ShopPanel(TownNpc keeper, string greeting)
    {
        var p = new Panel { Title = keeper.Name, Kind = PanelKind.Shop, Picture = keeper.Sprite, Back = Pop };
        p.Say(greeting);
        return p;
    }

    private bool Pay(int gold)
    {
        if (World.Gold < gold)
        {
            Note("You haven't the gold.");
            Play(Sfx.Bump);
            return false;
        }
        World.Gold -= gold;
        Play(Sfx.Coin);
        return true;
    }

    // ───────────────────────────── arms and armour ─────────────────────────────

    private void ArmsShop(TownNpc keeper, bool weapons)
    {
        var def = Town!.Def;
        Show(() =>
        {
            var p = ShopPanel(keeper, weapons ? "\"Steel for sale. Choose, and I'll take your old one in trade.\"" : "\"Armour, made to measure. I'll take what you're wearing in trade.\"");
            var stock = weapons ? def.Weapons.Select(id => (Id: id, Arms.Weapon(id).Name, Arms.Weapon(id).Price, About: $"{Arms.Weapon(id).Damage} damage{(Arms.Weapon(id).Range > 1 ? $", {Arms.Weapon(id).Range} squares" : Arms.Weapon(id).Range == 1 ? ", reaches two squares" : "")}. {Arms.Weapon(id).About}"))
                                : def.Armours.Select(id => (Id: id, Arms.Armour(id).Name, Arms.Armour(id).Price, About: $"Defence {Arms.Armour(id).Defence}. {Arms.Armour(id).About}"));
            foreach (var item in stock)
            {
                var it = item;
                var who = string.Join(", ", World.Party.Where(c => Arms.CanUse(c.Class, it.Id)).Select(c => c.Name));
                p.Add($"{it.Name,-12} {it.Price,4} gold", () => BuyArms(keeper, it.Id, it.Price, weapons), note: it.About + (who.Length > 0 ? $" For: {who}." : " Nobody in your company can use it."));
            }
            p.Add("Leave", Pop);
            return p;
        });
    }

    private void BuyArms(TownNpc keeper, string id, int price, bool weapon)
    {
        var p = new Panel { Title = "For whom?", Kind = PanelKind.Shop, Picture = keeper.Sprite, Back = Pop };
        foreach (var c in World.Party)
        {
            var who = c;
            bool can = Arms.CanUse(who.Class, id);
            string now = weapon ? who.WeaponItem.Name : who.ArmourItem.Name;
            int trade = (weapon ? who.WeaponItem.Price : who.ArmourItem.Price) / 2;
            p.Add($"{who.Name} ({who.Title}, has {now})", () =>
            {
                if (!Pay(Math.Max(0, price - trade))) return;
                if (weapon) who.Weapon = id;
                else who.Armour = id;
                Note(trade > 0 ? $"{who.Name} trades in the {now} and takes the {(weapon ? who.WeaponItem.Name : who.ArmourItem.Name)}." : $"{who.Name} takes the {(weapon ? who.WeaponItem.Name : who.ArmourItem.Name)}.");
                Pop();
            }, can, can ? (trade > 0 ? $"Costs {Math.Max(0, price - trade)} after trading in the {now}." : null) : $"A {who.Title.ToLowerInvariant()} can't use that.");
        }
        p.Add("Nobody", Pop);
        Push(p);
    }

    // ───────────────────────────── food ─────────────────────────────

    private void FoodShop(TownNpc keeper)
    {
        Show(() =>
        {
            var p = ShopPanel(keeper, $"\"Good plain food for the road. You have {World.Food} portions.\"");
            p.Add("25 portions for 25 gold", () => { if (Pay(25)) World.Food += 25; });
            p.Add("100 portions for 90 gold", () => { if (Pay(90)) World.Food += 100; });
            p.Add("Leave", Pop);
            return p;
        });
    }

    // ───────────────────────────── reagents ─────────────────────────────

    private void ReagentShop(TownNpc keeper)
    {
        var stock = Town!.Def.Reagents;
        Show(() =>
        {
            var p = ShopPanel(keeper, "\"Reagents, fresh and dry. Mix them yourself; I don't do spells.\"");
            foreach (var r in stock)
            {
                var reagent = r;
                p.Add($"{reagent,-10} {ReagentPrice[(int)reagent],3} gold   (you have {World.Reagents[(int)reagent]})", () => BuyReagent(keeper, reagent));
            }
            p.Add("Leave", Pop);
            return p;
        });
    }

    private void BuyReagent(TownNpc keeper, Reagent r)
    {
        int price = ReagentPrice[(int)r];
        if (World.Gold < price)
        {
            Note("You haven't the gold.");
            Play(Sfx.Bump);
            return;
        }
        World.Reagents[(int)r]++;
        // Now and then the keeper forgets to take the money: a small test of Candor.
        if (!World.Visit.Contains("honesty") && Rng.Chance(40))
        {
            World.Visit.Add("honesty");
            var q = new Panel { Title = keeper.Name, Kind = PanelKind.Shop, Picture = keeper.Sprite };
            q.Say($"{keeper.Name} wraps the {r.ToString().ToLowerInvariant()} and hands it over, talking all the while - and forgets to take your money.");
            q.Add("Pay anyway", () =>
            {
                Pop();
                World.Gold -= price;
                Play(Sfx.Coin);
                Note("\"Oh! Honest folk are rare. Thank you.\"");
                ChangeVirtue(Virtue.Candor, 4);
            });
            q.Add("Say nothing", () =>
            {
                Pop();
                Note("You walk off with it unpaid.");
                ChangeVirtue(Virtue.Candor, -3);
            });
            Push(q);
            return;
        }
        World.Gold -= price;
        Play(Sfx.Coin);
    }

    // ───────────────────────────── the healer ─────────────────────────────

    private void Healer(TownNpc keeper)
    {
        Show(() =>
        {
            var p = ShopPanel(keeper, "\"I can cure poison, close wounds, and - for those who can pay - call back the dead. And I am always short of blood.\"");
            p.Add("Cure poison (30 gold)", () => ForWhom(keeper, c => c.Alive && c.Status == Status.Poisoned, c =>
            {
                if (!Pay(30)) return;
                c.Status = Status.Good;
                Play(Sfx.Heal);
                Note($"{c.Name} is cured.");
            }), World.Party.Any(c => c.Status == Status.Poisoned));
            p.Add("Heal wounds (40 gold)", () => ForWhom(keeper, c => c.Alive && c.Hp < c.MaxHp, c =>
            {
                if (!Pay(40)) return;
                c.Hp = c.MaxHp;
                Play(Sfx.Heal);
                Note($"{c.Name} is healed.");
            }), World.Party.Any(c => c.Alive && c.Hp < c.MaxHp));
            p.Add("Raise the dead (300 gold)", () => ForWhom(keeper, c => !c.Alive, c =>
            {
                if (!Pay(300)) return;
                c.Revive();
                c.Hp = c.MaxHp;
                Play(Sfx.Heal);
                Note($"{c.Name} breathes again!");
            }), World.Party.Any(c => !c.Alive));
            p.Add("Give blood", GiveBlood, !World.Visit.Contains("blood") && World.Leader.Alive, World.Visit.Contains("blood") ? "You have given today." : "It will weaken you for a while.");
            p.Add("Leave", Pop);
            return p;
        });
    }

    private void GiveBlood()
    {
        var c = World.Leader;
        World.Visit.Add("blood");
        c.Hp = Math.Max(1, c.Hp - c.MaxHp / 4);
        Note("\"Thank you. This will save someone.\"");
        Play(Sfx.Hurt);
        ChangeVirtue(Virtue.Selflessness, 4);
    }

    private void ForWhom(TownNpc keeper, Func<Character, bool> needs, Action<Character> act)
    {
        var p = new Panel { Title = "For whom?", Kind = PanelKind.Shop, Picture = keeper.Sprite, Back = Pop };
        foreach (var c in World.Party.Where(needs))
        {
            var who = c;
            p.Add($"{who.Name}  {Math.Max(0, who.Hp)}/{who.MaxHp}{(who.Status == Status.Poisoned ? " poisoned" : !who.Alive ? " dead" : "")}", () => { act(who); Pop(); });
        }
        p.Add("Nobody", Pop);
        Push(p);
    }

    // ───────────────────────────── the inn ─────────────────────────────

    public int InnPrice => 10 * World.Party.Count(c => c.Alive);

    private void Inn(TownNpc keeper)
    {
        Show(() =>
        {
            var p = ShopPanel(keeper, "\"Beds, warm and dry. A night's rest sets most things right.\"");
            p.Add($"Rest for the night ({InnPrice} gold)", () =>
            {
                if (!Pay(InnPrice)) return;
                foreach (var c in Living)
                {
                    c.Hp = c.MaxHp;
                    c.Mp = c.MaxMp;
                }
                World.Turn += 80;
                Play(Sfx.Heal);
                Note("You sleep well, and wake refreshed.");
                Pop();
            });
            p.Add("Leave", Pop);
            return p;
        });
    }

    // ───────────────────────────── the guild ─────────────────────────────

    private void Guild(TownNpc keeper)
    {
        Show(() =>
        {
            var p = ShopPanel(keeper, $"\"Torches for the dark places, and seeing-gems for the lost. You have {World.Torches} torches and {World.Gems} gems.\"");
            p.Add("A torch (6 gold)", () => { if (Pay(6)) World.Torches++; }, note: "Lights a dungeon for a good while (L).");
            p.Add("Five torches (25 gold)", () => { if (Pay(25)) World.Torches += 5; });
            p.Add("A seeing-gem (60 gold)", () => { if (Pay(60)) World.Gems++; }, note: "Peer into it (G) to see the land, or a dungeon level, laid out like a map.");
            p.Add("Leave", Pop);
            return p;
        });
    }

    // ───────────────────────────── the shipwright ─────────────────────────────

    private void Shipwright(TownNpc keeper)
    {
        Show(() =>
        {
            var p = ShopPanel(keeper, World.HasShip
                ? "\"How's she sailing? Bring her back any time.\""
                : $"\"Want a ship? I've one just finished, moored by the town. {ShipPrice} gold, and she's yours.\"");
            p.Add($"Buy the ship ({ShipPrice} gold)", () =>
            {
                if (!Pay(ShipPrice)) return;
                World.HasShip = true;
                (World.ShipX, World.ShipY) = Land.ShipStart;
                Note("The ship is yours! She's moored by the town; walk aboard to sail.");
                Play(Sfx.Chime);
                Pop();
            }, !World.HasShip, World.HasShip ? "You already have a ship." : "Walk onto her to board; sail onto land to go ashore. F fires her cannon.");
            p.Add("Leave", Pop);
            return p;
        });
    }
}
