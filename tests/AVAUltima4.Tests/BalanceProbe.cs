using AVAUltima4.Combat;
using AVAUltima4.Rules;
using AVAUltima4.World;

namespace AVAUltima4.Tests;

/// <summary>
/// Measures how fights tend to go, for tuning: a party of a size and level against the creatures of
/// each tier. Runs only with ULTIMA4_BALANCE=1, and prints a table.
/// </summary>
public sealed class BalanceProbe(ITestOutputHelper output)
{
    [Fact]
    public void Measure()
    {
        if (Environment.GetEnvironmentVariable("ULTIMA4_BALANCE") != "1") return;
        foreach (var (size, level) in new[] { (1, 1), (2, 2), (3, 3), (4, 4), (5, 5), (6, 6), (8, 7) })
        {
            var line = $"party {size} at L{level}:";
            for (int tier = 1; tier <= 5; tier++)
            {
                int wins = 0, n = 200, losses = 0;
                double hpLeft = 0;
                for (int i = 0; i < n; i++)
                {
                    var rng = new Rng((ulong)(i * 7919 + tier * 31 + size));
                    var party = Party(size, level);
                    var kinds = Monsters.Meet(tier, false, deep: tier >= 4);
                    var kind = rng.Pick(kinds);
                    int count = rng.Range(1, Math.Min(8, size + 1));
                    if (kind.Tier >= 4) count = Math.Max(1, (count + 1) / 2);
                    var b = new Battle(party, Enumerable.Repeat(kind, count).ToList(), Ground.Grass, new WorldState(), rng);
                    var o = b.RunToEnd();
                    if (o == Outcome.Won) wins++;
                    if (o == Outcome.Lost) losses++;
                    hpLeft += party.Sum(c => Math.Max(0, c.Hp)) / (double)party.Sum(c => c.MaxHp);
                }
                line += $"  T{tier} win {wins * 100 / n}% lose {losses * 100 / n}% hp {hpLeft / n:P0}";
            }
            output.WriteLine(line);
        }
    }

    private static List<Character> Party(int size, int level)
    {
        (ClassKind K, string W, string A)[] kit =
        [
            (ClassKind.Fighter, "sword", "chain"), (ClassKind.Paladin, "mace", "chain"), (ClassKind.Ranger, "bow", "leather"),
            (ClassKind.Bard, "sling", "leather"), (ClassKind.Tinker, "axe", "chain"), (ClassKind.Druid, "mace", "leather"),
            (ClassKind.Mage, "staff", "cloth"), (ClassKind.Shepherd, "sling", "cloth"),
        ];
        var party = new List<Character>();
        for (int i = 0; i < size; i++)
        {
            var (k, w, a) = kit[i];
            party.Add(level == 1 ? Character.Make("P" + i, k) : Character.Companion("P" + i, k, level, w, a));
        }
        return party;
    }
}
