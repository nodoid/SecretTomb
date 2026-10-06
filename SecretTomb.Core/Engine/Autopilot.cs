using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SecretTomb.Core.World;

namespace SecretTomb.Core.Engine;

/// <summary>
/// Plays the tomb from start to finish: a walkthrough of places to visit and things to use, with
/// paths found by search (opening doors, jumping pits, swimming, riding the cart and taking the
/// teleporter on the way) and the laser fired at any ghoul or piranha in line. Used by the
/// attract-mode demo, the store captures and the tests.
/// </summary>
public sealed class Autopilot
{
    public enum Kind
    {
        Visit,
        Use,
    }

    public sealed record Goal(Kind Kind, int X, int Y)
    {
        public Point Cell => new(X, Y);
    }

    /// <summary>The way through the tomb.</summary>
    public static readonly Goal[] Walkthrough =
    [
        new(Kind.Visit, 27, 12),   // the laser pistol, held by a skeleton
        new(Kind.Use, 34, 10),     // the great inscription: learn the script
        new(Kind.Visit, 38, 16),   // a gem
        new(Kind.Visit, 48, 36),   // the jade key, at the end of the boulder run
        new(Kind.Visit, 56, 37),   // ammunition
        new(Kind.Use, 58, 8),      // a chest in the hall of pillars
        new(Kind.Visit, 57, 20),   // ammunition
        new(Kind.Use, 45, 23),     // "do not drink from the fountain"
        new(Kind.Visit, 57, 41),   // the first twin idol, through the teleporter
        new(Kind.Visit, 24, 24),   // the diving helmet, on its island
        new(Kind.Visit, 27, 36),   // the second idol, in the sunken chamber
        new(Kind.Use, 9, 14),      // "secret passages are not like the walls"
        new(Kind.Use, 9, 12),      // ...and here is one
        new(Kind.Use, 7, 9),       // the hidden treasure
        new(Kind.Use, 11, 9),
        new(Kind.Visit, 9, 8),
        new(Kind.Visit, 11, 11),
        new(Kind.Visit, 13, 26),   // a gem by the chasm
        new(Kind.Visit, 3, 40),    // the sacred book, across the chasm by mine cart
        new(Kind.Visit, 13, 44),   // ammunition
        new(Kind.Use, 19, 42),     // "the sacred book opens the door"
        new(Kind.Use, 17, 44),     // "the sign of the skull means death"
        new(Kind.Use, 31, 41),     // "the twins are a key"
        new(Kind.Use, 40, 42),     // the miraculous stone
        new(Kind.Use, 39, 37),     // the lever: a way out
        new(Kind.Visit, 29, 6),    // out of the pyramid
    ];

    private readonly Adventure _a;
    private int _index;
    private List<Point> _path;
    private Point _expected;
    private int _pathAge;
    private int _wait;
    private bool _toggle;
    private Vector2 _lastPos;
    private int _still;

    public Autopilot(Adventure adventure)
    {
        _a = adventure;
    }

    public int GoalIndex => _index;
    public Goal Current => _index < Walkthrough.Length ? Walkthrough[_index] : null;

