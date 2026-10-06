using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace SecretTomb.Core.World;

/// <summary>What a square of the tomb is made of.</summary>
public enum Tile : byte
{
    Wall,
    Floor,
    Grass,
    Tree,
    Secret,        // a wall that is a hidden door
    Door,          // closed stone door (opened and closed with ACTION)
    DoorOpen,
    JadeDoor,      // needs the jade key
    BookGate,      // needs the sacred book
    TwinGate,      // needs both twin idols
    Portcullis,    // raised by the lever
    Water,
    Pit,
    Chasm,
    Rail,          // rails over the chasm: only the mine cart can cross
    Stairs,
    Gateway,       // the pyramid's entrance (the way out)
    Fountain,      // the fountain of youth
    Sarcophagus,
    SarcophagusOpen,
    Tablet,        // an inscription in the old script
    Rosetta,       // "to the glory of Axayacatl", with its translation
    Lever,
    Teleporter,
    Launcher,      // a serpent's head that shoots darts
    Plate,         // pressure plate: sets a boulder rolling
    Cracked,       // looks like the floor, but gives way
    SkullWall,
    SunStone,      // checkpoint
    Skeleton,      // bones: monsters will not cross them
}

/// <summary>The four directions the explorer and the darts can face.</summary>
public enum Dir : byte
{
    Up,
    Right,
    Down,
    Left,
}

public static class DirExtensions
{
    public static Point Step(this Dir d) => d switch
    {
        Dir.Up => new Point(0, -1),
        Dir.Right => new Point(1, 0),
        Dir.Down => new Point(0, 1),
        _ => new Point(-1, 0),
    };

    public static Vector2 Vector(this Dir d) => d.Step().ToVector2();

    public static Dir Opposite(this Dir d) => (Dir)(((int)d + 2) % 4);

    public static float Angle(this Dir d) => (int)d * MathF.PI / 2;
}

/// <summary>Things lying in the tomb that the explorer can pick up (or open).</summary>
public enum ItemKind : byte
{
    Pistol,
    Ammo,
    Gem,
    Chest,
    JadeKey,
    Book,
    Idol,
    Helmet,
}

public enum MonsterKind : byte
{
    Ghoul,
    Guardian,
    Fish,
}

public sealed record ItemSpawn(ItemKind Kind, Point Cell);

public sealed record MonsterSpawn(MonsterKind Kind, Point Cell);

/// <summary>
/// The tomb of Axayacatl as loaded from its character map (World/Tomb.map). One character per
/// square:
/// <code>
///   T tree        , jungle      # wall          % secret door   . floor       Z skull wall
///   D door        J jade door   B book gate     W twin gate     | portcullis  l lever
///   ~ water       ^ pit         : chasm         = rails         c mine cart (on rails)
///   s stairs      E gateway     F fountain      X sarcophagus   R inscription (3 wide)
///   1-5 tablets   t teleporter (in pairs)       v &lt; &gt; n dart launchers (shooting down, left, right, up)
///   _ plate       O boulder     + cracked floor o sun stone (checkpoint)    x skeleton
///   p pistol (on a skeleton)    a ammunition    * gem   $ chest   k jade key   b sacred book
///   i twin idol   h diving helmet               m ghoul   G guardian   f piranha (in water)
///   @ start
/// </code>
/// </summary>
public sealed class Tomb
{
    public const int TileSize = 24;

    private readonly Tile[] _tiles;
    private readonly byte[] _param;

    public Tomb(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new Tile[width * height];
        _param = new byte[width * height];
    }

    public int Width { get; }
    public int Height { get; }

    public Point Start { get; private set; }
    public List<ItemSpawn> Items { get; } = new();
    public List<MonsterSpawn> Monsters { get; } = new();
    public List<Point> Boulders { get; } = new();
    public List<Point> Carts { get; } = new();

    /// <summary>The two ends of each teleporter pair.</summary>
    public List<(Point a, Point b)> Teleporters { get; } = new();

    /// <summary>Squares where the jungle ends and the pyramid begins (rows above are outside).</summary>
    public int OutsideRows { get; private set; }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
    public bool InBounds(Point p) => InBounds(p.X, p.Y);

