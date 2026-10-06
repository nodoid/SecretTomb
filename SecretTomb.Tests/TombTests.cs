using System.Linq;
using Microsoft.Xna.Framework;
using SecretTomb.Core.World;

namespace SecretTomb.Tests;

public class TombTests
{
    private readonly Tomb _tomb = Tomb.Load();

    [Fact]
    public void TheMapLoads()
    {
        Assert.Equal(60, _tomb.Width);
        Assert.Equal(48, _tomb.Height);
        Assert.Equal(Tile.Grass, _tomb[_tomb.Start]);
        Assert.Equal(6, _tomb.OutsideRows);
    }

    [Fact]
    public void TheTombHasEverythingTheQuestNeeds()
    {
        Assert.Single(_tomb.Items, i => i.Kind == ItemKind.Pistol);
        Assert.Single(_tomb.Items, i => i.Kind == ItemKind.JadeKey);
        Assert.Single(_tomb.Items, i => i.Kind == ItemKind.Book);
        Assert.Single(_tomb.Items, i => i.Kind == ItemKind.Helmet);
        Assert.Equal(2, _tomb.Items.Count(i => i.Kind == ItemKind.Idol));
        Assert.Single(_tomb.Teleporters);
        Assert.Single(_tomb.Carts);
        Assert.Single(_tomb.Boulders);
        int sarcophagi = 0, gateways = 0, rosetta = 0;
        for (int y = 0; y < _tomb.Height; y++)
            for (int x = 0; x < _tomb.Width; x++)
            {
                sarcophagi += _tomb[x, y] == Tile.Sarcophagus ? 1 : 0;
                gateways += _tomb[x, y] == Tile.Gateway ? 1 : 0;
                rosetta += _tomb[x, y] == Tile.Rosetta ? 1 : 0;
            }
        Assert.Equal(1, sarcophagi);
        Assert.Equal(1, gateways);
        Assert.Equal(3, rosetta);
    }

    [Fact]
    public void EveryTabletHasAMessage()
    {
        for (int y = 0; y < _tomb.Height; y++)
            for (int x = 0; x < _tomb.Width; x++)
                if (_tomb[x, y] == Tile.Tablet)
                    Assert.InRange(_tomb.Param(x, y), 1, 5);
    }

    [Fact]
    public void TheCartRunsAcrossTheChasm()
    {
        var line = _tomb.RailLine(_tomb.Carts[0]);
        Assert.Equal(8, line.Count);
        var first = line[0];
        var last = line[^1];
        Assert.False(Tomb.BlocksWalker(_tomb[first.X, first.Y - 1], false));
        Assert.False(Tomb.BlocksWalker(_tomb[last.X, last.Y + 1], false));
    }

    [Fact]
    public void MonstersStandOnGroundTheyCanWalk()
    {
        foreach (var m in _tomb.Monsters)
        {
            if (m.Kind == MonsterKind.Fish)
                Assert.Equal(Tile.Water, _tomb[m.Cell]);
            else
                Assert.True(Tomb.WalkableForMonster(_tomb[m.Cell], false), $"{m.Kind} at {m.Cell}");
        }
    }

    [Fact]
    public void TheMapIsWalledIn()
    {
        for (int x = 0; x < _tomb.Width; x++)
        {
            Assert.True(Tomb.BlocksWalker(_tomb[x, 0], false));
            Assert.True(Tomb.BlocksWalker(_tomb[x, _tomb.Height - 1], false));
        }
        for (int y = 0; y < _tomb.Height; y++)
        {
            Assert.True(Tomb.BlocksWalker(_tomb[0, y], false));
            Assert.True(Tomb.BlocksWalker(_tomb[_tomb.Width - 1, y], false));
        }
    }

    [Fact]
    public void CellsAndCentresRoundTrip()
    {
        var c = new Point(7, 11);
        Assert.Equal(c, Tomb.CellOf(Tomb.Centre(c)));
    }
}