    public Controls Next()
    {
        var none = new Controls();
        if (_a.Outcome != Outcome.Playing || !_a.Alive && _a.Reading == null)
            return none;

        // Press buttons as single taps: the adventure reacts to a press, not to holding.
        _toggle = !_toggle;

        if (_a.Reading != null)
            return _a.ReadingTicks > 80 && _toggle ? new Controls { Action = true } : none;
        if (_a.Riding != null)
            return none;
        if (_wait > 0)
        {
            _wait--;
            return none;
        }

        var shot = Shoot();
        if (shot != null)
            return new Controls { Aim = shot };

        var goal = Current;
        if (goal == null)
            return none;

        var cell = _a.Cell;
        var centre = Tomb.Centre(cell);
        bool centred = Vector2.Distance(_a.Pos, centre) < 3f;

        if (goal.Kind == Kind.Visit && cell == goal.Cell && centred)
        {
            Advance();
            return none;
        }
        if (goal.Kind == Kind.Use && Pathing.Dist(cell, goal.Cell) == 1 && centred && CanStand(cell))
        {
            var dir = DirTo(cell, goal.Cell);
            if (_a.Facing != dir)
                return Turn(dir);
            if (!_toggle)
                return none;
            Advance();
            _wait = 12;
            return new Controls { Action = true };
        }
        if (goal.Kind == Kind.Use && _a.ChestInReach() is { } chest && chest.Cell == goal.Cell)
        {
            if (!_toggle)
                return none;
            Advance();
            _wait = 12;
            return new Controls { Action = true };
        }

        // Follow the path.
        _pathAge++;
        if (_path == null || _path.Count == 0 || _pathAge > 40 || (cell != _expected && cell != _path[0]))
        {
            _path = FindPath(cell, goal);
            _expected = cell;
            _pathAge = 0;
            if (_path == null || _path.Count == 0)
                return Steer(centre);
        }

        // Unstick: if the explorer hasn't moved for a while, head for the middle of his square.
        if (Vector2.Distance(_a.Pos, _lastPos) < 0.05f)
            _still++;
        else
            _still = 0;
        _lastPos = _a.Pos;
        if (_still > 30 && !centred)
        {
            if (_still > 60)
                _still = 0;
            return Steer(centre);
        }

        var next = _path[0];
        var tile = _a.Tomb[next];
        bool far = Pathing.Dist(cell, next) > 1;

        // Ride the cart, or jump a pit: line up in the middle of the square first.
        if (far && IsCartEdge(cell, next))
        {
            if (!centred)
                return Steer(centre);
            return _toggle ? new Controls { Jump = true } : none;
        }
        if (far && IsTeleportEdge(cell, next))
        {
            // Standing on a teleporter that has just been used: step off, then back on.
            foreach (var n in Pathing.Around(cell))
                if (CanStand(n))
                {
                    _path = null;
                    return Steer(Tomb.Centre(n));
                }
        }
        if (far)
        {
            var dir = DirTo(cell, next);
            if (!centred)
                return Steer(centre);
            if (_a.Facing != dir)
                return Turn(dir);
            return _toggle ? new Controls { Jump = true } : none;
        }

        // Doors and gates: face them and open them.
        if (tile is Tile.Door or Tile.Secret or Tile.JadeDoor or Tile.BookGate or Tile.TwinGate)
        {
            var dir = DirTo(cell, next);
            if (!centred)
                return Steer(centre);
            if (_a.Facing != dir)
                return Turn(dir);
            return _toggle ? new Controls { Action = true } : none;
        }

        if (cell == next && Vector2.Distance(_a.Pos, Tomb.Centre(next)) < 2.5f)
        {
            _expected = next;
            _path.RemoveAt(0);
            if (_path.Count == 0)
                return none;
            next = _path[0];
        }
        return Steer(Tomb.Centre(next));
    }

    private void Advance()
    {
        _index++;
        _path = null;
    }

    private Controls Steer(Vector2 target)
    {
        var d = target - _a.Pos;
        if (d.Length() < 0.3f)
            return new Controls();
        // Move along one axis at a time, so as not to catch on corners.
        if (MathF.Abs(d.X) > 1.2f && MathF.Abs(d.Y) > 1.2f)
        {
            if (MathF.Abs(d.X) < MathF.Abs(d.Y))
                d.Y = 0;
            else
                d.X = 0;
        }
        float len = d.Length();
        var move = len > 1.25f ? d / len : d / 1.25f;
        return new Controls { Move = move };
    }

    /// <summary>A tiny step that turns the explorer without moving him off his square.</summary>
    private static Controls Turn(Dir dir) => new() { Move = dir.Vector() * 0.21f };

    private static Dir DirTo(Point from, Point to)
    {
        int dx = Math.Sign(to.X - from.X), dy = Math.Sign(to.Y - from.Y);
        return dx > 0 ? Dir.Right : dx < 0 ? Dir.Left : dy > 0 ? Dir.Down : Dir.Up;
    }

    // ---------------------------------------------------------------- shooting

    private Dir? Shoot()
    {
        if (!_a.HasPistol || _a.Ammo <= 0 || _a.Swimming && !_a.HasHelmet)
            return null;
        foreach (var m in _a.Monsters)
        {
            if (!m.Alive || m.Kind == MonsterKind.Guardian)
                continue;
            var d = m.Pos - _a.Pos;
            Dir dir;
            if (MathF.Abs(d.Y) < 5 && MathF.Abs(d.X) < 5.5f * Tomb.TileSize)
                dir = d.X > 0 ? Dir.Right : Dir.Left;
            else if (MathF.Abs(d.X) < 5 && MathF.Abs(d.Y) < 5.5f * Tomb.TileSize)
                dir = d.Y > 0 ? Dir.Down : Dir.Up;
            else
                continue;
            if (ClearShot(m.Pos, dir))
                return dir;
        }
        return null;
    }

