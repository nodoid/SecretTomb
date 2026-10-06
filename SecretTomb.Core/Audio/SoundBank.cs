using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace SecretTomb.Core.Audio;

/// <summary>Builds every sound effect at start-up from <see cref="Synth"/> and plays them.</summary>
public sealed class SoundBank : ISoundPlayer, IDisposable
{
    private readonly Dictionary<Sfx, SoundEffect> _effects = new();
    private readonly bool _enabled;
    private static readonly float[] VolumeSteps = [0.6f, 0.3f, 0f, 1f];
    private readonly SoundEffect _tune;
    private SoundEffectInstance _tuneInstance;
    private int _volumeIndex;

    public SoundBank()
    {
        try
        {
            foreach (var (id, pcm) in Build())
                _effects[id] = new SoundEffect(Synth.ToBytes(pcm), Synth.SampleRate, AudioChannels.Mono);
            _tune = new SoundEffect(Synth.ToBytes(TitleTune()), Synth.SampleRate, AudioChannels.Mono);
            _enabled = true;
        }
        catch (Exception)
        {
            // No audio device (e.g. headless CI): run silently.
            _enabled = false;
        }
    }

    /// <summary>Silences everything without changing the volume setting (store captures).</summary>
    public bool Muted { get; set; }

    public float Volume => VolumeSteps[_volumeIndex];

    /// <summary>0 off, 1 low, 2 medium, 3 high.</summary>
    public int Level => Volume <= 0f ? 0 : Volume < 0.5f ? 1 : Volume < 0.9f ? 2 : 3;

    /// <summary>Cycles the volume. Returns the new level 0..3 (0 = mute).</summary>
    public int CycleVolume()
    {
        _volumeIndex = (_volumeIndex + 1) % VolumeSteps.Length;
        Play(Sfx.Menu);
        return Level;
    }

    public void Play(Sfx sfx, float pitch = 0f)
    {
        if (!_enabled || Muted || Volume <= 0f)
            return;
        if (_effects.TryGetValue(sfx, out var e))
        {
            try
            {
                e.Play(Volume, Math.Clamp(pitch, -1f, 1f), 0f);
            }
            catch (Exception)
            {
            }
        }
    }

