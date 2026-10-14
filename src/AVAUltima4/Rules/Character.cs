namespace AVAUltima4.Rules;

public enum Status { Good, Poisoned, Dead }

/// <summary>
/// One of the party: attributes, level, hit points and mana, and what they carry. Levels come only
/// from the Queen, who raises anyone whose experience has earned it.
/// </summary>
public sealed class Character
{
    public const int MaxLevel = 8;
    public const int MaxStat = 50;

    public string Name { get; set; } = "";
    public ClassKind Class { get; set; }
    public int Str { get; set; }
    public int Dex { get; set; }
    public int Int { get; set; }
    public int Level { get; set; } = 1;
    public int Xp { get; set; }
    public int Hp { get; set; }
    public int Mp { get; set; }
    public Status Status { get; set; }
    public string Weapon { get; set; } = "hands";
    public string Armour { get; set; } = "none";

    public int MaxHp => 15 + Level * 15 + Str / 2;
    public int MaxMp => Math.Min(99, Int * ClassInfo.Get(Class).ManaTenths / 10);
    public bool Alive => Status != Status.Dead;
    public Weapon WeaponItem => Arms.Weapon(Weapon);
    public Armour ArmourItem => Arms.Armour(Armour);
    public int Defence => ArmourItem.Defence;
    public string Title => ClassInfo.NameOf(Class);

    /// <summary>Experience needed for each level: 100, 200, 400 ... 6400 for the eighth.</summary>
    public static int XpFor(int level) => level <= 1 ? 0 : 100 << (level - 2);

    public bool CanRise => Level < MaxLevel && Xp >= XpFor(Level + 1);

    public static Character Make(string name, ClassKind k)
    {
        var info = ClassInfo.Get(k);
        var c = new Character { Name = name, Class = k, Str = info.Str, Dex = info.Dex, Int = info.Int };
        c.Weapon = info.Weapons[0];
        c.Armour = info.Armour[0];
        c.Hp = c.MaxHp;
        c.Mp = c.MaxMp;
        return c;
    }

    /// <summary>A companion met on the road, already a few levels along.</summary>
    public static Character Companion(string name, ClassKind k, int level, string weapon, string armour)
    {
        var c = Make(name, k);
        while (c.Level < level) c.Rise(null);
        c.Xp = XpFor(level);
        c.Weapon = weapon;
        c.Armour = armour;
        c.Hp = c.MaxHp;
        c.Mp = c.MaxMp;
        return c;
    }

    /// <summary>Goes up a level: a little stronger, quicker and wiser, and fully healed.</summary>
    public void Rise(Rng? rng)
    {
        Level++;
        Str = Math.Min(MaxStat, Str + (rng?.Range(0, 2) ?? 1));
        Dex = Math.Min(MaxStat, Dex + (rng?.Range(0, 2) ?? 1));
        Int = Math.Min(MaxStat, Int + (rng?.Range(0, 2) ?? 1));
        Hp = MaxHp;
        Mp = MaxMp;
    }

    public void Hurt(int amount)
    {
        if (!Alive) return;
        Hp -= amount;
        if (Hp <= 0)
        {
            Hp = 0;
            Status = Status.Dead;
        }
    }

    public void Heal(int amount)
    {
        if (!Alive) return;
        Hp = Math.Min(MaxHp, Hp + amount);
    }

    public void Revive()
    {
        if (Alive) return;
        Status = Status.Good;
        Hp = Math.Max(1, MaxHp / 4);
    }

    /// <summary>Damage with a weapon: its dice and a little for strength.</summary>
    public int RollDamage(Rng rng) => WeaponItem.Damage.Roll(rng) + Str / 6;

    public Character Clone() => (Character)MemberwiseClone();
}
