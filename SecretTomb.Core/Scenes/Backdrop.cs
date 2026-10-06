using System;
using Microsoft.Xna.Framework;
using SecretTomb.Core.Graphics;

namespace SecretTomb.Core.Scenes;

/// <summary>The pyramid in the jungle at dusk, behind the title and the menus, with fireflies.</summary>
public static class Backdrop
{
    private static readonly Vector2[] Flies = MakeFlies();

    private static Vector2[] MakeFlies()
    {
        var rnd = new Random(1985);
        var flies = new Vector2[26];
        for (int i = 0; i < flies.Length; i++)
            flies[i] = new Vector2((float)rnd.NextDouble() * Canvas.Width, 120 + (float)rnd.NextDouble() * 100);
        return flies;
    }

    public static void Draw(Canvas c, int ticks, float dim = 1f)
    {
        c.Fill(0, 0, Canvas.Width, Canvas.Height, new Color(8, 18, 12));
        c.Draw("Title", new RectF(0, 0, Canvas.Width, 150), Color.Lerp(Color.Black, Color.White, dim));
        c.Gradient(0, 140, Canvas.Width, 84, new Color(12, 34, 20) * dim, new Color(4, 10, 6));
        float pulse = 0.75f + 0.25f * MathF.Sin(ticks * 0.05f);
        c.Glow(120, 38, 26, new Color(255, 210, 120) * (0.25f * pulse * dim));
        for (int i = 0; i < Flies.Length; i++)
        {
            var f = Flies[i];
            float x = f.X + MathF.Sin(ticks * 0.013f + i) * 14;
            float y = f.Y + MathF.Sin(ticks * 0.021f + i * 2.1f) * 6;
            float blink = MathF.Max(0, MathF.Sin(ticks * 0.06f + i * 1.3f));
            c.Glow(x, y, 2.2f, new Color(200, 255, 120) * (blink * 0.8f * dim));
        }
    }

    /// <summary>A dark panel for text over the backdrop.</summary>
    public static void Panel(Canvas c, float x, float y, float w, float h, float alpha = 0.72f)
    {
        c.Fill(x, y, w, h, new Color(10, 8, 24) * alpha);
        c.Frame(x, y, w, h, new Color(190, 140, 50) * 0.9f, 0.6f);
    }
}
