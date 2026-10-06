using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SecretTomb.Core.Audio;
using SecretTomb.Core.Localization;
using SecretTomb.Core.World;

namespace SecretTomb.Core.Engine;

public enum Outcome
{
    Playing,
    Won,
    Dead,
}

/// <summary>How hard the tomb is. Hard is the original: a single life.</summary>
public enum Difficulty
{
    Easy,
    Normal,
    Hard,
}

public enum DeathCause
{
    None,
    Ghoul,
    Guardian,
    Fish,
    Dart,
    Pit,
    Chasm,
    Drowned,
    Boulder,
    Collapse,
    Youth,
}

/// <summary>What the player asks for this tick (from the keyboard, touch, a pad or the autopilot).</summary>
public struct Controls
{
    /// <summary>Walking direction (-1..1 on each axis; y down).</summary>
    public Vector2 Move;

    /// <summary>Fire the laser the way the explorer faces.</summary>
    public bool Fire;

    /// <summary>Turn this way and fire (the original's four firing keys).</summary>
    public Dir? Aim;

    /// <summary>Jump, or board the mine cart.</summary>
    public bool Jump;

    /// <summary>Open, close, read, pull, take... whatever is in front of the explorer.</summary>
    public bool Action;
}

public sealed class Item
{
    public ItemKind Kind;
    public Point Cell;
    public bool Taken;
    public bool Opened;
    public bool Destroyed;
    public Vector2 Centre => Tomb.Centre(Cell);
}

public sealed class Monster
{
    public MonsterKind Kind;
    public Point Home;
    public Point Cell;
    public Vector2 Pos;
    public bool Moving;
    public bool Alive = true;
    public Dir Facing = Dir.Down;
    public int Anim;
    public int Stun;
}

public sealed class Bolt
{
    public Vector2 Pos;
    public Vector2 Start;
    public Dir Dir;
}

public sealed class Dart
{
    public Vector2 Pos;
    public Dir Dir;
}

public sealed class Boulder
{
    public Point Home;
    public Vector2 Pos;
    public Dir Dir;
    public bool Rolling;
    public bool Gone;
    public float Angle;
}

public sealed class Cart
{
    public List<Point> Line;
    public float Index;
    public int Target = -1;
    public bool Moving => Target >= 0;

    public Vector2 Pos
    {
        get
        {
            int i = Math.Clamp((int)MathF.Floor(Index), 0, Line.Count - 1);
            int j = Math.Min(i + 1, Line.Count - 1);
            float t = Index - i;
            return Vector2.Lerp(Tomb.Centre(Line[i]), Tomb.Centre(Line[j]), t);
        }
    }

    public bool Vertical => Line.Count > 1 && Line[0].X == Line[1].X;
}

public sealed class Particle
{
    public Vector2 Pos;
    public Vector2 Vel;
    public int Life;
    public int Max;
    public Color Color;
    public float Size;
    public bool Glow;
}

/// <summary>
/// One game of The Secret of the Tomb: the explorer, the tomb and everything in it, and the rules,
/// updated 50 times a second. Drawing is done by <see cref="TombRenderer"/>.
/// </summary>
public sealed class Adventure
{
    public const int TicksPerSecond = 50;
    public const float HalfSize = 6f;          // the explorer's feet: a 12 x 12 box
    public const float WalkSpeed = 1.25f;
    public const float SwimSpeed = 0.85f;
    public const int JumpLength = 30;
    public const float JumpSpeed = 1.65f;
    public const float MaxAir = 100f;
    public const int MaxLives = 5;
    public const int DeathPause = 120;
    public const int Leash = 7;                // squares a monster strays from home

    private readonly ISoundPlayer _sound;
    private readonly Random _random;
    private readonly List<(Point cell, Dir dir, int phase)> _launchers = new();
    private readonly Dictionary<Point, Boulder> _plates = new();
    private readonly HashSet<Point> _reserved = new();
    private int _fireCooldown;
    private int _noWeaponTicks;
    private Point? _teleportLock;
    private bool _prevJump;
    private bool _prevAction;
    private bool _prevFire;

    public Adventure(ISoundPlayer sound, int seed = 1985, Difficulty difficulty = Difficulty.Normal)
    {
        Difficulty = difficulty;
        _sound = sound ?? SilentSoundPlayer.Instance;
        _random = new Random(seed);
        Tomb = Tomb.Load();
        Setup();
    }

    public Adventure(ISoundPlayer sound, Tomb tomb, int seed = 1985, Difficulty difficulty = Difficulty.Normal)
    {
        Difficulty = difficulty;
        _sound = sound ?? SilentSoundPlayer.Instance;
        _random = new Random(seed);
        Tomb = tomb;
        Setup();
    }

