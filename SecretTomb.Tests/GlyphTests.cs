using System.Collections.Generic;
using SecretTomb.Core.Graphics;

namespace SecretTomb.Tests;

public class GlyphTests
{
    [Fact]
    public void EveryLetterHasItsOwnSign()
    {
        var seen = new HashSet<string>();
        for (int i = 0; i < Glyphs.Count; i++)
            Assert.True(seen.Add(string.Join(",", Glyphs.Rows(i).ToArray())), $"sign {i} repeats");
    }

    [Fact]
    public void AccentsAreIgnored()
    {
        Assert.Equal(Glyphs.IndexOf('E'), Glyphs.IndexOf('É'));
        Assert.Equal(Glyphs.IndexOf('A'), Glyphs.IndexOf('à'));
        Assert.Equal(26, Glyphs.IndexOf(Glyphs.Skull));
        Assert.Equal(-1, Glyphs.IndexOf(' '));
    }
}
