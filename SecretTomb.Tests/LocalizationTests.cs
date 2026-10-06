using System.Collections.Generic;
using System.Linq;
using SecretTomb.Core.Engine;
using SecretTomb.Core.Graphics;
using SecretTomb.Core.Localization;
using SecretTomb.Core.Oric;

namespace SecretTomb.Tests;

public class LocalizationTests
{
    private static IEnumerable<string> Everything()
    {
        yield return Strings.TitleLine1;
        yield return Strings.TitleLine2;
        yield return Strings.Title;
        yield return Strings.Subtitle;
        yield return Strings.Credit;
        yield return Strings.BasedOn;
        yield return Strings.Authors;
        yield return Strings.English;
        yield return Strings.French;
        foreach (bool m in new[] { false, true })
        {
            yield return Strings.PressToStart(m);
            yield return Strings.LanguageHint(m);
            yield return Strings.Resume(m);
            yield return Strings.BackToMenu(m);
            yield return Strings.CloseReading(m);
            yield return Strings.NextPage(m);
            yield return Strings.PlayOrMenu(m);
            yield return Strings.ChangeLetter(m);
            yield return Strings.AcceptLetter(m);
            foreach (var (k, v) in Strings.Controls(m))
            {
                yield return k;
                yield return v;
            }
        }
        yield return Strings.Play;
        yield return Strings.Instructions;
        yield return Strings.HallOfFame;
        yield return Strings.LanguageItem;
        yield return Strings.Volume + " : " + Strings.VolumeName(2);
        yield return Strings.DifficultyItem + " : " + Strings.DifficultyName(Difficulty.Normal);
        foreach (Difficulty d in System.Enum.GetValues<Difficulty>())
            yield return Strings.DifficultyName(d);
        foreach (string s in Strings.Story.Concat(Strings.Rules).Concat(Strings.Secrets))
            yield return s;
        for (int i = 0; i <= 5; i++)
            yield return Strings.Inscription(i);
        yield return Strings.Victory;
        yield return Strings.VictoryText1;
        yield return Strings.VictoryText2;
        yield return Strings.GameOver;
        yield return Strings.LostText;
        yield return Strings.TreasureLine(12, 12);
        yield return Strings.SlainLine(20);
    }

    /// <summary>Lines drawn on their own: they must fit the 40-column screen.</summary>
    private static IEnumerable<string> ScreenLines()
    {
        foreach (var s in Everything())
            if (!s.Contains("INSCRIPTION") && s.Length > 0)
                yield return s;
    }

    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.French)]
    public void EveryCharacterCanBeDrawn(Language language)
    {
        var saved = Strings.Current;
        Strings.Current = language;
        try
        {
            foreach (var s in Everything())
                foreach (char ch in s)
                    Assert.True(OricFont.CanDraw(ch), $"'{ch}' in \"{s}\"");
            foreach (DeathCause cause in System.Enum.GetValues<DeathCause>())
                foreach (char ch in Strings.DeathText(cause))
                    Assert.True(OricFont.CanDraw(ch), $"'{ch}' in death text {cause}");
        }
        finally
        {
            Strings.Current = saved;
        }
    }

    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.French)]
    public void TextFitsTheScreen(Language language)
    {
        var saved = Strings.Current;
        Strings.Current = language;
        try
        {
            foreach (var s in Everything().Where(s => !Strings.Story.Contains(s) || true))
                if (s != Strings.Inscription(3) && s != Strings.Inscription(2) && s != Strings.Inscription(5))
                    Assert.True(s.Length <= Canvas.Columns, $"too long ({s.Length}): {s}");
            Assert.True(Strings.TitleLine1.Length * 12 <= Canvas.Width);
            Assert.True(Strings.TitleLine2.Length * 12 <= Canvas.Width);
        }
        finally
        {
            Strings.Current = saved;
        }
    }

    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.French)]
    public void MessagesFitTheirBanner(Language language)
    {
        var saved = Strings.Current;
        Strings.Current = language;
        try
        {
            foreach (DeathCause cause in System.Enum.GetValues<DeathCause>())
                Assert.True(TombRenderer.Wrap(Strings.DeathText(cause), 38).Count <= 2, Strings.DeathText(cause));
            foreach (var s in new[] { Strings.FoundPistol, Strings.FoundHelmet, Strings.StoneTaken, Strings.PortcullisStuck })
                Assert.True(TombRenderer.Wrap(s, 38).Count <= 2, s);
        }
        finally
        {
            Strings.Current = saved;
        }
    }

    [Fact]
    public void LanguagesDiffer()
    {
        Strings.Current = Language.French;
        string fr = Strings.Title;
        Strings.Current = Language.English;
        Assert.NotEqual(fr, Strings.Title);
        Assert.Equal(Language.French, Strings.FromIso("fr"));
        Assert.Equal(Language.English, Strings.FromIso("de"));
    }
}
