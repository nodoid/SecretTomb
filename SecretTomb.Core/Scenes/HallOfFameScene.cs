using System;
using SecretTomb.Core.Audio;
using SecretTomb.Core.Engine;
using SecretTomb.Core.Graphics;
using SecretTomb.Core.Localization;
using SecretTomb.Core.Oric;
using SecretTomb.Core.Session;

namespace SecretTomb.Core.Scenes;

/// <summary>The Hall of Fame, with arcade style name entry after a qualifying game.</summary>
public sealed class HallOfFameScene : Scene
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789. <";
    private const char EndMark = '<';
    private const int NameY = 100;
    private readonly Adventure _finished;
    private bool _entering;
    private char[] _name;
    private int _cursor;
    private int _letter;
    private int _rank = -1;

    public HallOfFameScene(SecretTombGame game, Adventure finished) : base(game)
    {
        _finished = finished;
    }

    public bool Entering => _entering;

    public override void Enter()
    {
        if (_finished != null && Game.HighScores.Qualifies(_finished.Score))
        {
            _entering = true;
            _name = new string(' ', HighScoreTable.NameLength).ToCharArray();
            // Saved straight away (named "?????" until the name is entered), so the score is kept
            // even if the app is closed or killed during name entry.
            _rank = Game.HighScores.Add(_finished.Score, "");
            Game.HighScores.Save();
        }
    }

    public override void Leave()
    {
        if (_entering)
            Finish();
    }

    protected override void Update()
    {
        var input = Game.Input;
        // (L is a letter of the name while it is being typed.)
        if (input.LanguagePressed && !_entering)
            Game.ToggleLanguage();
        if (_entering)
        {
            UpdateEntry();
            return;
        }
        if (input.BackPressed)
            Game.ShowMenu();
        else if (Ticks > 25 && (input.ConfirmPressed || input.Taps.Count > 0))
            Game.StartNewGame();
        else if (Ticks > 600)
            Game.ShowTitle();
    }

    private void UpdateEntry()
    {
        var input = Game.Input;
        if (Ticks < 30)
            return;

        foreach (char ch in input.Typed)
        {
            if (ch == '\b')
            {
                if (_cursor > 0)
                    _name[--_cursor] = ' ';
            }
            else if (_cursor < HighScoreTable.NameLength)
            {
                _name[_cursor++] = ch;
            }
        }

        int step = 0;
        if (input.LeftPressed || input.UpPressed) step = -1;
        if (input.RightPressed || input.DownPressed) step = 1;
        bool accept = input.ConfirmPressed && input.Typed.Count == 0;

        // Touch: the left and right thirds change the letter, the middle accepts it.
        foreach (var tap in input.Taps)
        {
            if (tap.X < Canvas.Width / 3f) step = -1;
            else if (tap.X > Canvas.Width * 2 / 3f) step = 1;
            else accept = true;
        }

        if (step != 0)
        {
            _letter = (_letter + step + Alphabet.Length) % Alphabet.Length;
            Game.Sound.Play(Sfx.Menu);
        }

        if (accept && _cursor < HighScoreTable.NameLength)
        {
            char ch = Alphabet[_letter];
            if (ch == EndMark)
            {
                Finish();
                return;
            }
            _name[_cursor++] = ch;
            Game.Sound.Play(Sfx.Menu);
        }
        if (_cursor >= HighScoreTable.NameLength || input.BackPressed)
            Finish();
    }

    private void Finish()
    {
        _entering = false;
        Game.HighScores.Rename(_rank, new string(_name));
        Game.HighScores.Save();
        Game.Sound.Play(Sfx.Treasure);
    }

    public override void Draw(Canvas c)
    {
        Backdrop.Draw(c, Ticks, 0.35f);
        Backdrop.Panel(c, 8, 20, Canvas.Width - 16, 176, 0.75f);
        c.TextCenteredShadowed(2, Strings.HallOfFame, new Microsoft.Xna.Framework.Color(255, 214, 90), 2);
        if (_entering)
        {
            c.TextCentered(40, Strings.YourScore + " " + _finished.Score.ToString("D6"), OricColor.Yellow);
            c.TextCentered(70, Strings.EnterName, OricColor.Cyan);
            int x0 = (Canvas.Width - HighScoreTable.NameLength * 12) / 2;
            for (int i = 0; i < HighScoreTable.NameLength; i++)
            {
                char ch = i < _cursor ? _name[i] : (i == _cursor ? Alphabet[_letter] : '-');
                bool blink = i == _cursor && Ticks / 10 % 2 == 0;
                if (blink)
                    c.Fill(x0 + i * 12 - 1, NameY - 1, 8, 10, OricColor.Yellow);
                c.Text(x0 + i * 12, NameY, ch.ToString(), blink ? OricColor.Blue : OricColor.White);
            }
            if (Game.IsMobile)
            {
                c.Text(24, NameY, "<", OricColor.Yellow, 2);
                c.Text(204, NameY, ">", OricColor.Yellow, 2);
            }
            c.TextCentered(136, Strings.ChangeLetter(Game.IsMobile), OricColor.Yellow);
            c.TextCentered(148, Strings.AcceptLetter(Game.IsMobile), OricColor.Yellow);
            c.TextCentered(160, Strings.EndMarkHint, OricColor.Yellow);
            return;
        }

        c.TextAt(8, 24, Strings.Score, OricColor.Cyan);
        c.TextAt(24, 24, Strings.NameHeader, OricColor.Cyan);
        var entries = Game.HighScores.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            var ink = i == _rank && Ticks / 8 % 2 == 0 ? OricColor.White : OricColor.Yellow;
            c.TextAt(4, 38 + i * 15, $"{i + 1,2}", OricColor.Green);
            c.TextAt(8, 38 + i * 15, entries[i].Score.ToString("D6"), ink);
            c.TextAt(24, 38 + i * 15, entries[i].Name, ink);
        }
        c.TextCentered(200, Strings.PlayOrMenu(Game.IsMobile), OricColor.Cyan);
        c.TextCentered(212, Strings.Credit + " - " + Strings.Subtitle, OricColor.White);
    }
}
