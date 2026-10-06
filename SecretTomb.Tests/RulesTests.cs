using System.Linq;
using Microsoft.Xna.Framework;
using SecretTomb.Core.Audio;
using SecretTomb.Core.Engine;
using SecretTomb.Core.World;

namespace SecretTomb.Tests;

/// <summary>The rules, on small hand-made tombs.</summary>
public class RulesTests
{
    private static Adventure Make(Difficulty difficulty, params string[] rows) =>
        new(SilentSoundPlayer.Instance, Tomb.Parse(string.Join("\n", rows)), 1, difficulty);

    private static Adventure Make(params string[] rows) => Make(Difficulty.Normal, rows);

    private static void Run(Adventure a, Controls c, int ticks)
    {
        for (int i = 0; i < ticks; i++)
            a.Tick(c);
    }

    private static Controls Move(Dir d) => new() { Move = d.Vector() };

    [Fact]
    public void WalkingIntoAPitIsFatal()
    {
        var a = Make("#######", "#@.^..#", "#######");
        Run(a, Move(Dir.Right), 60);
        Assert.Equal(DeathCause.Pit, a.Dying);
        Assert.Equal(2, a.Lives);
    }

    [Fact]
    public void JumpingClearsAPit()
    {
        var a = Make("########", "#.@^...#", "########");
        a.Tick(Move(Dir.Right) with { Move = new Vector2(0.21f, 0) });
        a.Tick(new Controls { Jump = true });
        Run(a, new Controls(), 40);
        Assert.Equal(DeathCause.None, a.Dying);
        Assert.Equal(new Point(4, 1), a.Cell);
    }

    [Fact]
    public void WallsStopTheExplorer()
    {
        var a = Make("#####", "#@..#", "#####");
        Run(a, Move(Dir.Right), 200);
        Assert.Equal(new Point(3, 1), a.Cell);
        Assert.True(a.Alive);
    }

    [Fact]
    public void HoldingYourBreathRunsOut()
    {
        var a = Make("##############", "#@~~~~~~~~~~~#", "##############");
        Run(a, Move(Dir.Right), 40);
        Assert.True(a.Swimming);
        Run(a, new Controls(), 200);
        Assert.Equal(DeathCause.Drowned, a.Dying);
    }

    [Fact]
    public void TheDivingHelmetLetsYouBreathe()
    {
        var a = Make("##############", "#@h~~~~~~~~~~#", "##############");
        Run(a, Move(Dir.Right), 60);
        Assert.True(a.HasHelmet);
        Run(a, new Controls(), 600);
        Assert.True(a.Alive);
        Assert.Equal(Adventure.MaxAir, a.Air);
    }

    [Fact]
    public void ThePistolComesWithShots()
    {
        var a = Make("#####", "#@p.#", "#####");
        Run(a, Move(Dir.Right), 30);
        Assert.True(a.HasPistol);
        Assert.Equal(a.PistolShots, a.Ammo);
    }

    [Fact]
    public void TheLaserKillsAGhoulAndLeavesItsBones()
    {
        var a = Make("##########", "#@p.....m#", "##########");
        Run(a, Move(Dir.Right), 25);
        a.Tick(new Controls { Aim = Dir.Right });
        Run(a, new Controls(), 40);
        Assert.False(a.Monsters[0].Alive);
        Assert.Equal(1, a.Slain);
        Assert.Contains(Enumerable.Range(1, 8), x => a.Tomb[x, 1] == Tile.Skeleton && x > 3);
    }

    [Fact]
    public void GuardiansCannotBeHarmed()
    {
        var a = Make("##############", "#@p.........G#", "##############");
        Run(a, Move(Dir.Right), 25);
        a.Tick(new Controls { Aim = Dir.Right });
        Run(a, new Controls(), 20);
        Assert.True(a.Monsters[0].Alive);
    }

    [Fact]
    public void AGhoulsTouchIsDeadly()
    {
        var a = Make("#######", "#@...m#", "#######");
        Run(a, Move(Dir.Right), 30);
        Run(a, new Controls(), 100);
        Assert.Equal(DeathCause.Ghoul, a.Dying);
    }

    [Fact]
    public void MonstersWillNotCrossBones()
    {
        var a = Make("#########", "#@..x..m#", "#########");
        Run(a, new Controls(), 400);
        Assert.True(a.Alive);
        Assert.True(a.Monsters[0].Pos.X > 4.5f * Tomb.TileSize);
    }

    [Fact]
    public void ShootingAChestDestroysIt()
    {
        var a = Make("#########", "#@p...$.#", "#########");
        Run(a, Move(Dir.Right), 25);
        a.Tick(new Controls { Aim = Dir.Right });
        Run(a, new Controls(), 30);
        var chest = a.Items.Single(i => i.Kind == ItemKind.Chest);
        Assert.True(chest.Destroyed);
    }

