using System;
using Microsoft.Xna.Framework;
using SecretTomb.Core.Audio;
using SecretTomb.Core.Graphics;
using SecretTomb.Core.Localization;

namespace SecretTomb.Core.Scenes;

/// <summary>
/// The title: the pyramid at dusk, the name, the credits, and the two language buttons
/// (ENGLISH / FRANÇAIS) to switch between English and French.
/// </summary>
public sealed class TitleScene : Scene
{
    private const int DemoAfter = 20 * 50;
    private const float ButtonY = 162;
    private const float ButtonW = 66;
    private const float ButtonH = 13;
    private int _idle;

    public TitleScene(SecretTombGame game) : base(game)
    {
    }

    public static RectF EnglishButton => new(Canvas.Width / 2f - ButtonW - 5, ButtonY, ButtonW, ButtonH);
    public static RectF FrenchButton => new(Canvas.Width / 2f + 5, ButtonY, ButtonW, ButtonH);

    public override void Enter() => Game.Sound.PlayTune();

    public override void Leave() => Game.Sound.StopTune();

    protected override void Update()
    {
        var input = Game.Input;
        _idle++;
        if (input.LanguagePressed || input.LeftPressed || input.RightPressed)
        {
            Game.ToggleLanguage();
            Game.Sound.Play(Sfx.Menu);
            _idle = 0;
        }
        if (input.BackPressed)
        {
            Game.Quit();
            return;
        }
        foreach (var tap in input.Taps)
        {
            _idle = 0;
            if (EnglishButton.Inflate(3).Contains(tap))
            {
                SetLanguage(Language.English);
                return;
            }
            if (FrenchButton.Inflate(3).Contains(tap))
            {
                SetLanguage(Language.French);
                return;
            }
            if (Ticks > 10)
            {
                Game.ShowMenu();
                return;
            }
        }
        if (Ticks > 10 && input.ConfirmPressed)
            Game.ShowMenu();
        else if (_idle > DemoAfter)
            Game.ShowDemo();
    }

    private void SetLanguage(Language language)
    {
        if (Strings.Current != language)
            Game.ToggleLanguage();
        Game.Sound.Play(Sfx.Menu);
    }

    public override void Draw(Canvas c)
    {
        Backdrop.Draw(c, Ticks);
        DrawName(c, 58, Ticks);
        float pulse = 0.6f + 0.4f * MathF.Sin(Ticks * 0.12f);
        string start = Strings.PressToStart(Game.IsMobile);
        c.TextCenteredShadowed(132, start, Color.Lerp(new Color(255, 214, 90), Color.White, pulse));

        // Language buttons: the current language is lit.
        DrawButton(c, EnglishButton, Strings.English, Strings.Current == Language.English);
        DrawButton(c, FrenchButton, Strings.French, Strings.Current == Language.French);
        c.TextCenteredShadowed(150, Strings.LanguageHint(Game.IsMobile), new Color(170, 200, 230));

        c.TextCenteredShadowed(190, Strings.Credit, Color.White);
        c.TextCenteredShadowed(201, Strings.BasedOn, new Color(170, 230, 170));
        c.TextCenteredShadowed(211, Strings.Authors, new Color(170, 230, 170));
    }

    private void DrawButton(Canvas c, RectF r, string text, bool on)
    {
        bool hover = Game.Input.Pointer is { } p && r.Contains(p);
        c.Fill(r.X, r.Y, r.W, r.H, on ? new Color(240, 196, 80) : new Color(20, 16, 40) * 0.8f);
        c.Frame(r.X, r.Y, r.W, r.H, hover || on ? Color.White : new Color(190, 140, 50), 0.7f);
        c.Text(r.X + (r.W - Canvas.TextWidth(text)) / 2, r.Y + 3, text, on ? new Color(40, 20, 60) : Color.White);
    }

    /// <summary>The game's name in two lines of gold, with a glint running across it.</summary>
    public static void DrawName(Canvas c, float y, int ticks)
    {
        var gold = new Color(255, 214, 90);
        c.Fill(20, y - 6, Canvas.Width - 40, 62, Color.Black * 0.35f);
        c.TextCenteredShadowed(y, Strings.TitleLine1, gold, 2);
        c.TextCenteredShadowed(y + 18, Strings.TitleLine2, gold, 2);
        c.TextCenteredShadowed(y + 40, Strings.Subtitle, new Color(150, 220, 255));
        float sweep = ticks * 2.2f % 360 - 60;
        c.Glow(sweep, y + 16, 12, Color.White * 0.3f);
    }
}