    public Tile this[int x, int y]
    {
        get => InBounds(x, y) ? _tiles[y * Width + x] : Tile.Wall;
        set
        {
            if (InBounds(x, y))
                _tiles[y * Width + x] = value;
        }
    }

    public Tile this[Point p]
    {
        get => this[p.X, p.Y];
        set => this[p.X, p.Y] = value;
    }

    /// <summary>Extra data for a square: tablet number, launcher direction, inscription part.</summary>
    public byte Param(int x, int y) => InBounds(x, y) ? _param[y * Width + x] : (byte)0;

    public void SetParam(int x, int y, byte value)
    {
        if (InBounds(x, y))
            _param[y * Width + x] = value;
    }

    public static Point CellOf(Vector2 p) => new((int)MathF.Floor(p.X / TileSize), (int)MathF.Floor(p.Y / TileSize));

    public static Vector2 Centre(Point c) => new(c.X * TileSize + TileSize / 2f, c.Y * TileSize + TileSize / 2f);

    /// <summary>A stable per-square number used to vary the artwork.</summary>
    public static int Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 73856093 ^ y * 19349663;
            h ^= h >> 13;
            h *= 0x5bd1e995;
            return (h ^ (h >> 15)) & 0x7FFFFFFF;
        }
    }

    // ---------------------------------------------------------------- what blocks what

    /// <summary>Squares that stop the explorer (walls and closed doors; hazards are walkable).</summary>
    public static bool BlocksWalker(Tile t, bool portcullisOpen) => t switch
    {
        Tile.Wall or Tile.Tree or Tile.Secret or Tile.Door or Tile.JadeDoor or Tile.BookGate or Tile.TwinGate
            or Tile.Fountain or Tile.Sarcophagus or Tile.SarcophagusOpen or Tile.Tablet or Tile.Rosetta or Tile.Lever
            or Tile.Launcher or Tile.SkullWall => true,
        Tile.Portcullis => !portcullisOpen,
        _ => false,
    };

    /// <summary>Squares a laser bolt or a dart cannot pass.</summary>
    public static bool BlocksShot(Tile t, bool portcullisOpen) => BlocksWalker(t, portcullisOpen);

    /// <summary>Squares a ghoul or guardian may walk on: never stairs, bones, water or holes.</summary>
    public static bool WalkableForMonster(Tile t, bool portcullisOpen) => t switch
    {
        Tile.Floor or Tile.DoorOpen or Tile.Plate or Tile.SunStone or Tile.Teleporter => true,
        Tile.Portcullis => portcullisOpen,
        _ => false,
    };

    public bool IsWater(Point p) => this[p] == Tile.Water;

    // ---------------------------------------------------------------- loading

    public static Tomb Load()
    {
        using var stream = typeof(Tomb).Assembly.GetManifestResourceStream("Tomb.map")
                           ?? throw new InvalidOperationException("Tomb.map is missing");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    public static Tomb Parse(string text)
    {
        var lines = new List<string>();
        foreach (var raw in text.Replace("\r", "").Split('\n'))
            if (raw.Length > 0)
                lines.Add(raw);
        int width = 0;
        foreach (var l in lines)
            width = Math.Max(width, l.Length);
        var tomb = new Tomb(width, lines.Count);
        var teleporters = new List<Point>();
        int outside = 0;

        for (int y = 0; y < lines.Count; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char ch = x < lines[y].Length ? lines[y][x] : '#';
                var p = new Point(x, y);
                Tile t = Tile.Floor;
                switch (ch)
                {
                    case 'T': t = Tile.Tree; break;
                    case ',': t = Tile.Grass; break;
                    case '#': t = Tile.Wall; break;
                    case '%': t = Tile.Secret; break;
                    case '.': t = Tile.Floor; break;
                    case 'Z': t = Tile.SkullWall; break;
                    case 'D': t = Tile.Door; break;
                    case 'J': t = Tile.JadeDoor; break;
                    case 'B': t = Tile.BookGate; break;
                    case 'W': t = Tile.TwinGate; break;
                    case '|': t = Tile.Portcullis; break;
                    case 'l': t = Tile.Lever; break;
                    case '~': t = Tile.Water; break;
                    case '^': t = Tile.Pit; break;
                    case ':': t = Tile.Chasm; break;
                    case '=': t = Tile.Rail; break;
                    case 'c': t = Tile.Rail; tomb.Carts.Add(p); break;
                    case 's': t = Tile.Stairs; break;
                    case 'E': t = Tile.Gateway; break;
                    case 'F': t = Tile.Fountain; break;
                    case 'X': t = Tile.Sarcophagus; break;
                    case 'R': t = Tile.Rosetta; break;
                    case >= '1' and <= '9': t = Tile.Tablet; tomb.SetParam(x, y, (byte)(ch - '0')); break;
                    case 't': t = Tile.Teleporter; teleporters.Add(p); break;
                    case 'v': t = Tile.Launcher; tomb.SetParam(x, y, (byte)Dir.Down); break;
                    case '<': t = Tile.Launcher; tomb.SetParam(x, y, (byte)Dir.Left); break;
                    case '>': t = Tile.Launcher; tomb.SetParam(x, y, (byte)Dir.Right); break;
                    case 'n': t = Tile.Launcher; tomb.SetParam(x, y, (byte)Dir.Up); break;
                    case '_': t = Tile.Plate; break;
                    case 'O': t = Tile.Floor; tomb.Boulders.Add(p); break;
                    case '+': t = Tile.Cracked; break;
                    case 'o': t = Tile.SunStone; break;
                    case 'x': t = Tile.Skeleton; break;
                    case 'p': t = Tile.Skeleton; tomb.Items.Add(new ItemSpawn(ItemKind.Pistol, p)); break;
                    case 'a': tomb.Items.Add(new ItemSpawn(ItemKind.Ammo, p)); break;
                    case '*': tomb.Items.Add(new ItemSpawn(ItemKind.Gem, p)); break;
                    case '$': tomb.Items.Add(new ItemSpawn(ItemKind.Chest, p)); break;
                    case 'k': tomb.Items.Add(new ItemSpawn(ItemKind.JadeKey, p)); break;
                    case 'b': tomb.Items.Add(new ItemSpawn(ItemKind.Book, p)); break;
                    case 'i': tomb.Items.Add(new ItemSpawn(ItemKind.Idol, p)); break;
                    case 'h': tomb.Items.Add(new ItemSpawn(ItemKind.Helmet, p)); break;
                    case 'm': tomb.Monsters.Add(new MonsterSpawn(MonsterKind.Ghoul, p)); break;
                    case 'G': tomb.Monsters.Add(new MonsterSpawn(MonsterKind.Guardian, p)); break;
                    case 'f': t = Tile.Water; tomb.Monsters.Add(new MonsterSpawn(MonsterKind.Fish, p)); break;
                    case '@': t = Tile.Grass; tomb.Start = p; break;
                    default: t = Tile.Wall; break;
                }
                tomb[x, y] = t;
                if (t == Tile.Gateway)
                    outside = y;
            }
        }

        // Inscription parts, left to right (it is drawn once across all three squares).
        for (int y = 0; y < tomb.Height; y++)
        {
            int run = 0;
            for (int x = 0; x < tomb.Width; x++)
            {
                run = tomb[x, y] == Tile.Rosetta ? run + 1 : 0;
                if (run > 0)
                    tomb.SetParam(x, y, (byte)(run - 1));
            }
        }

        for (int i = 0; i + 1 < teleporters.Count; i += 2)
            tomb.Teleporters.Add((teleporters[i], teleporters[i + 1]));
        tomb.OutsideRows = outside;
        return tomb;
    }

    /// <summary>The straight line of rails a cart stands on, from one end to the other.</summary>
    public List<Point> RailLine(Point cart)
    {
        var line = new List<Point>();
        bool vertical = this[cart.X, cart.Y - 1] == Tile.Rail || this[cart.X, cart.Y + 1] == Tile.Rail;
        var step = vertical ? new Point(0, 1) : new Point(1, 0);
        var p = cart;
        while (this[p.X - step.X, p.Y - step.Y] == Tile.Rail)
            p = new Point(p.X - step.X, p.Y - step.Y);
        while (this[p] == Tile.Rail)
        {
            line.Add(p);
            p = new Point(p.X + step.X, p.Y + step.Y);
        }
        return line;
    }
}
