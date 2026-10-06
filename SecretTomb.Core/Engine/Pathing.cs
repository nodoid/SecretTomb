using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SecretTomb.Core.Engine;

/// <summary>Breadth-first searches over the tomb's squares.</summary>
public static class Pathing
{
    private static readonly Point[] Steps = [new(0, -1), new(1, 0), new(0, 1), new(-1, 0)];

    /// <summary>
    /// The first step from <paramref name="from"/> towards <paramref name="goal"/>, through squares
    /// that pass <paramref name="passable"/>. If the goal can't be reached, heads for the reachable
    /// square nearest to it. Null when there is nowhere better to go.
    /// </summary>
    public static Point? StepTowards(Point from, Point goal, Func<Point, bool> passable, int maxNodes = 600)
    {
        if (from == goal)
            return null;
        var parent = new Dictionary<Point, Point> { [from] = from };
        var queue = new Queue<Point>();
        queue.Enqueue(from);
        Point best = from;
        int bestDist = Dist(from, goal);
        while (queue.Count > 0 && parent.Count < maxNodes)
        {
            var c = queue.Dequeue();
            if (c == goal)
            {
                best = c;
                break;
            }
            foreach (var s in Steps)
            {
                var n = new Point(c.X + s.X, c.Y + s.Y);
                if (parent.ContainsKey(n) || !passable(n))
                    continue;
                parent[n] = c;
                queue.Enqueue(n);
                int d = Dist(n, goal);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = n;
                }
            }
        }
        if (best == from)
            return null;
        var step = best;
        while (parent[step] != from)
            step = parent[step];
        return step;
    }

    /// <summary>A full path (excluding the start) using custom edges, or null.</summary>
    public static List<Point> Path(Point from, Func<Point, bool> isGoal, Func<Point, IEnumerable<Point>> neighbours,
        int maxNodes = 20000)
    {
        var parent = new Dictionary<Point, Point> { [from] = from };
        var queue = new Queue<Point>();
        queue.Enqueue(from);
        while (queue.Count > 0 && parent.Count < maxNodes)
        {
            var c = queue.Dequeue();
            if (isGoal(c))
            {
                var path = new List<Point>();
                for (var p = c; p != from; p = parent[p])
                    path.Add(p);
                path.Reverse();
                return path;
            }
            foreach (var n in neighbours(c))
            {
                if (parent.ContainsKey(n))
                    continue;
                parent[n] = c;
                queue.Enqueue(n);
            }
        }
        return null;
    }

    public static IEnumerable<Point> Around(Point c)
    {
        foreach (var s in Steps)
            yield return new Point(c.X + s.X, c.Y + s.Y);
    }

    public static int Dist(Point a, Point b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}
