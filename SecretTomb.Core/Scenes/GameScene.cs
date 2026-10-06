using SecretTomb.Core.Engine;
using SecretTomb.Core.Graphics;

namespace SecretTomb.Core.Scenes;

/// <summary>Play: the controls drive the adventure, which is drawn by <see cref="TombRenderer"/>.</summary>
public sealed class GameScene : Scene, IPausable
{
    private readonly Adventure _adventure;
    private int _endTicks;

    public GameScene(SecretTombGame game, Adventure adventure) : base(game)
    {
        _adventure = adventure;
    }

    public override void Enter() => Game.Input.PlayControls = true;

    public override void Leave() => Game.Input.PlayControls = false;

    protected override void Update()
    {
        var input = Game.Input;
        if (input.BackPressed && _adventure.Reading == null)
        {
            Game.ShowMenu();
            return;
        }
        var controls = input.Game;
        // On a touch screen, tapping anywhere closes an inscription.
        if (_adventure.Reading != null && input.Taps.Count > 0)
            controls.Action = true;
        _adventure.Tick(controls);
        if (_adventure.Outcome != Outcome.Playing && ++_endTicks > 60)
            Game.GameEnded(_adventure);
    }

    public override void Draw(Canvas c) => TombRenderer.Draw(c, _adventure, Ticks, Game.IsMobile);
}
