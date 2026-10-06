using System;
using System.IO;
using SecretTomb.Core;
using SecretTomb.Core.Engine;
using SecretTomb.Core.Localization;
using SecretTomb.Core.Session;
using SecretTomb.Core.World;

namespace SecretTomb.Tests;

public class SessionTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "goldeneagle-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    [Fact]
    public void High_scores_are_kept_in_order_and_saved()
    {
        var t = new HighScoreTable(_dir);
        Assert.Equal(HighScoreTable.Capacity, t.Entries.Count);
        int rank = t.Add(99999, "eagle!");
        Assert.Equal(0, rank);
        Assert.Equal("EAGLE", t.Entries[0].Name);
        Assert.Equal(-1, t.Add(1, "LOSER"));
        t.Save();

        var u = new HighScoreTable(_dir);
        u.Load();
        Assert.Equal(99999, u.Best);
        Assert.Equal(HighScoreTable.Capacity, u.Entries.Count);
    }

    [Fact]
    public void A_corrupt_score_file_falls_back_to_defaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "halloffame.txt"), "garbage\n\t\t\n");
        var t = new HighScoreTable(_dir);
        t.Load();
        Assert.Equal(HighScoreTable.Capacity, t.Entries.Count);
    }

    [Fact]
    public void Settings_remember_the_language_and_volume()
    {
        var s = GameSettings.Load(_dir, "en");
        Assert.Equal(Language.English, s.Language);
        s.Language = Language.French;
        s.Volume = 2;
        s.Save();
        var t = GameSettings.Load(_dir, "en");
        Assert.Equal(Language.French, t.Language);
        Assert.Equal(2, t.Volume);
    }

    [Fact]
    public void The_first_run_uses_the_device_language()
    {
        Assert.Equal(Language.French, GameSettings.Load(_dir, "fr").Language);
        Assert.Equal(Language.English, GameSettings.Load(_dir, "ja").Language);
    }

    [Fact]
    public void High_scores_carry_over_to_the_next_session()
    {
        var first = new HighScoreTable(_dir);
        first.Load();
        int rank = first.Add(99999, "");
        first.Save();
        first.Rename(rank, "INDY");
        first.Save();
        Assert.False(File.Exists(Path.Combine(_dir, "halloffame.txt.tmp")));

        var next = new HighScoreTable(_dir);
        next.Load();
        Assert.Equal(99999, next.Best);
        Assert.Equal("INDY", next.Entries[0].Name);
    }

    [Fact]
    public void The_difficulty_is_remembered()
    {
        var s = GameSettings.Load(_dir, "en");
        s.Difficulty = SecretTomb.Core.Engine.Difficulty.Hard;
        s.Save();
        Assert.Equal(SecretTomb.Core.Engine.Difficulty.Hard, GameSettings.Load(_dir, "en").Difficulty);
    }

    [Fact]
    public void Pathing_steps_round_a_wall()
    {
        var wall = new Microsoft.Xna.Framework.Point(1, 0);
        var step = Pathing.StepTowards(new(0, 0), new(2, 0), p => p != wall && p.X >= 0 && p.Y >= 0 && p.X < 3 && p.Y < 3);
        Assert.Equal(new Microsoft.Xna.Framework.Point(0, 1), step);
    }
}