    private void Setup()
    {
        Lives = StartLives;
        Pos = Tomb.Centre(Tomb.Start);
        Checkpoint = Tomb.Start;
        Camera = Pos;
        foreach (var s in Tomb.Items)
            Items.Add(new Item { Kind = s.Kind, Cell = s.Cell });
        foreach (var s in Tomb.Monsters)
            Monsters.Add(new Monster { Kind = s.Kind, Home = s.Cell, Cell = s.Cell, Pos = Tomb.Centre(s.Cell) });
        foreach (var b in Tomb.Boulders)
            Boulders.Add(new Boulder { Home = b, Pos = Tomb.Centre(b) });
        foreach (var c in Tomb.Carts)
            Carts.Add(new Cart { Line = Tomb.RailLine(c), Index = Tomb.RailLine(c).IndexOf(c) });
        for (int y = 0; y < Tomb.Height; y++)
            for (int x = 0; x < Tomb.Width; x++)
            {
                if (Tomb[x, y] == Tile.Launcher)
                    _launchers.Add((new Point(x, y), (Dir)Tomb.Param(x, y), (x * 23 + y * 11) % DartPeriod));
                if (Tomb[x, y] == Tile.Plate)
                {
                    // The plate sets off the boulder lying in line with it.
                    Boulder best = null;
                    int bestDist = int.MaxValue;
                    foreach (var b in Boulders)
                    {
                        if (b.Home.X != x && b.Home.Y != y)
                            continue;
                        int d = Pathing.Dist(b.Home, new Point(x, y));
                        if (d < bestDist)
                        {
                            bestDist = d;
                            best = b;
                        }
                    }
                    if (best != null)
                        _plates[new Point(x, y)] = best;
                }
            }
        TotalTreasure = 0;
        foreach (var i in Items)
            if (i.Kind is ItemKind.Gem or ItemKind.Chest)
                TotalTreasure++;
        Say(() => Strings.Intro, 200);
    }

    // ---------------------------------------------------------------- state

    public Tomb Tomb { get; }
    public List<Item> Items { get; } = new();
    public List<Monster> Monsters { get; } = new();
    public List<Bolt> Bolts { get; } = new();
    public List<Dart> Darts { get; } = new();
    public List<Boulder> Boulders { get; } = new();
    public List<Cart> Carts { get; } = new();
    public List<Particle> Particles { get; } = new();

    public Vector2 Pos { get; private set; }
    public Dir Facing { get; private set; } = Dir.Down;
    public bool Walking { get; private set; }
    public int WalkTicks { get; private set; }
    public int JumpTicks { get; private set; }
    public float JumpHeight => JumpTicks > 0 ? MathF.Sin(MathF.PI * JumpTicks / JumpLength) * 9f : 0f;
    public bool Swimming { get; private set; }
    public float Air { get; private set; } = MaxAir;
    public Cart Riding { get; private set; }
    public int Invulnerable { get; private set; }

    /// <summary>The demo and store captures: nothing hurts the explorer.</summary>
    public bool GodMode { get; set; }

    public DeathCause Dying { get; private set; }
    public int DeathTicks { get; private set; }
    public Point Checkpoint { get; private set; }
    public Outcome Outcome { get; private set; }

    public int Lives { get; private set; }

    // ---------------------------------------------------------------- difficulty

    public Difficulty Difficulty { get; }

    /// <summary>Lives at the start: five, three, or one as in the original.</summary>
    public int StartLives => Difficulty switch { Difficulty.Easy => 5, Difficulty.Hard => 1, _ => 3 };

    /// <summary>Breath lost per tick under water (Normal: four seconds of breath).</summary>
    public float AirLoss => Difficulty switch { Difficulty.Easy => 0.4f, Difficulty.Hard => 0.6f, _ => 0.5f };

    public int PistolShots => Difficulty switch { Difficulty.Easy => 16, Difficulty.Hard => 10, _ => 12 };
    public int AmmoShots => Difficulty switch { Difficulty.Easy => 10, Difficulty.Hard => 6, _ => 8 };

    /// <summary>Monsters move this much faster (or slower) than on Normal.</summary>
    public float MonsterSpeed => Difficulty switch { Difficulty.Easy => 0.8f, Difficulty.Hard => 1.12f, _ => 1f };

    /// <summary>Ticks between darts.</summary>
    public int DartPeriod => Difficulty switch { Difficulty.Easy => 80, Difficulty.Hard => 50, _ => 64 };

    /// <summary>The escape bonus is halved on Easy and doubled on Hard.</summary>
    public float BonusFactor => Difficulty switch { Difficulty.Easy => 0.5f, Difficulty.Hard => 2f, _ => 1f };
    public int Score { get; private set; }
    public int Bonus { get; private set; }
    public int Ammo { get; private set; }
    public bool HasPistol { get; private set; }
    public bool HasKey { get; private set; }
    public bool HasBook { get; private set; }
    public int Idols { get; private set; }
    public bool HasHelmet { get; private set; }
    public bool HasStone { get; private set; }
    public bool KnowsScript { get; private set; }
    public bool PortcullisOpen { get; private set; }

    public int Slain { get; private set; }
    public int TreasureFound { get; private set; }
    public int TotalTreasure { get; private set; }
    public int SecretsFound { get; private set; }

    public int Ticks { get; private set; }
    public int Seconds => Ticks / TicksPerSecond;

    /// <summary>The inscription being read (0 the great inscription, 1-9 a tablet), or null.</summary>
    public int? Reading { get; private set; }
    public int ReadingTicks { get; private set; }

    /// <summary>Ticks of earthquake left (the tomb shakes when the stone is taken).</summary>
    public int Shake { get; private set; }

    public int Flash { get; private set; }

    public Vector2 Camera { get; private set; }

    public Func<string> Message { get; private set; }
    public int MessageTicks { get; private set; }

    public Point Cell => Tomb.CellOf(Pos);
    public bool Alive => Dying == DeathCause.None && Outcome == Outcome.Playing;
    public bool Outside => Cell.Y < Tomb.OutsideRows;

    // ---------------------------------------------------------------- messages

