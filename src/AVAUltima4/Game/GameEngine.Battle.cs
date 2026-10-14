using AVAUltima4.Combat;
using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Game;

/// <summary>What the battle controls are doing for the member whose turn it is.</summary>
public enum BattleUi { Command, Aim, Spells, Ally }

public sealed partial class GameEngine
{
    public BattleUi Ui { get; private set; }
    public int UiIndex { get; private set; }
    /// <summary>The spell being aimed, or null when aiming a weapon.</summary>
    public Spell? Aiming { get; private set; }
    private double _battleOver;
    private Action? _afterWin;
    private Fighter? _uiFor;

    /// <summary>Starts a fight. <paramref name="won"/> runs if the party wins it.</summary>
    public void StartBattle(List<Monster> foes, Ground ground, Action? won = null)
    {
        CloseAll();
        Pending = null;
        Battle = new Battle(World.Party, foes, ground, World, Rng);
        Battle.Sound += Play;
        if (InDemo) Battle.PartyQuick = true;
        _afterWin = won;
        _battleOver = 0;
        Ui = BattleUi.Command;
        _uiFor = null;
        Mode = Mode.Battle;
        Play(Sfx.MusicStop);
        Play(Sfx.BattleMusic);
    }

    private void SyncUi()
    {
        var c = Battle?.Current;
        if (c == _uiFor) return;
        _uiFor = c;
        Ui = BattleUi.Command;
        Aiming = null;
    }

    private void BattleKey(string key)
    {
        var b = Battle;
        if (b is null) return;
        if (b.Outcome != Outcome.None)
        {
            if (key is "Enter" or "Space") _battleOver = 99;
            return;
        }
        if (!b.WaitingForPlayer)
        {
            if (key is "Q" or "Esc" && b.PartyQuick)
            {
                b.PartyQuick = false;
                b.Say("You take back command.");
            }
            return;
        }
        SyncUi();
        var c = b.Current!;
        var dir = Dirs.FromKey(key);
        switch (Ui)
        {
            case BattleUi.Command:
                if (dir is { } d)
                {
                    if (!b.Move(d)) Play(Sfx.Bump);
                    break;
                }
                switch (key)
                {
                    case "A":
                        Ui = BattleUi.Aim;
                        Aiming = null;
                        b.Say($"{c.Name}: attack which way?");
                        break;
                    case "C":
                        if (!b.Castable(c).Any()) { b.Say("No spells mixed. (M mixes reagents, out of a fight.)"); break; }
                        Ui = BattleUi.Spells;
                        UiIndex = 0;
                        break;
                    case "Space" or "Enter": b.Pass(); break;
                    case "Q":
                        b.PartyQuick = true;
                        b.Say("The computer takes command (Q to take it back).");
                        break;
                }
                break;
            case BattleUi.Aim:
                if (dir is { } ad)
                {
                    if (Aiming is { } s) b.Cast(s.Id, ad);
                    else b.Attack(ad);
                    Ui = BattleUi.Command;
                }
                else if (key is "Esc" or "Back") Ui = BattleUi.Command;
                break;
            case BattleUi.Spells:
            {
                var list = b.Castable(c).ToList();
                switch (key)
                {
                    case "Up": UiIndex = (UiIndex + list.Count - 1) % list.Count; Play(Sfx.Menu); break;
                    case "Down": UiIndex = (UiIndex + 1) % list.Count; Play(Sfx.Menu); break;
                    case "Esc" or "Back": Ui = BattleUi.Command; break;
                    case "Enter" or "Space":
                    {
                        var s = list[Math.Clamp(UiIndex, 0, list.Count - 1)];
                        if (!b.CanCast(c, s)) { b.Say($"{c.Name} hasn't the mana for {s.Name}."); Ui = BattleUi.Command; break; }
                        Aiming = s;
                        if (s.Target == SpellTarget.Line) { Ui = BattleUi.Aim; b.Say($"{s.Name}: which way?"); }
                        else if (s.Target == SpellTarget.Ally) { Ui = BattleUi.Ally; UiIndex = 0; }
                        else { b.Cast(s.Id); Ui = BattleUi.Command; }
                        break;
                    }
                }
                break;
            }
            case BattleUi.Ally:
            {
                var allies = b.Party.Where(p => !p.Escaped).ToList();
                switch (key)
                {
                    case "Up": UiIndex = (UiIndex + allies.Count - 1) % allies.Count; Play(Sfx.Menu); break;
                    case "Down": UiIndex = (UiIndex + 1) % allies.Count; Play(Sfx.Menu); break;
                    case "Esc" or "Back": Ui = BattleUi.Command; break;
                    case "Enter" or "Space":
                        b.Cast(Aiming!.Id, ally: allies[Math.Clamp(UiIndex, 0, allies.Count - 1)]);
                        Ui = BattleUi.Command;
                        break;
                }
                break;
            }
        }
        SyncUi();
    }

    private void FinishBattle()
    {
        var b = Battle!;
        Battle = null;
        Mode = Mode.Play;
        Play(Sfx.MusicStop);
        Play(World.Where switch { Where.Dungeon => Sfx.DungeonMusic, Where.Town => Sfx.TownMusic, _ => World.Aboard ? Sfx.SeaMusic : Sfx.WorldMusic });
        if (InDemo && b.Outcome == Outcome.Lost)
        {
            EndDemo();
            return;
        }
        switch (b.Outcome)
        {
            case Outcome.Won:
                if (b.Gold > 0)
                {
                    World.Gold += b.Gold;
                    Note($"Victory! You find {b.Gold} gold.");
                }
                else Note("Victory!");
                if (b.EvilSlain > 0 && !b.Ran) ChangeVirtue(Virtue.Daring, 1, quiet: true);
                if (b.Spared > 0)
                {
                    ChangeVirtue(Virtue.Mercy, 1, quiet: true);
                    ChangeVirtue(Virtue.Fairness, 1, quiet: true);
                }
                if (b.StruckFleeing > 0) ChangeVirtue(Virtue.Mercy, -2 * Math.Min(2, b.StruckFleeing));
                var won = _afterWin;
                _afterWin = null;
                won?.Invoke();
                break;
            case Outcome.Fled:
                Note("You got away.");
                if (b.Foes.Any(f => f.Evil && !f.Gone)) ChangeVirtue(Virtue.Daring, -2);
                _afterWin = null;
                break;
            case Outcome.Lost:
                _afterWin = null;
                PartyLost();
                break;
        }
    }
}
