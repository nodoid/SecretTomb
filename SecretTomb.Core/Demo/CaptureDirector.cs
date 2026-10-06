using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SecretTomb.Core.Audio;
using SecretTomb.Core.Engine;
using SecretTomb.Core.Localization;
using SecretTomb.Core.Scenes;
using SecretTomb.Core.World;

namespace SecretTomb.Core.Demo;

/// <summary>
/// Store asset capture: plays a list of scenes, saving frames as PNG (stills, or every tick for
/// videos) and recording the sound effects so a matching audio track can be mixed. Start with
/// <c>--capture &lt;folder&gt; [stills|videos|all] [--mobile] [--lang fr|en] [--scale n]</c>.
/// </summary>
public sealed class CaptureDirector : ISoundPlayer
{
    /// <summary>
    /// One capture: the scene to make, how long to run it before saving, how many frames to save,
    /// and (for the tomb) the moment to wait for.
    /// </summary>
    public sealed record Shot(string Name, Func<SecretTombGame, ISoundPlayer, Scene> Create, int SkipTicks,
        int Frames = 1, Func<Adventure, bool> Ready = null);

    private readonly string _folder;
    private readonly List<Shot> _shots;
    private int _index = -1;
    private Scene _scene;
    private int _frame;
    private bool _saveThisFrame;
    private bool _recording;
    private readonly List<(int frame, Sfx sfx)> _events = new();

    public CaptureDirector(string folder, List<Shot> shots, bool mobile, Language language, float scale)
    {
        Scale = scale;
        _folder = folder;
        _shots = shots;
        Mobile = mobile;
        Language = language;
    }

    public bool Mobile { get; }

    /// <summary>Capture resolution in device pixels per virtual pixel (10 gives 2400 x 2240).</summary>
    public float Scale { get; }
    public Language Language { get; }
    public bool Finished { get; private set; }

    public static CaptureDirector FromArgs(string[] args)
    {
        int i = Array.IndexOf(args, "--capture");
        if (i < 0 || i + 1 >= args.Length)
            return null;
        string folder = args[i + 1];
        string what = i + 2 < args.Length && !args[i + 2].StartsWith("--") ? args[i + 2] : "all";
        bool mobile = Array.IndexOf(args, "--mobile") >= 0;
        int l = Array.IndexOf(args, "--lang");
        var language = l >= 0 && l + 1 < args.Length ? Strings.FromIso(args[l + 1]) : Language.English;
        var shots = new List<Shot>();
        if (what is "stills" or "all")
            shots.AddRange(Stills());
        if (what is "videos" or "all")
            shots.AddRange(Videos());
        int sc = Array.IndexOf(args, "--scale");
        float scale = sc >= 0 && sc + 1 < args.Length && float.TryParse(args[sc + 1], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float s) ? s : what == "videos" ? 5f : 10f;
        return new CaptureDirector(folder, shots, mobile, language, scale);
    }

    private static Scene Tomb(SecretTombGame g, ISoundPlayer s) => new DemoScene(g, s, attract: false);

    private static bool Within(Adventure a, int x0, int y0, int x1, int y1) =>
        a.Cell.X >= x0 && a.Cell.X <= x1 && a.Cell.Y >= y0 && a.Cell.Y <= y1;

    private static bool Near(Adventure a, MonsterKind kind, float squares)
    {
        foreach (var m in a.Monsters)
            if (m.Alive && m.Kind == kind && Vector2.Distance(m.Pos, a.Pos) < squares * World.Tomb.TileSize)
                return true;
        return false;
    }

    private static bool BoulderRolling(Adventure a)
    {
        foreach (var b in a.Boulders)
            if (b.Rolling && !b.Gone)
                return true;
        return false;
    }

    private static bool CartHalfway(Adventure a) => a.Riding is { } c && c.Index > c.Line.Count * 0.45f;