    public void Say(Func<string> text, int ticks = 150)
    {
        Message = text;
        MessageTicks = ticks;
    }

    private void Play(Sfx sfx, float pitch = 0f) => _sound.Play(sfx, pitch);

    /// <summary>Plays a sound only if it happens near the explorer.</summary>
    private void PlayNear(Sfx sfx, Vector2 where, float squares = 7)
    {
        if (Vector2.Distance(where, Pos) < squares * Tomb.TileSize)
            Play(sfx);
    }

    // ---------------------------------------------------------------- the tick

    public void Tick(Controls c)
    {
        if (Outcome != Outcome.Playing)
            return;
        Ticks++;
        bool jumpHit = c.Jump && !_prevJump;
        bool actionHit = c.Action && !_prevAction;
        bool fireHit = c.Fire && !_prevFire;
        _prevJump = c.Jump;
        _prevAction = c.Action;
        _prevFire = c.Fire;

        if (MessageTicks > 0 && --MessageTicks == 0)
            Message = null;
        if (Shake > 0)
            Shake--;
        if (Flash > 0)
            Flash--;
        UpdateParticles();

        if (Reading != null)
        {
            ReadingTicks++;
            if (ReadingTicks > 15 && (actionHit || jumpHit || fireHit || c.Aim != null))
                Reading = null;
            return;
        }

        if (Dying != DeathCause.None)
        {
            DeathTicks++;
            UpdateMonsters();
            UpdateDarts();
            UpdateBoulders();
            if (DeathTicks >= DeathPause)
            {
                if (Lives > 0)
                    Respawn();
                else
                    Outcome = Outcome.Dead;
            }
            UpdateCamera();
            return;
        }

        if (Invulnerable > 0)
            Invulnerable--;
        if (_fireCooldown > 0)
            _fireCooldown--;
        if (_noWeaponTicks > 0)
            _noWeaponTicks--;

        if (Riding != null)
            UpdateRide();
        else
        {
            if (c.Aim is { } aim)
            {
                Facing = aim;
                Fire();
            }
            else if (c.Fire && (fireHit || _fireCooldown == 0))
                Fire();

            if (jumpHit && !TryBoard())
                StartJump();
            if (actionHit && !TryBoard())
                Act();
            if (Reading != null)
                return;
            MovePlayer(c.Move);
        }

        UpdateMonsters();
        UpdateBolts();
        UpdateDarts();
        UpdateBoulders();
        if (Alive && Riding == null)
            CheckGround();
        if (Alive)
            TakeItems();
        UpdateCamera();
    }

    // ---------------------------------------------------------------- moving

    private bool Blocked(Point cell) => Tomb.BlocksWalker(Tomb[cell], PortcullisOpen);

    /// <summary>True when the explorer's feet can stand at this point.</summary>
    public bool Free(Vector2 p)
    {
        int x0 = (int)MathF.Floor((p.X - HalfSize) / Tomb.TileSize);
        int x1 = (int)MathF.Floor((p.X + HalfSize - 0.01f) / Tomb.TileSize);
        int y0 = (int)MathF.Floor((p.Y - HalfSize) / Tomb.TileSize);
        int y1 = (int)MathF.Floor((p.Y + HalfSize - 0.01f) / Tomb.TileSize);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (Blocked(new Point(x, y)))
                    return false;
        // A boulder waiting to roll is in the way too.
        foreach (var b in Boulders)
            if (!b.Gone && !b.Rolling && MathF.Abs(b.Pos.X - p.X) < 11 + HalfSize && MathF.Abs(b.Pos.Y - p.Y) < 11 + HalfSize)
                return false;
        return true;
    }

    private void MovePlayer(Vector2 move)
    {
        if (JumpTicks > 0)
        {
            var v = Facing.Vector() * JumpSpeed;
            Step(v.X, 0);
            Step(0, v.Y);
            if (++JumpTicks > JumpLength)
                JumpTicks = 0;
            Walking = false;
            return;
        }

        if (move.LengthSquared() < 0.04f)
        {
            Walking = false;
            return;
        }
        if (move.LengthSquared() > 1)
            move.Normalize();

        // Face the way the stick or keys point most strongly (keeping the current way on a diagonal).
        Dir want;
        if (MathF.Abs(move.X) > MathF.Abs(move.Y) + 0.05f)
            want = move.X > 0 ? Dir.Right : Dir.Left;
        else if (MathF.Abs(move.Y) > MathF.Abs(move.X) + 0.05f)
            want = move.Y > 0 ? Dir.Down : Dir.Up;
        else
            want = Facing;
        Facing = want;

        float speed = Swimming ? SwimSpeed : WalkSpeed;
        var d = move * speed;
        bool movedX = Step(d.X, 0);
        bool movedY = Step(0, d.Y);
        // Slide round corners into corridors.
        if (!movedX && d.X != 0 && MathF.Abs(move.Y) < 0.3f)
            Nudge(true, MathF.Sign(d.X), speed);
        if (!movedY && d.Y != 0 && MathF.Abs(move.X) < 0.3f)
            Nudge(false, MathF.Sign(d.Y), speed);

        Walking = true;
        WalkTicks++;
        if (WalkTicks % 14 == 0 && !Swimming)
            Play(Sfx.Step);
        if (WalkTicks % 30 == 0 && Swimming)
            Play(Sfx.Splash);
    }

    private bool Step(float dx, float dy)
    {
        if (dx == 0 && dy == 0)
            return false;
        var next = Pos + new Vector2(dx, dy);
        if (!Free(next))
            return false;
        Pos = next;
        return true;
    }