    private bool ClearShot(Vector2 target, Dir dir)
    {
        var p = _a.Pos;
        var step = dir.Vector() * 4f;
        for (int i = 0; i < 40; i++)
        {
            p += step;
            if (Vector2.Distance(p, target) < 8)
                return true;
            if (Tomb.BlocksShot(_a.Tomb[Tomb.CellOf(p)], _a.PortcullisOpen))
                return false;
            foreach (var item in _a.Items)
                if (!item.Taken && !item.Opened && (item.Kind is ItemKind.Chest or ItemKind.Ammo) &&
                    Vector2.Distance(item.Centre, p) < 9)
                    return false;
        }
        return false;
    }

    // ---------------------------------------------------------------- searching

    private bool CanStand(Point c)
    {
        var t = _a.Tomb[c];
        if (Tomb.BlocksWalker(t, _a.PortcullisOpen))
            return false;
        return t switch
        {
            Tile.Pit or Tile.Chasm or Tile.Rail or Tile.Cracked or Tile.Fountain => false,
            Tile.Water => _a.HasHelmet || SwimAllowed,
            _ => !StaticBoulderAt(c),
        };
    }

    /// <summary>The swim to the diving helmet is short enough to hold one's breath.</summary>
    private bool SwimAllowed => Current is { } g && g.X == 24 && g.Y == 24;

    private bool StaticBoulderAt(Point c)
    {
        foreach (var b in _a.Boulders)
            if (!b.Gone && !b.Rolling && Tomb.CellOf(b.Pos) == c)
                return true;
        return false;
    }

    private bool Openable(Point c) => _a.Tomb[c] switch
    {
        Tile.Door or Tile.Secret => true,
        Tile.JadeDoor => _a.HasKey,
        Tile.BookGate => _a.HasBook,
        Tile.TwinGate => _a.Idols >= 2,
        _ => false,
    };

    private IEnumerable<Point> Neighbours(Point c)
    {
        foreach (var n in Pathing.Around(c))
        {
            if (CanStand(n) || Openable(n))
            {
                // Secret doors are only opened when the walkthrough asks for it.
                if (_a.Tomb[n] == Tile.Secret && !(Current is { Kind: Kind.Use } g && g.Cell == n))
                    continue;
                yield return n;
                continue;
            }
            // A pit one square wide can be jumped.
            if (_a.Tomb[n] == Tile.Pit)
            {
                var land = new Point(n.X + (n.X - c.X), n.Y + (n.Y - c.Y));
                if (CanStand(land))
                    yield return land;
            }
        }
        if (_a.Tomb[c] == Tile.Teleporter)
        {
            foreach (var (a, b) in _a.Tomb.Teleporters)
            {
                if (a == c) yield return b;
                if (b == c) yield return a;
            }
        }
        foreach (var cart in _a.Carts)
        {
            if (cart.Moving)
                continue;
            int at = (int)MathF.Round(cart.Index);
            int other = at == 0 ? cart.Line.Count - 1 : 0;
            var end = cart.Line[at];
            var next = cart.Line[at == 0 ? Math.Min(1, cart.Line.Count - 1) : at - 1];
            var board = new Point(end.X - (next.X - end.X), end.Y - (next.Y - end.Y));
            if (board != c)
                continue;
            var far = cart.Line[other];
            var before = cart.Line[other == 0 ? Math.Min(1, cart.Line.Count - 1) : other - 1];
            yield return new Point(far.X + (far.X - before.X), far.Y + (far.Y - before.Y));
        }
    }

    private bool IsCartEdge(Point from, Point to)
    {
        foreach (var cart in _a.Carts)
            if (Vector2.Distance(cart.Pos, Tomb.Centre(from)) < 26 && Pathing.Dist(from, to) > 2)
                return true;
        return false;
    }

    private bool IsTeleportEdge(Point from, Point to)
    {
        foreach (var (a, b) in _a.Tomb.Teleporters)
            if ((a == from && b == to) || (b == from && a == to))
                return true;
        return false;
    }

    private List<Point> FindPath(Point from, Goal goal)
    {
        Func<Point, bool> isGoal = goal.Kind == Kind.Visit
            ? c => c == goal.Cell
            : c => Pathing.Dist(c, goal.Cell) == 1 && CanStand(c);
        return Pathing.Path(from, isGoal, Neighbours);
    }
}
