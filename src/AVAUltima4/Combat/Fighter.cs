using AVAUltima4.Rules;

namespace AVAUltima4.Combat;

public enum Side { Party, Foe }

/// <summary>
/// Someone on the battlefield: one of the party (whose wounds stay on the character after the
/// fight) or a creature, with the passing state of this one fight.
/// </summary>
public sealed class Fighter
{
    public required string Name { get; set; }
    public Side Side { get; init; }
    public Character? Pc { get; init; }
    public Monster? Kind { get; init; }
    public int X { get; set; }
    public int Y { get; set; }
    public int MaxHp { get; init; }
    private int _hp;

    public int Hp
    {
        get => Pc?.Hp ?? _hp;
        set { if (Pc is null) _hp = value; else Pc.Hp = value; }
    }

    public int Asleep { get; set; }
    public bool Fleeing { get; set; }
    public bool Escaped { get; set; }
    /// <summary>Struck by the party while it was running away.</summary>
    public bool HitWhileFleeing { get; set; }
    public bool IsPc => Pc is not null;
    public bool Dead => Pc is not null ? !Pc.Alive : _hp <= 0;
    /// <summary>Out of the fight: dead or gone.</summary>
    public bool Gone => Dead || Escaped;
    public string Sprite => Pc is null ? Kind!.Sprite : ClassInfo.NameOf(Pc.Class).ToLowerInvariant();
    public bool Evil => Kind?.Evil == true;
    public bool Undead => Kind?.Is(Traits.Undead) == true;

    public int Distance(int x, int y) => Math.Max(Math.Abs(X - x), Math.Abs(Y - y));
    public int Distance(Fighter o) => Distance(o.X, o.Y);
    /// <summary>Next to, along one of the four directions.</summary>
    public bool Beside(Fighter o) => Math.Abs(X - o.X) + Math.Abs(Y - o.Y) == 1;

    public override string ToString() => Name;
}

/// <summary>A number or word floating up from a square for a moment.</summary>
public sealed class Floater(int x, int y, string text, uint colour)
{
    public int X { get; } = x;
    public int Y { get; } = y;
    public string Text { get; } = text;
    public uint Colour { get; } = colour;
    public double Age { get; set; }
}

/// <summary>A missile or a bolt of magic crossing the field, drawn for a moment.</summary>
public sealed class Bolt(int x0, int y0, int x1, int y1, uint colour)
{
    public int X0 { get; } = x0;
    public int Y0 { get; } = y0;
    public int X1 { get; } = x1;
    public int Y1 { get; } = y1;
    public uint Colour { get; } = colour;
    public double Age { get; set; }
}