    /// <summary>Moving along one axis into a wall: drift towards the middle of an open row or column.</summary>
    private void Nudge(bool alongX, int sign, float speed)
    {
        var cell = Cell;
        float centre = alongX ? Tomb.Centre(cell).Y : Tomb.Centre(cell).X;
        float current = alongX ? Pos.Y : Pos.X;
        float diff = centre - current;
        if (MathF.Abs(diff) < 0.2f)
            return;
        var ahead = alongX ? new Point(cell.X + sign, cell.Y) : new Point(cell.X, cell.Y + sign);
        if (Blocked(ahead))
            return;
        float stepBy = MathF.Sign(diff) * MathF.Min(speed, MathF.Abs(diff));
        if (alongX)
            Step(0, stepBy);
        else
            Step(stepBy, 0);
    }

    private void StartJump()
    {
        if (JumpTicks > 0 || Swimming)
            return;
        JumpTicks = 1;
        Play(Sfx.Jump);
    }

    // ---------------------------------------------------------------- the ground under the explorer

    private void CheckGround()
    {
        var cell = Cell;
        var tile = Tomb[cell];
        if (JumpTicks == 0)
        {
            switch (tile)
            {
                case Tile.Pit:
                    Die(DeathCause.Pit);
                    return;
                case Tile.Chasm:
                case Tile.Rail:
                    Die(DeathCause.Chasm);
                    return;
                case Tile.Cracked:
                    Tomb[cell] = Tile.Pit;
                    Dust(Tomb.Centre(cell), new Color(90, 100, 160), 18);
                    Die(DeathCause.Collapse);
                    return;
                case Tile.Plate when _plates.TryGetValue(cell, out var boulder) && !boulder.Rolling && !boulder.Gone:
                    boulder.Rolling = true;
                    var dx = cell.X - boulder.Home.X;
                    var dy = cell.Y - boulder.Home.Y;
                    boulder.Dir = dx > 0 ? Dir.Right : dx < 0 ? Dir.Left : dy > 0 ? Dir.Down : Dir.Up;
                    Play(Sfx.Boulder);
                    Say(() => Strings.BoulderComing, 120);
                    Shake = Math.Max(Shake, 40);
                    break;
            }
        }

        bool wasSwimming = Swimming;
        Swimming = tile == Tile.Water && JumpTicks == 0;
        if (Swimming)
        {
            if (!wasSwimming)
            {
                Play(Sfx.Splash);
                Splash(Pos);
                if (!HasHelmet)
                    Say(() => Strings.HoldBreath, 100);
            }
            if (!HasHelmet && !GodMode)
            {
                float before = Air;
                Air = MathF.Max(0, Air - AirLoss);
                if (before > 30 && Air <= 30)
                    Play(Sfx.Warning);
                if (Air <= 0)
                {
                    Die(DeathCause.Drowned);
                    return;
                }
            }
        }
        else
        {
            Air = MathF.Min(MaxAir, Air + 3f);
        }

        if (tile == Tile.SunStone && Checkpoint != cell)
        {
            Checkpoint = cell;
            Play(Sfx.Checkpoint);
            Say(() => Strings.CheckpointReached, 100);
        }

        if (tile == Tile.Teleporter)
        {
            if (_teleportLock != cell)
            {
                foreach (var (a, b) in Tomb.Teleporters)
                {
                    if (a != cell && b != cell)
                        continue;
                    var to = a == cell ? b : a;
                    Sparkle(Pos, new Color(80, 255, 230), 24);
                    Pos = Tomb.Centre(to);
                    Camera = Pos;
                    _teleportLock = to;
                    Flash = 20;
                    Play(Sfx.Teleport);
                    Sparkle(Pos, new Color(80, 255, 230), 24);
                    Say(() => Strings.Teleported, 100);
                    break;
                }
            }
        }
        else
        {
            _teleportLock = null;
        }

        if (tile == Tile.Gateway && HasStone)
            Win();
    }

    // ---------------------------------------------------------------- items

    private void TakeItems()
    {
        foreach (var item in Items)
        {
            if (item.Taken || item.Kind == ItemKind.Chest)
                continue;
            if (Vector2.Distance(item.Centre, Pos) > 12f)
                continue;
            item.Taken = true;
            switch (item.Kind)
            {
                case ItemKind.Pistol:
                    HasPistol = true;
                    Ammo += PistolShots;
                    Play(Sfx.Take);
                    Say(() => Strings.FoundPistol, 200);
                    break;
                case ItemKind.Ammo:
                    Ammo += AmmoShots;
                    Play(Sfx.Ammo);
                    Say(() => Strings.FoundAmmo, 100);
                    break;
                case ItemKind.Gem:
                    Score += 100;
                    TreasureFound++;
                    Play(Sfx.Treasure);
                    Say(() => Strings.FoundGem, 100);
                    break;
                case ItemKind.JadeKey:
                    HasKey = true;
                    Score += 500;
                    Play(Sfx.Take);
                    Say(() => Strings.FoundKey, 150);
                    break;
                case ItemKind.Book:
                    HasBook = true;
                    Score += 500;
                    Play(Sfx.Take);
                    Say(() => Strings.FoundBook, 150);
                    break;
                case ItemKind.Idol:
                    Idols++;
                    Score += 1000;
                    Play(Sfx.Treasure);
                    if (Idols >= 2)
                        Say(() => Strings.FoundBothIdols, 150);
                    else
                        Say(() => Strings.FoundIdol, 150);
                    break;
                case ItemKind.Helmet:
                    HasHelmet = true;
                    Score += 250;
                    Play(Sfx.Take);
                    Say(() => Strings.FoundHelmet, 180);
                    break;
            }
            Sparkle(item.Centre, new Color(255, 230, 140), 12);
        }
    }