    // The moments worth showing, in the order the autopilot reaches them.
    private static bool HallFight(Adventure a) => a.HasPistol && Within(a, 19, 11, 39, 16) && a.Bolts.Count > 0;
    private static bool Rosetta(Adventure a) => a.Reading == 0 && a.ReadingTicks > 20;
    private static bool Pillars(Adventure a) => Within(a, 41, 8, 58, 20) && Near(a, MonsterKind.Guardian, 3.5f) && !a.HasKey;
    private static bool Boulder(Adventure a) => BoulderRolling(a) && a.Boulders[0].Pos.Y > 26 * World.Tomb.TileSize;
    private static bool Fountain(Adventure a) => a.Reading == 2 && a.ReadingTicks > 20;
    private static bool Teleport(Adventure a) => a.Idols == 0 && a.Flash > 0 && a.Flash < 6 && Within(a, 51, 40, 58, 46);
    private static bool Swim(Adventure a) => a.Swimming && a.HasHelmet && Near(a, MonsterKind.Fish, 3f);
    private static bool Darts(Adventure a) => Within(a, 10, 13, 15, 13) && a.Darts.Count > 0 && a.Darts[0].Pos.Y > 12.8f * World.Tomb.TileSize;
    private static bool Cart(Adventure a) => CartHalfway(a);
    private static bool Library(Adventure a) => Within(a, 2, 39, 14, 45) && a.Bolts.Count > 0;
    private static bool Burial(Adventure a) => Within(a, 32, 38, 44, 46) && !a.HasStone && Near(a, MonsterKind.Guardian, 4f);
    private static bool Stone(Adventure a) => a.HasStone && a.Shake > 110;

    /// <summary>The still screenshots, in store order.</summary>
    public static IEnumerable<Shot> Stills()
    {
        yield return new Shot("01-hall", Tomb, 0, Ready: HallFight);
        yield return new Shot("02-title", (g, s) => new TitleScene(g), 100);
        yield return new Shot("03-pillars", Tomb, 0, Ready: Pillars);
        yield return new Shot("04-swim", Tomb, 0, Ready: Swim);
        yield return new Shot("05-rosetta", Tomb, 0, Ready: Rosetta);
        yield return new Shot("06-boulder", Tomb, 0, Ready: Boulder);
        yield return new Shot("07-cart", Tomb, 0, Ready: Cart);
        yield return new Shot("08-burial", Tomb, 0, Ready: Burial);
        yield return new Shot("09-darts", Tomb, 0, Ready: Darts);
        yield return new Shot("10-teleport", Tomb, 0, Ready: Teleport);
        yield return new Shot("11-library", Tomb, 0, Ready: Library);
        yield return new Shot("12-stone", Tomb, 0, Ready: Stone);
        yield return new Shot("13-victory", (g, s) => VictoryScene(g), 30);
        yield return new Shot("14-menu", (g, s) => new MenuScene(g), 30);
        yield return new Shot("15-instructions", (g, s) => new InstructionsScene(g), 5);
        yield return new Shot("16-fountain", Tomb, 0, Ready: Fountain);
    }

    /// <summary>One continuous montage of the game in action (about 29 seconds at 50 frames a second).</summary>
    public static IEnumerable<Shot> Videos()
    {
        yield return new Shot("video/01", (g, s) => new TitleScene(g), 20, 140);
        yield return new Shot("video/02", Tomb, 0, 140, a => a.Ticks >= 60);
        yield return new Shot("video/03", Tomb, 0, 150, a => a.HasPistol && a.Ticks > 0 && Within(a, 19, 11, 39, 16));
        yield return new Shot("video/04", Tomb, 0, 150, a => Within(a, 41, 8, 58, 20) && !a.HasKey && Near(a, MonsterKind.Guardian, 6f));
        yield return new Shot("video/05", Tomb, 0, 130, a => BoulderRolling(a));
        yield return new Shot("video/06", Tomb, 0, 140, a => a.Swimming && a.HasHelmet);
        yield return new Shot("video/07", Tomb, 0, 120, a => Within(a, 6, 13, 16, 13) && a.Idols == 2);
        yield return new Shot("video/08", Tomb, 0, 150, a => a.Riding != null);
        yield return new Shot("video/09", Tomb, 0, 150, a => Within(a, 32, 38, 44, 46) && !a.HasStone);
        yield return new Shot("video/10", (g, s) => VictoryScene(g), 0, 140);
    }

