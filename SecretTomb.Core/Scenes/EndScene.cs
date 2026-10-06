using System;
using Microsoft.Xna.Framework;
using SecretTomb.Core.Engine;
using SecretTomb.Core.Graphics;
using SecretTomb.Core.Localization;

namespace SecretTomb.Core.Scenes;

/// <summary>The end of a game: escape with the stone, or the tomb keeps its secret.</summary>
public sealed class EndScene : Scene
{
    private readonly Adventure _adventure;

    public EndScene(SecretTombGame game, Adventure adventure) : base(game)
    {
        _adventure = adventure;
    }

    public bool Won => _adventure.Outcome == Outcome.Won;

    protected override void Update()
    {
        var input = Game.Input;
        if (input.LanguagePressed)
            Game.ToggleLanguage();
        if (Ticks > 60 && (input.ConfirmPressed || input.Taps.Count > 0 || input.BackPressed) || Ticks > 750)
            Game.ShowHallOfFame(_adventure);
    }

    public override void Draw(Canvas c)
    {
        Backdrop.Draw(c, Ticks, Won ? 0.8f : 0.3f);
        Backdrop.Panel(c, 16, 8, Canvas.Width - 32, 190, 0.7f);
        if (Won)
        {
            c.TextCenteredShadowed(16, Strings.Victory, new Color(255, 214, 90), 2);
            float pulse = 0.8f + 0.2f * MathF.Sin(Ticks * 0.1f);
            c.Glow(120, 60, 26 * pulse, new Color(120, 255, 255) * 0.5f);
            c.Draw("Stone", new RectF(106, 46, 28, 28));
            c.TextCenteredShadowed(84, Strings.VictoryText1, Color.White);
            c.TextCenteredShadowed(94, Strings.VictoryText2, new Color(150, 255, 255));
        }
        else
        {
            c.TextCenteredShadowed(16, Strings.GameOver, new Color(255, 90, 70), 2);
            int frame = Ticks / 12 % 2;
            c.Draw("Ghoul" + frame, new RectF(78, 44, 33, 36));
            c.Draw("Guardian" + frame, new RectF(122, 40, 39, 42));
            c.TextCenteredShadowed(90, Strings.LostText, Color.White);
        }

        c.TextCentered(112, Strings.TreasureLine(_adventure.TreasureFound, _adventure.TotalTreasure), new Color(150, 220, 255));
        c.TextCentered(124, Strings.SlainLine(_adventure.Slain), new Color(150, 220, 255));
        c.TextCentered(136, Strings.TimeTaken(_adventure.Seconds), new Color(150, 220, 255));
        if (Won)
            c.TextCentered(148, Strings.BonusLine(_adventure.Bonus), new Color(150, 255, 150));
        c.TextCentered(166, Strings.YourScore + " " + _adventure.Score.ToString("D6"), new Color(255, 214, 90));
        if (Ticks > 60 && Ticks / 25 % 2 == 0)
            c.TextCenteredShadowed(206, Strings.NextPage(Game.IsMobile), new Color(255, 160, 220));
    }
}