    // ---------------------------------------------------------------- the laser

    private void Fire()
    {
        if (!HasPistol)
        {
            if (_noWeaponTicks == 0)
            {
                Say(() => Strings.NoWeapon, 100);
                _noWeaponTicks = 100;
            }
            return;
        }
        if (_fireCooldown > 0)
            return;
        _fireCooldown = 12;
        if (Ammo <= 0)
        {
            Play(Sfx.Empty);
            Say(() => Strings.NoAmmo, 100);
            return;
        }
        Ammo--;
        var start = Pos + Facing.Vector() * 8f;
        Bolts.Add(new Bolt { Pos = start, Start = start, Dir = Facing });
        Play(Sfx.Laser);
        if (Ammo == 3)
            Say(() => Strings.LowAmmo, 90);
    }

    private void UpdateBolts()
    {
        for (int i = Bolts.Count - 1; i >= 0; i--)
        {
            var b = Bolts[i];
            bool dead = false;
            for (int s = 0; s < 3 && !dead; s++)
            {
                b.Pos += b.Dir.Vector() * 2f;
                dead = BoltHits(b);
            }
            if (dead || Vector2.Distance(b.Pos, b.Start) > 260)
                Bolts.RemoveAt(i);
        }
    }

    private bool BoltHits(Bolt b)
    {
        var cell = Tomb.CellOf(b.Pos);
        if (Tomb.BlocksShot(Tomb[cell], PortcullisOpen))
        {
            Sparkle(b.Pos - b.Dir.Vector() * 2, new Color(140, 255, 240), 6);
            return true;
        }
        foreach (var m in Monsters)
        {
            if (!m.Alive || Vector2.Distance(m.Pos, b.Pos) > (m.Kind == MonsterKind.Guardian ? 11f : 9f))
                continue;
            if (m.Kind == MonsterKind.Guardian)
            {
                Play(Sfx.Ricochet);
                Sparkle(b.Pos, new Color(160, 200, 255), 10);
                Say(() => Strings.GuardianImmune, 120);
                return true;
            }
            Kill(m, 0);
            return true;
        }
        foreach (var boulder in Boulders)
        {
            if (boulder.Gone || Vector2.Distance(boulder.Pos, b.Pos) > 11f)
                continue;
            Smash(boulder);
            return true;
        }
        foreach (var item in Items)
        {
            if (item.Taken || item.Opened || item.Kind is not (ItemKind.Chest or ItemKind.Ammo))
                continue;
            if (Vector2.Distance(item.Centre, b.Pos) > 8f)
                continue;
            item.Taken = true;
            item.Destroyed = true;
            Dust(item.Centre, new Color(170, 110, 60), 16);
            Play(Sfx.Smash);
            if (item.Kind == ItemKind.Chest)
                Say(() => Strings.ChestDestroyed, 150);
            else
                Say(() => Strings.AmmoDestroyed, 150);
            return true;
        }
        return false;
    }

    /// <summary>A monster dies; ghouls leave their bones behind, which the others will not cross.</summary>
    private void Kill(Monster m, int bonus)
    {
        m.Alive = false;
        _reserved.Remove(m.Cell);
        Slain++;
        Score += (m.Kind == MonsterKind.Fish ? 150 : 100) + bonus;
        Play(Sfx.Kill);
        Dust(m.Pos, m.Kind == MonsterKind.Fish ? new Color(230, 100, 80) : new Color(200, 210, 170), 20);
        if (m.Kind == MonsterKind.Ghoul)
        {
            var c = Tomb.CellOf(m.Pos);
            if (Tomb[c] is Tile.Floor or Tile.DoorOpen)
                Tomb[c] = Tile.Skeleton;
        }
    }

    // ---------------------------------------------------------------- monsters

    private bool MonsterCan(Monster m, Point c)
    {
        if (Math.Max(Math.Abs(c.X - m.Home.X), Math.Abs(c.Y - m.Home.Y)) > Leash)
            return false;
        if (m.Kind == MonsterKind.Fish)
            return Tomb[c] == Tile.Water;
        return Tomb.WalkableForMonster(Tomb[c], PortcullisOpen);
    }

    private void UpdateMonsters()
    {
        var target = Cell;
        foreach (var m in Monsters)
        {
            if (!m.Alive)
                continue;
            m.Anim++;
            if (m.Stun > 0)
            {
                m.Stun--;
                continue;
            }
            float speed = m.Kind switch
            {
                MonsterKind.Guardian => 0.8f,
                MonsterKind.Fish => 0.95f,
                _ => 1.0f,
            } * MonsterSpeed;
            if (m.Moving)
            {
                var goal = Tomb.Centre(m.Cell);
                var d = goal - m.Pos;
                if (d.Length() <= speed)
                {
                    m.Pos = goal;
                    m.Moving = false;
                }
                else
                {
                    d.Normalize();
                    m.Pos += d * speed;
                }
            }
            if (!m.Moving)
                ChooseStep(m, target);
            if (Alive && Touches(m))
                Bite(m);
        }
    }

