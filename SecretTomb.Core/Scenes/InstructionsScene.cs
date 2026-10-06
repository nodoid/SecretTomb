using Microsoft.Xna.Framework;
using SecretTomb.Core.Graphics;
using SecretTomb.Core.Localization;

namespace SecretTomb.Core.Scenes;

/// <summary>Four pages: the story, the dangers, the secrets and the controls.</summary>
public sealed class InstructionsScene : Scene
{
    public const int Pages = 4;
    private readonly int _page;

    public InstructionsScene(SecretTombGame game, int page = 0) : base(game)
    {
        _page = page;
    }

    protected override void Update()
    {
        var input = Game.Input;
        if (input.LanguagePressed)
            Game.ToggleLanguage();
        if (input.BackPressed)
        {
            Game.ShowMenu();
            return;
        }
        if (Ticks > 10 && (input.ConfirmPressed || input.Taps.Count > 0 || input.RightPressed))
        {
            if (_page + 1 < Pages)
                Game.SwitchTo(new InstructionsScene(Game, _page + 1));
            else
                Game.ShowMenu();
        }
        else if (Ticks > 10 && input.LeftPressed && _page > 0)
        {
            Game.SwitchTo(new InstructionsScene(Game, _page - 1));
        }
    }

    public override void Draw(Canvas c)
    {
        Backdrop.Draw(c, Ticks, 0.3f);
        Backdrop.Panel(c, 4, 4, Canvas.Width - 8, 192, 0.8f);
        c.TextCenteredShadowed(9, Strings.Title, new Color(255, 214, 90));
        switch (_page)
        {
            case 0:
                Header(c, Strings.StoryHeader);
                Lines(c, Strings.Story);
                break;
            case 1:
                Header(c, Strings.RulesHeader);
                Lines(c, Strings.Rules);
                break;
            case 2:
                Header(c, Strings.SecretsHeader);
                Lines(c, Strings.Secrets);
                break;
            default:
                Header(c, Strings.ControlsHeader);
                var controls = Strings.Controls(Game.IsMobile);
                for (int i = 0; i < controls.Length; i++)
                {
                    c.TextAt(2, 42 + i * 14, controls[i].key, new Color(255, 214, 90));
                    c.TextAt(19, 42 + i * 14, controls[i].action, Color.White);
                }
                break;
        }
        c.TextCenteredShadowed(200, Strings.Page(_page + 1, Pages), new Color(150, 220, 255));
        if (Ticks / 25 % 2 == 0)
            c.TextCenteredShadowed(212, Strings.NextPage(Game.IsMobile), new Color(255, 160, 220));
    }

    private static void Header(Canvas c, string text)
    {
        c.TextCentered(22, text, new Color(150, 220, 255));
        float w = text.Length * 6;
        c.Fill((Canvas.Width - w) / 2, 31, w, 0.8f, new Color(150, 220, 255));
    }

    private static void Lines(Canvas c, string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
            c.TextAt(1, 38 + i * 10, lines[i], Color.White);
    }
}