    [Fact]
    public void DoorsOpenAndClose()
    {
        var a = Make("######", "#@D..#", "######");
        a.Tick(new Controls { Move = new Vector2(0.21f, 0) });
        a.Tick(new Controls { Action = true });
        Assert.Equal(Tile.DoorOpen, a.Tomb[2, 1]);
        a.Tick(new Controls());
        a.Tick(new Controls { Action = true });
        Assert.Equal(Tile.Door, a.Tomb[2, 1]);
    }

    [Fact]
    public void TheJadeDoorNeedsItsKey()
    {
        var a = Make("#######", "#@kJ..#", "#######");
        a.Tick(new Controls { Move = new Vector2(0.21f, 0) });
        Assert.False(a.HasKey);
        Run(a, Move(Dir.Right), 30);
        Assert.True(a.HasKey);
        a.Tick(new Controls { Action = true });
        Assert.Equal(Tile.DoorOpen, a.Tomb[3, 1]);
    }

    [Fact]
    public void TheTwinGateNeedsBothIdols()
    {
        var a = Make("########", "#@i.W..#", "########");
        Run(a, Move(Dir.Right), 60);
        Assert.Equal(1, a.Idols);
        a.Tick(new Controls { Action = true });
        Assert.Equal(Tile.TwinGate, a.Tomb[4, 1]);
    }

    [Fact]
    public void SecretDoorsOpenWhenSearched()
    {
        var a = Make("######", "#@%..#", "######");
        a.Tick(new Controls { Move = new Vector2(0.21f, 0) });
        a.Tick(new Controls { Action = true });
        Assert.Equal(Tile.Floor, a.Tomb[2, 1]);
        Assert.Equal(1, a.SecretsFound);
    }

    [Fact]
    public void TheFountainOfYouthIsFatal()
    {
        var a = Make("#####", "#@F.#", "#####");
        a.Tick(new Controls { Move = new Vector2(0.21f, 0) });
        a.Tick(new Controls { Action = true });
        Assert.Equal(DeathCause.Youth, a.Dying);
    }

    [Fact]
    public void TheScriptCanOnlyBeReadAfterTheGreatInscription()
    {
        var a = Make("#R##1#", "#@...#", "######");
        a.Tick(new Controls { Move = new Vector2(0, -0.21f) });
        a.Tick(new Controls { Action = true });
        Assert.Equal(0, a.Reading);
        Assert.True(a.KnowsScript);
    }

    [Fact]
    public void TheCartCrossesTheChasm()
    {
        var a = Make("#####", "##@##", "##c##", "##=##", "##=##", "##.##", "#####");
        a.Tick(new Controls { Jump = true });
        Assert.NotNull(a.Riding);
        Run(a, new Controls(), 100);
        Assert.Null(a.Riding);
        Assert.Equal(new Point(2, 5), a.Cell);
        Assert.True(a.Alive);
    }

    [Fact]
    public void EscapingWithTheStoneWins()
    {
        var a = Make("#,,,#", "##E##", "##.##", "##@##", "##X##", "#####");
        a.Tick(new Controls { Move = new Vector2(0, 0.21f) });
        a.Tick(new Controls { Action = true });
        Assert.True(a.HasStone);
        Run(a, Move(Dir.Up), 80);
        Assert.Equal(Outcome.Won, a.Outcome);
        Assert.True(a.Bonus > 0);
    }

    [Fact]
    public void LosingTheLastLifeEndsTheGame()
    {
        var a = Make(Difficulty.Hard, "#######", "#@.^..#", "#######");
        Assert.Equal(1, a.Lives);
        Run(a, Move(Dir.Right), 60);
        Run(a, new Controls(), Adventure.DeathPause + 5);
        Assert.Equal(Outcome.Dead, a.Outcome);
    }

    [Fact]
    public void AfterDeathTheExplorerReturnsToTheSunStone()
    {
        var a = Make("########", "#@o..^.#", "########");
        Run(a, Move(Dir.Right), 120);
        Assert.Equal(DeathCause.Pit, a.Dying);
        Run(a, new Controls(), Adventure.DeathPause + 5);
        Assert.True(a.Alive);
        Assert.Equal(new Point(2, 1), a.Cell);
    }

    [Theory]
    [InlineData(Difficulty.Easy, 5)]
    [InlineData(Difficulty.Normal, 3)]
    [InlineData(Difficulty.Hard, 1)]
    public void DifficultySetsTheLives(Difficulty difficulty, int lives)
    {
        var a = Make(difficulty, "####", "#@.#", "####");
        Assert.Equal(lives, a.Lives);
        Assert.Equal(lives, a.StartLives);
    }

    [Fact]
    public void HarderGamesAreHarder()
    {
        var easy = Make(Difficulty.Easy, "####", "#@.#", "####");
        var hard = Make(Difficulty.Hard, "####", "#@.#", "####");
        Assert.True(easy.AirLoss < hard.AirLoss);
        Assert.True(easy.MonsterSpeed < hard.MonsterSpeed);
        Assert.True(easy.PistolShots > hard.PistolShots);
        Assert.True(easy.DartPeriod > hard.DartPeriod);
    }
}