    private bool Aware(Monster m)
    {
        if (!Alive || Riding != null)
            return false;
        if (m.Kind == MonsterKind.Fish && !Swimming)
            return false;
        return Vector2.Distance(m.Pos, Pos) < 7.5f * Tomb.TileSize;
    }

    private void ChooseStep(Monster m, Point player)
    {
        var goal = Aware(m) ? player : m.Home;
        if (goal == m.Cell)
        {
            _reserved.Add(m.Cell);
            return;
        }
        _reserved.Remove(m.Cell);
        var next = Pathing.StepTowards(m.Cell, goal, c => MonsterCan(m, c) && !_reserved.Contains(c), 300);
        if (next is { } n)
        {
            var delta = new Point(n.X - m.Cell.X, n.Y - m.Cell.Y);
            m.Facing = delta.X > 0 ? Dir.Right : delta.X < 0 ? Dir.Left : delta.Y > 0 ? Dir.Down : Dir.Up;
            m.Cell = n;
            m.Moving = true;
        }
        _reserved.Add(m.Cell);
    }

    private bool Touches(Monster m)
    {
        if (m.Kind == MonsterKind.Fish && !Swimming)
            return false;
        float reach = m.Kind == MonsterKind.Guardian ? 14f : 12f;
        return Vector2.Distance(m.Pos, Pos) < reach;
    }

    private void Bite(Monster m)
    {
        if (GodMode || Invulnerable > 0)
        {
            m.Stun = 30;
            return;
        }
        Die(m.Kind switch
        {
            MonsterKind.Guardian => DeathCause.Guardian,
            MonsterKind.Fish => DeathCause.Fish,
            _ => DeathCause.Ghoul,
        });
    }

    // ---------------------------------------------------------------- traps

    private void UpdateDarts()
    {
        foreach (var (cell, dir, phase) in _launchers)
        {
            if ((Ticks + phase) % DartPeriod != 0)
                continue;
            var at = Tomb.Centre(cell);
            if (Vector2.Distance(at, Pos) > 10 * Tomb.TileSize)
                continue;
            Darts.Add(new Dart { Pos = at + dir.Vector() * 12f, Dir = dir });
            PlayNear(Sfx.Dart, at, 6);
        }
        for (int i = Darts.Count - 1; i >= 0; i--)
        {
            var d = Darts[i];
            d.Pos += d.Dir.Vector() * 3f;
            if (Tomb.BlocksShot(Tomb[Tomb.CellOf(d.Pos)], PortcullisOpen))
            {
                Darts.RemoveAt(i);
                continue;
            }
            if (Alive && JumpTicks == 0 && Riding == null && Vector2.Distance(d.Pos, Pos) < 8f)
            {
                Darts.RemoveAt(i);
                if (!GodMode && Invulnerable == 0)
                    Die(DeathCause.Dart);
            }
        }
    }

    private void UpdateBoulders()
    {
        foreach (var b in Boulders)
        {
            if (!b.Rolling || b.Gone)
                continue;
            b.Pos += b.Dir.Vector() * 2f;
            b.Angle += 2f / 11f * (b.Dir is Dir.Left or Dir.Up ? -1 : 1);
            if (Ticks % 6 == 0)
                Shake = Math.Max(Shake, 3);
            var ahead = Tomb.CellOf(b.Pos + b.Dir.Vector() * 11f);
            if (Tomb.BlocksWalker(Tomb[ahead], PortcullisOpen))
            {
                Smash(b);
                continue;
            }
            foreach (var m in Monsters)
                if (m.Alive && Vector2.Distance(m.Pos, b.Pos) < 14f)
                    Kill(m, 0);
            if (Alive && Vector2.Distance(b.Pos, Pos) < 15f && !GodMode && Invulnerable == 0)
                Die(DeathCause.Boulder);
        }
    }

    private void Smash(Boulder b)
    {
        b.Gone = true;
        b.Rolling = false;
        Play(Sfx.Smash);
        Dust(b.Pos, new Color(170, 150, 120), 40);
        Shake = Math.Max(Shake, 15);
    }

    // ---------------------------------------------------------------- the mine cart

    private bool TryBoard()
    {
        foreach (var cart in Carts)
        {
            if (cart.Moving || Vector2.Distance(cart.Pos, Pos) > 27f)
                continue;
            cart.Target = cart.Index < 0.5f ? cart.Line.Count - 1 : 0;
            Riding = cart;
            Pos = cart.Pos;
            JumpTicks = 0;
            Play(Sfx.Cart);
            Say(() => Strings.RideCart, 100);
            return true;
        }
        return false;
    }

    private void UpdateRide()
    {
        var cart = Riding;
        float step = 2f / Tomb.TileSize;
        if (MathF.Abs(cart.Index - cart.Target) <= step)
        {
            cart.Index = cart.Target;
            var end = cart.Line[cart.Target];
            var from = cart.Line[cart.Target == 0 ? Math.Min(1, cart.Line.Count - 1) : cart.Target - 1];
            var beyond = new Point(end.X + (end.X - from.X), end.Y + (end.Y - from.Y));
            cart.Target = -1;
            Riding = null;
            Pos = Tomb.Centre(beyond);
            Facing = beyond.X > end.X ? Dir.Right : beyond.X < end.X ? Dir.Left : beyond.Y > end.Y ? Dir.Down : Dir.Up;
            return;
        }
        cart.Index += MathF.Sign(cart.Target - cart.Index) * step;
        Pos = cart.Pos;
        Facing = cart.Vertical ? (cart.Target > 0 ? Dir.Down : Dir.Up) : (cart.Target > 0 ? Dir.Right : Dir.Left);
        if (Ticks % 50 == 0)
            Play(Sfx.Cart);
    }

