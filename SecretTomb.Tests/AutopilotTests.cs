using System;
using SecretTomb.Core.Audio;
using SecretTomb.Core.Engine;
using Xunit.Abstractions;

namespace SecretTomb.Tests;

/// <summary>The autopilot (used by the demo and the store captures) plays the whole tomb.</summary>
public class AutopilotTests
{
    private readonly ITestOutputHelper _out;

    public AutopilotTests(ITestOutputHelper output)
    {
        _out = output;
    }

    [Fact]
    public void AutopilotEscapesWithTheStone()
    {
        var a = new Adventure(SilentSoundPlayer.Instance) { GodMode = true };
        var pilot = new Autopilot(a);
        int lastGoal = -1, since = 0;
        for (int i = 0; i < 60000 && a.Outcome == Outcome.Playing; i++)
        {
            a.Tick(pilot.Next());
            if (pilot.GoalIndex != lastGoal)
            {
                _out.WriteLine($"tick {a.Ticks}: goal {pilot.GoalIndex} {pilot.Current} at {a.Cell}");
                lastGoal = pilot.GoalIndex;
                since = 0;
            }
            else if (++since == 3000)
                _out.WriteLine($"stuck on {pilot.Current} at {a.Cell} pos {a.Pos} facing {a.Facing}");
        }
        _out.WriteLine($"outcome {a.Outcome} after {a.Seconds}s, score {a.Score}, slain {a.Slain}, treasure {a.TreasureFound}/{a.TotalTreasure}");
        Assert.Equal(Outcome.Won, a.Outcome);
        Assert.True(a.HasStone);
        Assert.Equal(2, a.Idols);
    }
}
