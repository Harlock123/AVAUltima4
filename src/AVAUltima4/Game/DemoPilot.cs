using AVAUltima4.Combat;
using AVAUltima4.World;

namespace AVAUltima4.Game;

/// <summary>
/// Plays the attract demo: a ready-made company walks the land from place to place, greets the
/// pilgrims it meets, fights whatever comes at it (the computer fighting), and steps through a
/// moongate when one is open on its way.
/// </summary>
public sealed class DemoPilot(GameEngine game)
{
    private double _next = 1.0;
    private (int X, int Y)? _target;
    private readonly HashSet<(int, int)> _tried = [];
    public double Elapsed { get; private set; }

    public void Update(double dt)
    {
        Elapsed += dt;
        if (game.Mode == Mode.Battle)
        {
            if (game.Battle is { } b) b.PartyQuick = true;
            return;
        }
        _next -= dt;
        if (_next > 0) return;
        _next = game.Top is not null ? 1.6 : 0.2;
        if (game.Top is { } p)
        {
            // Read the panel for a moment, then take the first choice ("Greet them", "Continue").
            int pick = p.Options.FindIndex(o => o.Enabled && o.Act is not null);
            if (pick >= 0) { p.Selected = pick; game.Input("Enter"); }
            else game.Input("Esc");
            return;
        }
        if (game.Mode != Mode.Play || game.World.Where != Where.World) return;
        game.Input(NextKey());
    }

    private (int X, int Y) PickTarget()
    {
        var land = GameEngine.Land;
        var w = game.World;
        var choices = land.Sites.Where(s => s.Id != "undermere").Select(s => (s.X, s.Y)).Concat(land.Gates)
            .Where(p => !_tried.Contains(p) && Overworld.Dist(p.Item1, p.Item2, w.X, w.Y) is > 6 and < 30).ToList();
        if (choices.Count == 0)
        {
            _tried.Clear();
            return (land.Site("brightholm").X, land.Site("brightholm").Y + 1);
        }
        return choices[game.Rng.Next(choices.Count)];
    }

    /// <summary>The key that takes the company one step along the shortest way to where it's going.</summary>
    private string NextKey()
    {
        var w = game.World;
        _target ??= PickTarget();
        var goal = _target.Value;
        if (Overworld.Dist(w.X, w.Y, goal.X, goal.Y) <= 1)
        {
            _tried.Add(goal);
            _target = PickTarget();
            goal = _target.Value;
        }
        var land = GameEngine.Land;
        var start = (w.X, w.Y);
        var prev = new Dictionary<(int, int), (int, int)> { [start] = start };
        var q = new Queue<(int X, int Y)>();
        q.Enqueue(start);
        bool found = false;
        while (q.Count > 0 && !found)
        {
            var (x, y) = q.Dequeue();
            foreach (var n in Overworld.Neighbours(x, y))
            {
                if (prev.ContainsKey(n) || !Terrain.Walkable(land[n.X, n.Y])) continue;
                if (Terrain.IsSite(land[n.X, n.Y]) && n != goal) continue;
                prev[n] = (x, y);
                if (n == goal) { found = true; break; }
                q.Enqueue(n);
            }
        }
        if (!found)
        {
            _tried.Add(goal);
            _target = null;
            return "Space";
        }
        var step = goal;
        while (prev[step] != start) step = prev[step];
        return step.Item1 > w.X ? "Right" : step.Item1 < w.X ? "Left" : step.Item2 > w.Y ? "Down" : "Up";
    }
}