    // ---------------------------------------------------------------- ACTION

    /// <summary>The square in front of the explorer.</summary>
    public Point Front => Tomb.CellOf(Pos + Facing.Vector() * 15f);

    /// <summary>A closed chest within reach, if any.</summary>
    public Item ChestInReach()
    {
        foreach (var item in Items)
            if (item.Kind == ItemKind.Chest && !item.Taken && !item.Opened && Vector2.Distance(item.Centre, Pos) < 20f)
                return item;
        return null;
    }

    public bool CartInReach()
    {
        foreach (var cart in Carts)
            if (!cart.Moving && Vector2.Distance(cart.Pos, Pos) <= 27f)
                return true;
        return false;
    }

    /// <summary>What ACTION would do now (for the on-screen hint), or null.</summary>
    public Func<string> ActionHint()
    {
        if (!Alive || Riding != null || Reading != null)
            return null;
        if (CartInReach())
            return () => Strings.HintRide;
        if (ChestInReach() != null)
            return () => Strings.HintOpen;
        return Tomb[Front] switch
        {
            Tile.Door or Tile.JadeDoor or Tile.BookGate or Tile.TwinGate => () => Strings.HintOpen,
            Tile.DoorOpen => () => Strings.HintClose,
            Tile.Tablet or Tile.Rosetta => () => Strings.HintRead,
            Tile.Lever => () => Strings.HintPull,
            Tile.Fountain => () => Strings.HintDrink,
            Tile.Sarcophagus => () => Strings.HintOpen,
            Tile.Wall or Tile.Secret or Tile.SkullWall => () => Strings.HintSearch,
            _ => null,
        };
    }

    private void Act()
    {
        var chest = ChestInReach();
        if (chest != null)
        {
            chest.Opened = true;
            Score += 500;
            TreasureFound++;
            Play(Sfx.Treasure);
            Sparkle(chest.Centre, new Color(255, 220, 90), 24);
            Say(() => Strings.ChestOpened, 120);
            return;
        }

        var front = Front;
        switch (Tomb[front])
        {
            case Tile.Door:
                Tomb[front] = Tile.DoorOpen;
                Play(Sfx.Door);
                break;
            case Tile.DoorOpen:
                if (CanClose(front))
                {
                    Tomb[front] = Tile.Door;
                    Play(Sfx.Door);
                }
                break;
            case Tile.Secret:
                Tomb[front] = Tile.Floor;
                Score += 50;
                SecretsFound++;
                Play(Sfx.Secret);
                Dust(Tomb.Centre(front), new Color(220, 180, 100), 24);
                Say(() => Strings.SecretFound, 150);
                break;
            case Tile.Wall:
            case Tile.SkullWall:
                Say(() => Strings.SolidStone, 60);
                break;
            case Tile.JadeDoor:
                if (HasKey)
                    Unlock(front, () => Strings.JadeOpens);
                else
                {
                    Play(Sfx.Locked);
                    Say(() => Strings.JadeLocked, 120);
                }
                break;
            case Tile.BookGate:
                if (HasBook)
                    Unlock(front, () => Strings.BookOpens);
                else
                {
                    Play(Sfx.Locked);
                    Say(() => Strings.GateWontOpen, 120);
                }
                break;
            case Tile.TwinGate:
                if (Idols >= 2)
                    Unlock(front, () => Strings.TwinsOpen);
                else
                {
                    Play(Sfx.Locked);
                    Say(() => Strings.GateWontOpen, 120);
                }
                break;
            case Tile.Lever:
                PortcullisOpen = !PortcullisOpen;
                Play(Sfx.Lever);
                Shake = Math.Max(Shake, 20);
                Say(() => PortcullisOpen ? Strings.PortcullisRises : Strings.PortcullisFalls, 120);
                break;
            case Tile.Portcullis:
                Say(() => Strings.PortcullisStuck, 120);
                break;
            case Tile.Tablet:
                Reading = Tomb.Param(front.X, front.Y);
                ReadingTicks = 0;
                Play(Sfx.Read);
                break;
            case Tile.Rosetta:
                Reading = 0;
                ReadingTicks = 0;
                if (!KnowsScript)
                {
                    KnowsScript = true;
                    Score += 250;
                }
                Play(Sfx.Read);
                break;
            case Tile.Fountain:
                Die(DeathCause.Youth);
                break;
            case Tile.Sarcophagus:
                Tomb[front] = Tile.SarcophagusOpen;
                HasStone = true;
                Score += 5000;
                Shake = 150;
                Play(Sfx.Stone);
                Sparkle(Tomb.Centre(front), new Color(160, 255, 255), 40);
                Say(() => Strings.StoneTaken, 300);
                break;
            case Tile.SarcophagusOpen:
                Say(() => Strings.SarcophagusEmpty, 100);
                break;
        }
    }

    private void Unlock(Point cell, Func<string> message)
    {
        Tomb[cell] = Tile.DoorOpen;
        Play(Sfx.Unlock);
        Sparkle(Tomb.Centre(cell), new Color(255, 230, 140), 20);
        Say(message, 150);
    }