    private static Scene VictoryScene(SecretTombGame g)
    {
        var demo = new DemoScene(g, SilentSoundPlayer.Instance, attract: false);
        demo.FastForward(a => a.Outcome != Outcome.Playing);
        return new EndScene(g, demo.Adventure);
    }

    public void Play(Sfx sfx, float pitch = 0f)
    {
        if (_index >= 0 && _shots[_index].Frames > 1 && _recording)
            _events.Add((_frame, sfx));
    }

    /// <summary>Called instead of the normal update while capturing.</summary>
    public void Update(SecretTombGame game)
    {
        if (Finished)
            return;
        if (_scene == null)
        {
            if (++_index >= _shots.Count)
            {
                Finished = true;
                game.Exit();
                return;
            }
            var shot0 = _shots[_index];
            _recording = false;
            _scene = shot0.Create(game, this);
            _scene.Enter();
            game.ShowCaptureScene(_scene);
            for (int i = 0; i < shot0.SkipTicks; i++)
                _scene.Tick();
            if (shot0.Ready != null && _scene is DemoScene demo)
            {
                demo.FastForward(shot0.Ready);
                demo.Adventure.SnapCamera();
            }
            _frame = 0;
            _events.Clear();
            _recording = true;
        }

        _scene.Tick();
        _saveThisFrame = true;
    }

    /// <summary>Called after the screen has been drawn.</summary>
    public void AfterDraw(RenderTarget2D target)
    {
        if (!_saveThisFrame || _index < 0 || _index >= _shots.Count)
            return;
        _saveThisFrame = false;
        var shot = _shots[_index];
        string path = shot.Frames > 1
            ? Path.Combine(_folder, shot.Name, $"f{_frame:D5}.png")
            : Path.Combine(_folder, "stills", shot.Name + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using (var file = File.Create(path))
            target.SaveAsPng(file, target.Width, target.Height);
        _frame++;
        if (_frame >= shot.Frames)
        {
            if (shot.Frames > 1)
                WriteAudio(Path.Combine(_folder, shot.Name + ".wav"), shot.Frames, shot.Name == "video/01");
            Console.WriteLine($"captured {shot.Name}");
            _scene.Leave();
            _scene = null;
        }
    }

    private void WriteAudio(string path, int frames, bool tune)
    {
        var clips = new Dictionary<Sfx, short[]>();
        foreach (var (id, pcm) in SoundBank.Build())
            clips[id] = pcm;
        int total = frames * Synth.SampleRate / SecretTombGame.TicksPerSecond;
        var mix = new int[total];
        void Add(short[] clip, int start)
        {
            for (int i = 0; i < clip.Length && start + i < total; i++)
                mix[start + i] += clip[i] * 6 / 10;
        }
        if (tune)
            Add(SoundBank.TitleTune(), 0);
        foreach (var (frame, sfx) in _events)
            Add(clips[sfx], frame * Synth.SampleRate / SecretTombGame.TicksPerSecond);
        var samples = new short[total];
        for (int i = 0; i < total; i++)
            samples[i] = (short)Math.Clamp(mix[i], short.MinValue, short.MaxValue);
        Wav.Write(path, samples, Synth.SampleRate);
    }
}

public static class Wav
{
    /// <summary>Writes 16-bit mono PCM as a .wav file.</summary>
    public static void Write(string path, short[] samples, int rate)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var w = new BinaryWriter(File.Create(path));
        int bytes = samples.Length * 2;
        w.Write("RIFF"u8.ToArray());
        w.Write(36 + bytes);
        w.Write("WAVE"u8.ToArray());
        w.Write("fmt "u8.ToArray());
        w.Write(16);
        w.Write((short)1);
        w.Write((short)1);
        w.Write(rate);
        w.Write(rate * 2);
        w.Write((short)2);
        w.Write((short)16);
        w.Write("data"u8.ToArray());
        w.Write(bytes);
        foreach (var s in samples)
            w.Write(s);
    }
}