    internal static IEnumerable<(Sfx, short[])> Build()
    {
        yield return (Sfx.Step, Synth.Noise(0.02, 0.18, 0, 16));
        yield return (Sfx.Laser, Synth.Sweep(0.16, 2400, 500, 0.4, 0.05));
        yield return (Sfx.Ricochet, Synth.Concat(Synth.Sweep(0.05, 3000, 2600, 0.35, 0.2), Synth.Sweep(0.14, 2200, 3400, 0.25, 0)));
        yield return (Sfx.Kill, Synth.Mix(Synth.Noise(0.3, 0.55, 0, 3), Synth.Sweep(0.3, 500, 80, 0.35, 0)));
        yield return (Sfx.Empty, Synth.Tune(0.3, (180, 0.04), (0, 0.04), (180, 0.04)));
        yield return (Sfx.Take, Synth.Tune(0.4, (784, 0.05), (1047, 0.08)));
        yield return (Sfx.Ammo, Synth.Concat(Synth.Sweep(0.08, 600, 1600, 0.35, 0.35), Synth.Sweep(0.08, 800, 2000, 0.35, 0)));
        yield return (Sfx.Treasure, Synth.Tune(0.4, (1047, 0.06), (1319, 0.06), (1568, 0.06), (2093, 0.18)));
        yield return (Sfx.Door, Synth.Mix(Synth.Sweep(0.35, 120, 70, 0.35, 0.1), Synth.Noise(0.35, 0.25, 0, 24)));
        yield return (Sfx.Secret, Synth.Concat(Synth.Noise(0.3, 0.3, 0.1, 30), Synth.Tune(0.35, (523, 0.08), (659, 0.08), (784, 0.08), (1047, 0.2))));
        yield return (Sfx.Locked, Synth.Tune(0.35, (196, 0.08), (0, 0.04), (196, 0.12)));
        yield return (Sfx.Unlock, Synth.Concat(Synth.Noise(0.05, 0.5, 0, 4), Synth.Tune(0.4, (659, 0.07), (988, 0.15))));
        yield return (Sfx.Dart, Synth.Mix(Synth.Noise(0.12, 0.3, 0, 1), Synth.Sweep(0.12, 1800, 1200, 0.15, 0)));
        yield return (Sfx.Boulder, Synth.Noise(1.4, 0.55, 0.3, 60));
        yield return (Sfx.Smash, Synth.Mix(Synth.Noise(0.5, 0.7, 0, 8), Synth.Sweep(0.5, 160, 40, 0.4, 0)));
        yield return (Sfx.Splash, Synth.Concat(Synth.Noise(0.12, 0.45, 0.1, 2), Synth.Noise(0.2, 0.2, 0, 5)));
        yield return (Sfx.Jump, Synth.Sweep(0.16, 300, 700, 0.3, 0.1));
        yield return (Sfx.Fall, Synth.Concat(Synth.Sweep(0.8, 1400, 110, 0.35, 0.25), Synth.Noise(0.25, 0.6, 0, 6)));
        yield return (Sfx.Death, Synth.Tune(0.4, (392, 0.2), (370, 0.2), (349, 0.2), (330, 0.5)));
        yield return (Sfx.Drown, Synth.Concat(Synth.Sweep(0.15, 300, 600, 0.3, 0.3), Synth.Sweep(0.15, 250, 550, 0.3, 0.3),
            Synth.Sweep(0.6, 400, 80, 0.35, 0)));
        yield return (Sfx.Teleport, Synth.Concat(Synth.Sweep(0.25, 200, 2400, 0.3, 0.3), Synth.Sweep(0.25, 2400, 300, 0.3, 0)));
        yield return (Sfx.Cart, Synth.Mix(Synth.Noise(1.0, 0.3, 0.2, 40), Synth.Sweep(1.0, 90, 120, 0.15, 0.1)));
        yield return (Sfx.Lever, Synth.Concat(Synth.Noise(0.06, 0.5, 0, 6), Synth.Sweep(0.6, 70, 50, 0.4, 0.1)));
        yield return (Sfx.Checkpoint, Synth.Tune(0.35, (659, 0.08), (880, 0.08), (1319, 0.16)));
        yield return (Sfx.Read, Synth.Tune(0.3, (440, 0.06), (554, 0.06), (659, 0.12)));
        yield return (Sfx.Stone, Synth.Tune(0.45, (523, 0.1), (659, 0.1), (784, 0.1), (1047, 0.1), (1319, 0.1), (1568, 0.4)));
        yield return (Sfx.Victory, Synth.Tune(0.45, (392, 0.15), (523, 0.15), (659, 0.15), (784, 0.3), (659, 0.15),
            (784, 0.15), (1047, 0.6)));
        yield return (Sfx.Menu, Synth.Sweep(0.05, 900, 1200, 0.3, 0.1));
        yield return (Sfx.Start, Synth.Tune(0.4, (294, 0.12), (349, 0.12), (440, 0.12), (587, 0.3)));
        yield return (Sfx.Warning, Synth.Tune(0.35, (880, 0.08), (0, 0.06), (880, 0.08)));
    }

    /// <summary>The title tune: a pentatonic melody over a drum, like a flute in the jungle.</summary>
    internal static short[] TitleTune()
    {
        const double q = 0.17;
        var melody = Synth.Tune(0.3,
            (587, q), (659, q), (784, q * 2), (659, q), (587, q), (523, q * 2),
            (440, q), (523, q), (587, q * 3), (0, q),
            (784, q), (880, q), (1047, q * 2), (880, q), (784, q), (659, q * 2),
            (587, q), (659, q), (523, q), (440, q * 4));
        int beats = melody.Length / (int)(q * 2 * Synth.SampleRate);
        var drum = new List<short[]>();
        for (int i = 0; i < beats; i++)
            drum.Add(Synth.Concat(Synth.Mix(Synth.Sweep(0.08, 140, 60, 0.4, 0), Synth.Noise(0.05, 0.15, 0, 10)),
                Synth.Silence(q * 2 - 0.08)));
        return Synth.Mix(melody, Synth.Concat(drum.ToArray()));
    }

    /// <summary>Plays the title tune once (restarting it if it is already playing).</summary>
    public void PlayTune()
    {
        if (!_enabled || Muted || Volume <= 0f || _tune == null)
            return;
        try
        {
            _tuneInstance?.Stop();
            _tuneInstance ??= _tune.CreateInstance();
            _tuneInstance.Volume = Volume;
            _tuneInstance.Play();
        }
        catch (Exception)
        {
        }
    }

    public void StopTune()
    {
        try
        {
            _tuneInstance?.Stop();
        }
        catch (Exception)
        {
        }
    }

    public void Dispose()
    {
        _tuneInstance?.Dispose();
        _tune?.Dispose();
        foreach (var e in _effects.Values)
            e.Dispose();
        _effects.Clear();
    }
}