    private bool CanClose(Point cell)
    {
        if (Cell == cell)
            return false;
        var r = new Rectangle(cell.X * Tomb.TileSize, cell.Y * Tomb.TileSize, Tomb.TileSize, Tomb.TileSize);
        if (r.Contains((int)(Pos.X - HalfSize), (int)Pos.Y) || r.Contains((int)(Pos.X + HalfSize), (int)Pos.Y) ||
            r.Contains((int)Pos.X, (int)(Pos.Y - HalfSize)) || r.Contains((int)Pos.X, (int)(Pos.Y + HalfSize)))
            return false;
        foreach (var m in Monsters)
            if (m.Alive && (m.Cell == cell || Tomb.CellOf(m.Pos) == cell))
                return false;
        return true;
    }

    // ---------------------------------------------------------------- death and victory

    private void Die(DeathCause cause)
    {
        if (!Alive)
            return;
        Dying = cause;
        DeathTicks = 0;
        Lives--;
        JumpTicks = 0;
        Walking = false;
        Shake = Math.Max(Shake, 12);
        Play(cause switch
        {
            DeathCause.Pit or DeathCause.Chasm or DeathCause.Collapse => Sfx.Fall,
            DeathCause.Drowned => Sfx.Drown,
            _ => Sfx.Death,
        });
        Say(() => Strings.DeathText(cause), DeathPause);
        if (cause is DeathCause.Ghoul or DeathCause.Guardian or DeathCause.Dart or DeathCause.Boulder or DeathCause.Fish)
            Dust(Pos, new Color(200, 40, 30), 20);
    }

    private void Respawn()
    {
        Dying = DeathCause.None;
        Pos = Tomb.Centre(Checkpoint);
        Camera = Pos;
        Facing = Dir.Down;
        Air = MaxAir;
        Swimming = false;
        Riding = null;
        Invulnerable = 150;
        Darts.Clear();
        Bolts.Clear();
        _reserved.Clear();
        foreach (var m in Monsters)
        {
            if (!m.Alive)
                continue;
            m.Cell = m.Home;
            m.Pos = Tomb.Centre(m.Home);
            m.Moving = false;
            m.Stun = 50;
        }
        foreach (var cart in Carts)
            cart.Target = -1;
        Flash = 15;
        Say(() => Strings.LivesLeft(Lives), 120);
    }

    private void Win()
    {
        Outcome = Outcome.Won;
        Bonus = (int)((Lives * 1000 + Math.Max(0, 3000 - Seconds * 2)) * BonusFactor);
        Score += Bonus;
        Play(Sfx.Victory);
    }

    // ---------------------------------------------------------------- camera and effects

    /// <summary>The play area below the title bar and above the status bar.</summary>
    public const float ViewWidth = 240f;
    public const float ViewHeight = 188f;

    private void UpdateCamera()
    {
        var target = Pos;
        var cam = Camera + (target - Camera) * 0.15f;
        Camera = ClampCamera(cam);
    }

    /// <summary>Puts the camera straight on the explorer (after a jump cut).</summary>
    public void SnapCamera()
    {
        Camera = ClampCamera(Pos);
    }

    /// <summary>Keeps the view inside the tomb (or centred on a tomb smaller than the view).</summary>
    private Vector2 ClampCamera(Vector2 cam)
    {
        float mw = Tomb.Width * Tomb.TileSize, mh = Tomb.Height * Tomb.TileSize;
        cam.X = mw <= ViewWidth ? mw / 2 : Math.Clamp(cam.X, ViewWidth / 2, mw - ViewWidth / 2);
        cam.Y = mh <= ViewHeight ? mh / 2 : Math.Clamp(cam.Y, ViewHeight / 2, mh - ViewHeight / 2);
        return cam;
    }

    private void UpdateParticles()
    {
        for (int i = Particles.Count - 1; i >= 0; i--)
        {
            var p = Particles[i];
            p.Pos += p.Vel;
            p.Vel *= 0.9f;
            if (++p.Life >= p.Max)
                Particles.RemoveAt(i);
        }
    }

    private void Dust(Vector2 at, Color colour, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float a = (float)(_random.NextDouble() * Math.PI * 2);
            float s = 0.4f + (float)_random.NextDouble() * 1.6f;
            Particles.Add(new Particle
            {
                Pos = at, Vel = new Vector2(MathF.Cos(a), MathF.Sin(a)) * s, Max = 25 + _random.Next(25),
                Color = colour, Size = 1.2f + (float)_random.NextDouble() * 1.8f,
            });
        }
    }

    private void Sparkle(Vector2 at, Color colour, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float a = (float)(_random.NextDouble() * Math.PI * 2);
            float s = 0.5f + (float)_random.NextDouble() * 1.5f;
            Particles.Add(new Particle
            {
                Pos = at, Vel = new Vector2(MathF.Cos(a), MathF.Sin(a)) * s, Max = 18 + _random.Next(18),
                Color = colour, Size = 1f + (float)_random.NextDouble(), Glow = true,
            });
        }
    }

    private void Splash(Vector2 at)
    {
        for (int i = 0; i < 12; i++)
        {
            float a = (float)(_random.NextDouble() * Math.PI * 2);
            Particles.Add(new Particle
            {
                Pos = at, Vel = new Vector2(MathF.Cos(a), MathF.Sin(a)) * 1.2f, Max = 20, Color = new Color(200, 240, 255),
                Size = 1.2f, Glow = true,
            });
        }
    }
}
