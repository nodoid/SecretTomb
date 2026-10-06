using System;
using SecretTomb.Core.Audio;
using SecretTomb.Core.Engine;
using SecretTomb.Core.Graphics;
using SecretTomb.Core.Scenes;

namespace SecretTomb.Core.Demo;

/// <summary>The tomb played by the <see cref="Autopilot"/>: the attract mode and the store captures.</summary>
public sealed class DemoScene : Scene
{
    private readonly Autopilot _pilot;
    private readonly bool _attract;

    public DemoScene(SecretTombGame game, ISoundPlayer sound, bool attract, int seed = 1985) : base(game)
    {
        Adventure = new Adventure(sound, seed) { GodMode = true };
        _pilot = new Autopilot(Adventure);
        _attract = attract;
    }

    public Adventure Adventure { get; }
    public Autopilot Pilot => _pilot;

    protected override void Update()
    {
        if (_attract)
        {
            var input = Game.Input;
            if (input.ConfirmPressed || input.Taps.Count > 0 || input.BackPressed || Ticks > 120 * 50 ||
                Adventure.Outcome != Outcome.Playing)
            {
                Game.ShowTitle();
                return;
            }
        }
        Adventure.Tick(_pilot.Next());
    }

    public override void Draw(Canvas c) => TombRenderer.Draw(c, Adventure, Ticks, Game.IsMobile, _attract);

    /// <summary>Runs the demo without drawing until <paramref name="ready"/> holds (store captures).</summary>
    public void FastForward(Func<Adventure, bool> ready, int limit = 200000)
    {
        for (int i = 0; i < limit && !ready(Adventure) && Adventure.Outcome == Outcome.Playing; i++)
            Tick();
    }
}
