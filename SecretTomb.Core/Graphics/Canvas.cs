using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SecretTomb.Core.Oric;

namespace SecretTomb.Core.Graphics;

/// <summary>A rectangle in (fractional) virtual pixels.</summary>
public readonly record struct RectF(float X, float Y, float W, float H)
{
    public float Right => X + W;
    public float Bottom => Y + H;
    public Vector2 Center => new(X + W / 2, Y + H / 2);
    public bool Contains(float x, float y) => x >= X && x < Right && y >= Y && y < Bottom;
    public bool Contains(Vector2 p) => Contains(p.X, p.Y);
    public RectF Inflate(float by) => new(X - by, Y - by, W + by * 2, H + by * 2);
    public static implicit operator RectF(Rectangle r) => new(r.X, r.Y, r.Width, r.Height);
}

/// <summary>
/// Draws the game at high resolution. Layout uses a 240x224 virtual screen (the Oric's, so the
/// screens keep its proportions and text grid), but everything is rendered at the full device
/// resolution: the artwork is drawn from high-resolution pictures, shapes are filled per device
/// pixel row with smooth shading, and the ROM font is smoothed with the Scale2x filter.
/// </summary>
public sealed class Canvas : IDisposable
{
    public const int Width = 240;
    public const int Height = 224;
    public const int Columns = Width / OricFont.GlyphWidth;   // 40
    public const int Rows = Height / OricFont.GlyphHeight;    // 28

    /// <summary>Font glyphs are smoothed to this many texels per pixel (Scale2x twice).</summary>
    public const int FontDetail = 4;

    private readonly GraphicsDevice _device;
    private readonly SpriteBatch _batch;
    private readonly Texture2D _pixel;
    private readonly Texture2D _ramp;
    private readonly Texture2D _shadow;
    private readonly Texture2D _glow;
    private readonly Texture2D _edge;
    private readonly Texture2D _font;
    private readonly Texture2D _glyphs;
    private readonly Dictionary<string, Texture2D> _art = new();
    private Vector2 _offset;

    public Canvas(GraphicsDevice device)
    {
        _device = device;
        _batch = new SpriteBatch(device);
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData([Color.White]);
        _ramp = BuildRamp(device);
        _shadow = BuildRadial(device, 64, 32, 0.55f);
        _glow = BuildRadial(device, 64, 64, 1f);
        _edge = BuildEdge(device);
        _font = BuildFont(device, OricFont.GlyphCount, OricFont.GlyphAt);
        _glyphs = BuildFont(device, Glyphs.Count, Glyphs.Rows);
    }

    /// <summary>Device pixels per virtual pixel for the frame being drawn.</summary>
    public float Scale { get; private set; } = 1f;

    /// <summary>Shifts everything drawn (screen shake), in virtual pixels.</summary>
    public Vector2 Offset
    {
        get => _offset;
        set => _offset = value;
    }

    private int DeviceWidth => (int)MathF.Round(Width * Scale);
    private int DeviceHeight => (int)MathF.Round(Height * Scale);

    private Vector2 Dev(float x, float y) => new((x + _offset.X) * Scale, (y + _offset.Y) * Scale);

    public void Begin(float scale)
    {
        Scale = scale;
        _offset = Vector2.Zero;
        _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
    }

    public void End() => _batch.End();

    // ---------------------------------------------------------------- rectangles and lines

    public void Fill(float x, float y, float w, float h, Color color)
    {
        if (w <= 0 || h <= 0)
            return;
        _batch.Draw(_pixel, Dev(x, y), null, color, 0f, Vector2.Zero, new Vector2(w * Scale, h * Scale),
            SpriteEffects.None, 0f);
    }

    public void Fill(float x, float y, float w, float h, OricColor color) => Fill(x, y, w, h, color.ToColor());

    /// <summary>Outline of a rectangle.</summary>
    public void Frame(float x, float y, float w, float h, Color color, float thickness = 1f)
    {
        Fill(x, y, w, thickness, color);
        Fill(x, y + h - thickness, w, thickness, color);
        Fill(x, y, thickness, h, color);
        Fill(x + w - thickness, y, thickness, h, color);
    }

    public void Frame(float x, float y, float w, float h, OricColor color, float thickness = 1f) =>
        Frame(x, y, w, h, color.ToColor(), thickness);

    /// <summary>A thin line (width in virtual pixels; at least one device pixel).</summary>
    public void Line(Vector2 a, Vector2 b, Color color, float width = 0.6f)
    {
        var d = (b - a) * Scale;
        float length = d.Length();
        if (length < 0.01f)
            return;
        float w = MathF.Max(1f, width * Scale);
        float angle = MathF.Atan2(d.Y, d.X);
        _batch.Draw(_pixel, Dev(a.X, a.Y), null, color, angle, new Vector2(0f, 0.5f), new Vector2(length, w),
            SpriteEffects.None, 0f);
    }

    public void Line(float x0, float y0, float x1, float y1, Color color, float width = 0.6f) =>
        Line(new Vector2(x0, y0), new Vector2(x1, y1), color, width);

    /// <summary>
    /// A soft-edged band of shade: opaque <paramref name="color"/> along one side of the rectangle,
    /// fading to nothing at the other (direction 0 top, 1 right, 2 bottom, 3 left = the dark side).
    /// </summary>
    public void Fade(float x, float y, float w, float h, Color color, int darkSide)
    {
        if (w <= 0 || h <= 0)
            return;
        // The edge texture is opaque at the top and clear at the bottom.
        switch (darkSide)
        {
            case 0:
                _batch.Draw(_edge, Dev(x, y), null, color, 0f, Vector2.Zero,
                    new Vector2(w * Scale / _edge.Width, h * Scale / _edge.Height), SpriteEffects.None, 0f);
                break;
            case 2:
                _batch.Draw(_edge, Dev(x, y), null, color, 0f, Vector2.Zero,
                    new Vector2(w * Scale / _edge.Width, h * Scale / _edge.Height), SpriteEffects.FlipVertically, 0f);
                break;
            case 3:
                _batch.Draw(_edge, Dev(x, y + h), null, color, -MathF.PI / 2, Vector2.Zero,
                    new Vector2(h * Scale / _edge.Width, w * Scale / _edge.Height), SpriteEffects.None, 0f);
                break;
            default:
                _batch.Draw(_edge, Dev(x + w, y), null, color, MathF.PI / 2, Vector2.Zero,
                    new Vector2(h * Scale / _edge.Width, w * Scale / _edge.Height), SpriteEffects.None, 0f);
                break;
        }
    }

    // ---------------------------------------------------------------- polygons

    /// <summary>Fills a convex polygon, one device pixel row at a time.</summary>
    public void FillPolygon(ReadOnlySpan<Vector2> points, Color color)
    {
        foreach (var (y, x0, x1) in Geometry.Spans(points, Scale, DeviceWidth, DeviceHeight))
            _batch.Draw(_pixel, new Vector2(x0, y), null, color, 0f, Vector2.Zero, new Vector2(x1 - x0, 1f),
                SpriteEffects.None, 0f);
    }

    /// <summary>
    /// Fills a convex polygon with smooth shading: <paramref name="brightness"/> gives the light
    /// (0 dark .. 1 full colour) at a virtual point, interpolated along each row.
    /// </summary>
    public void ShadePolygon(ReadOnlySpan<Vector2> points, Color color, Func<float, float, float> brightness)
    {
        foreach (var (y, x0, x1) in Geometry.Spans(points, Scale, DeviceWidth, DeviceHeight))
        {
            float vy = (y + 0.5f) / Scale;
            float b0 = Math.Clamp(brightness(x0 / Scale, vy), 0f, 1f);
            float b1 = Math.Clamp(brightness(x1 / Scale, vy), 0f, 1f);
            int u0 = (int)(b0 * 255), u1 = (int)(b1 * 255);
            var flip = SpriteEffects.None;
            if (u0 > u1)
            {
                (u0, u1) = (u1, u0);
                flip = SpriteEffects.FlipHorizontally;
            }
            var src = new Rectangle(u0, 0, Math.Max(1, u1 - u0), 1);
            _batch.Draw(_ramp, new Vector2(x0, y), src, color, 0f, Vector2.Zero,
                new Vector2((x1 - x0) / src.Width, 1f), flip, 0f);
        }
    }

    /// <summary>A vertical gradient filling a rectangle.</summary>
    public void Gradient(float x, float y, float w, float h, Color top, Color bottom)
    {
        Fill(x, y, w, h, bottom);
        Fade(x, y, w, h, top, 0);
    }

    /// <summary>A soft dark ellipse on the floor under a figure.</summary>
    public void Shadow(float cx, float cy, float w, float h, float opacity = 0.6f)
    {
        _batch.Draw(_shadow, Dev(cx - w / 2, cy - h / 2), null, Color.Black * opacity, 0f,
            Vector2.Zero, new Vector2(w * Scale / _shadow.Width, h * Scale / _shadow.Height), SpriteEffects.None, 0f);
    }

    /// <summary>A soft pool of light (torches, magic, lasers).</summary>
    public void Glow(float cx, float cy, float radius, Color color)
    {
        _batch.Draw(_glow, Dev(cx - radius, cy - radius), null, color, 0f, Vector2.Zero,
            new Vector2(radius * 2 * Scale / _glow.Width, radius * 2 * Scale / _glow.Height), SpriteEffects.None, 0f);
    }

    /// <summary>A soft ellipse of light or colour.</summary>
    public void GlowEllipse(float cx, float cy, float rx, float ry, Color color)
    {
        _batch.Draw(_glow, Dev(cx - rx, cy - ry), null, color, 0f, Vector2.Zero,
            new Vector2(rx * 2 * Scale / _glow.Width, ry * 2 * Scale / _glow.Height), SpriteEffects.None, 0f);
    }

    // ---------------------------------------------------------------- pictures

    /// <summary>The high-resolution picture with this name (embedded Art/name.png), or null.</summary>
    public Texture2D Art(string name)
    {
        if (_art.TryGetValue(name, out var tex))
            return tex;
        using var stream = typeof(Canvas).Assembly.GetManifestResourceStream("Art." + name + ".png");
        if (stream != null)
        {
            tex = Texture2D.FromStream(_device, stream);
            // Pictures are stored with straight alpha; the sprite batch blends premultiplied colour.
            var data = new Color[tex.Width * tex.Height];
            tex.GetData(data);
            for (int i = 0; i < data.Length; i++)
            {
                var c = data[i];
                data[i] = new Color(c.R * c.A / 255, c.G * c.A / 255, c.B * c.A / 255, c.A);
            }
            tex.SetData(data);
        }
        _art[name] = tex;
        return tex;
    }

    /// <summary>Draws a picture stretched into a rectangle.</summary>
    public void Draw(string name, RectF r, Color? tint = null, bool mirror = false)
    {
        var tex = Art(name);
        if (tex == null || r.W <= 0 || r.H <= 0)
            return;
        _batch.Draw(tex, Dev(r.X, r.Y), null, tint ?? Color.White, 0f, Vector2.Zero,
            new Vector2(r.W * Scale / tex.Width, r.H * Scale / tex.Height),
            mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }

    /// <summary>Draws a picture centred on a point, rotated, at a size in virtual pixels.</summary>
    public void DrawRotated(string name, Vector2 centre, float w, float h, float rotation, Color? tint = null,
        bool mirror = false)
    {
        var tex = Art(name);
        if (tex == null)
            return;
        _batch.Draw(tex, Dev(centre.X, centre.Y), null, tint ?? Color.White, rotation,
            new Vector2(tex.Width / 2f, tex.Height / 2f), new Vector2(w * Scale / tex.Width, h * Scale / tex.Height),
            mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }

    /// <summary>Draws the top part (0..1) of a picture, e.g. a swimmer whose legs are under water.</summary>
    public void DrawTop(string name, RectF r, float part, Color? tint = null, bool mirror = false)
    {
        var tex = Art(name);
        if (tex == null || r.W <= 0 || r.H <= 0)
            return;
        int sh = Math.Max(1, (int)(tex.Height * part));
        _batch.Draw(tex, Dev(r.X, r.Y), new Rectangle(0, 0, tex.Width, sh), tint ?? Color.White, 0f, Vector2.Zero,
            new Vector2(r.W * Scale / tex.Width, r.H * Scale / tex.Height),
            mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }

    // ---------------------------------------------------------------- text

    /// <summary>Text at a virtual position in the (smoothed) Oric ROM font.</summary>
    public void Text(float x, float y, string text, Color ink, float scale = 1f)
    {
        float gw = OricFont.GlyphWidth * scale, gh = OricFont.GlyphHeight * scale;
        var size = new Vector2(gw * Scale / (OricFont.GlyphWidth * FontDetail), gh * Scale / (OricFont.GlyphHeight * FontDetail));
        for (int i = 0; i < text.Length; i++)
        {
            int index = OricFont.IndexOf(text[i]);
            if (index == 0)
                continue;
            var src = new Rectangle(index * OricFont.GlyphWidth * FontDetail, 0, OricFont.GlyphWidth * FontDetail,
                OricFont.GlyphHeight * FontDetail);
            _batch.Draw(_font, Dev(x + i * gw, y), src, ink, 0f, Vector2.Zero, size, SpriteEffects.None, 0f);
        }
    }

    public void Text(float x, float y, string text, OricColor ink, float scale = 1f) => Text(x, y, text, ink.ToColor(), scale);

    /// <summary>Text with a dark drop shadow, for titles over pictures.</summary>
    public void TextShadowed(float x, float y, string text, Color ink, float scale = 1f)
    {
        Text(x + 0.5f * scale, y + 0.5f * scale, text, Color.Black * 0.8f, scale);
        Text(x, y, text, ink, scale);
    }

    /// <summary>Text at a character cell (40 columns).</summary>
    public void TextAt(int column, float y, string text, OricColor ink) =>
        Text(column * OricFont.GlyphWidth, y, text, ink);

    public void TextAt(int column, float y, string text, Color ink) =>
        Text(column * OricFont.GlyphWidth, y, text, ink);

    /// <summary>Text horizontally centred on the screen.</summary>
    public void TextCentered(float y, string text, OricColor ink, float scale = 1f) =>
        TextCentered(y, text, ink.ToColor(), scale);

    public void TextCentered(float y, string text, Color ink, float scale = 1f) =>
        Text((Width - text.Length * OricFont.GlyphWidth * scale) / 2, y, text, ink, scale);

    public void TextCenteredShadowed(float y, string text, Color ink, float scale = 1f) =>
        TextShadowed((Width - text.Length * OricFont.GlyphWidth * scale) / 2, y, text, ink, scale);

    public static float TextWidth(string text, float scale = 1f) => text.Length * OricFont.GlyphWidth * scale;

    /// <summary>A line of the tomb's own script (see <see cref="Glyphs"/>), each sign 8 x 8.</summary>
    public void GlyphText(float x, float y, string text, Color ink, float scale = 1f)
    {
        float gw = Glyphs.Size * scale;
        var size = new Vector2(gw * Scale / (Glyphs.Size * FontDetail), gw * Scale / (Glyphs.Size * FontDetail));
        int col = 0;
        foreach (char ch in text)
        {
            int index = Glyphs.IndexOf(ch);
            if (index >= 0)
            {
                var src = new Rectangle(index * Glyphs.Size * FontDetail, 0, Glyphs.Size * FontDetail, Glyphs.Size * FontDetail);
                _batch.Draw(_glyphs, Dev(x + col * gw, y), src, ink, 0f, Vector2.Zero, size, SpriteEffects.None, 0f);
            }
            col++;
        }
    }

    // ---------------------------------------------------------------- textures

    private static Texture2D BuildRamp(GraphicsDevice device)
    {
        var data = new Color[256];
        for (int i = 0; i < 256; i++)
            data[i] = new Color(i, i, i, 255);
        var tex = new Texture2D(device, 256, 1);
        tex.SetData(data);
        return tex;
    }

    private static Texture2D BuildEdge(GraphicsDevice device)
    {
        const int n = 64;
        var data = new Color[n];
        for (int i = 0; i < n; i++)
        {
            float a = 1f - (i + 0.5f) / n;
            a = a * a * (3 - 2 * a);
            data[i] = new Color(a, a, a, a);
        }
        var tex = new Texture2D(device, 1, n);
        tex.SetData(data);
        return tex;
    }

    private static Texture2D BuildRadial(GraphicsDevice device, int w, int h, float softness)
    {
        var data = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f) / w * 2 - 1, dy = (y + 0.5f) / h * 2 - 1;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                float a = Math.Clamp((1f - d) / softness, 0f, 1f);
                a = a * a * (3 - 2 * a);
                data[y * w + x] = new Color(a, a, a, a);
            }
        var tex = new Texture2D(device, w, h);
        tex.SetData(data);
        return tex;
    }

    /// <summary>A strip of 1-bit glyphs (rows given MSB-left), smoothed with Scale2x.</summary>
    private static Texture2D BuildFont(GraphicsDevice device, int count, Func<int, ReadOnlySpan<byte>> glyph)
    {
        int cw = count == Glyphs.Count ? Glyphs.Size : OricFont.GlyphWidth;
        int ch = count == Glyphs.Count ? Glyphs.Size : OricFont.GlyphHeight;
        int gw = cw * FontDetail, gh = ch * FontDetail;
        int w = count * gw;
        var data = new Color[w * gh];
        for (int g = 0; g < count; g++)
        {
            var rows = glyph(g);
            var bits = new byte[cw * ch];
            for (int y = 0; y < ch; y++)
                for (int x = 0; x < cw; x++)
                    if ((rows[y] & (1 << (cw - 1 - x))) != 0)
                        bits[y * cw + x] = 1;
            var (big, bw, bh) = PixelArt.Smooth(bits, cw, ch, FontDetail);
            for (int y = 0; y < bh; y++)
                for (int x = 0; x < bw; x++)
                    if (big[y * bw + x] != 0)
                        data[y * w + g * gw + x] = Color.White;
        }
        var tex = new Texture2D(device, w, gh);
        tex.SetData(data);
        return tex;
    }

    public void Dispose()
    {
        foreach (var t in _art.Values)
            t?.Dispose();
        _art.Clear();
        _pixel.Dispose();
        _ramp.Dispose();
        _shadow.Dispose();
        _glow.Dispose();
        _edge.Dispose();
        _font.Dispose();
        _glyphs.Dispose();
        _batch.Dispose();
    }
}

/// <summary>Enlarging pixel art without blocks: the Scale2x (EPX) filter, applied repeatedly.</summary>
public static class PixelArt
{
    /// <summary>Scale2x: doubles the image, rounding off diagonal edges instead of making stairs.</summary>
    public static byte[] Scale2x(byte[] src, int w, int h)
    {
        var dst = new byte[w * 2 * h * 2];
        int dw = w * 2;
        byte At(int x, int y) => src[Math.Clamp(y, 0, h - 1) * w + Math.Clamp(x, 0, w - 1)];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                byte b = At(x, y - 1), d = At(x - 1, y), e = At(x, y), f = At(x + 1, y), hh = At(x, y + 1);
                byte e0 = e, e1 = e, e2 = e, e3 = e;
                if (b != hh && d != f)
                {
                    e0 = d == b ? d : e;
                    e1 = b == f ? f : e;
                    e2 = d == hh ? d : e;
                    e3 = hh == f ? f : e;
                }
                dst[y * 2 * dw + x * 2] = e0;
                dst[y * 2 * dw + x * 2 + 1] = e1;
                dst[(y * 2 + 1) * dw + x * 2] = e2;
                dst[(y * 2 + 1) * dw + x * 2 + 1] = e3;
            }
        return dst;
    }

    /// <summary>Applies Scale2x until the image is <paramref name="factor"/> (a power of two) times larger.</summary>
    public static (byte[] pixels, int width, int height) Smooth(byte[] src, int w, int h, int factor)
    {
        var p = src;
        for (int f = 1; f < factor; f *= 2)
        {
            p = Scale2x(p, w, h);
            w *= 2;
            h *= 2;
        }
        return (p, w, h);
    }
}

/// <summary>Raster helpers (testable without a graphics device).</summary>
public static class Geometry
{
    /// <summary>
    /// Horizontal spans (device row, left, right) covering a convex polygon given in virtual
    /// pixels, rasterised at <paramref name="scale"/> device pixels per virtual pixel and clipped.
    /// </summary>
    public static List<(int y, float x0, float x1)> Spans(ReadOnlySpan<Vector2> pts, float scale, int width, int height)
    {
        var result = new List<(int, float, float)>();
        if (pts.Length < 3)
            return result;
        float top = float.MaxValue, bottom = float.MinValue;
        foreach (var p in pts)
        {
            top = Math.Min(top, p.Y * scale);
            bottom = Math.Max(bottom, p.Y * scale);
        }
        int y0 = Math.Max(0, (int)MathF.Floor(top));
        int y1 = Math.Min(height - 1, (int)MathF.Ceiling(bottom) - 1);
        for (int y = y0; y <= y1; y++)
        {
            float yc = y + 0.5f;
            float left = float.MaxValue, right = float.MinValue;
            for (int i = 0; i < pts.Length; i++)
            {
                var a = pts[i] * scale;
                var b = pts[(i + 1) % pts.Length] * scale;
                float lo = Math.Min(a.Y, b.Y), hi = Math.Max(a.Y, b.Y);
                if (yc < lo || yc > hi || hi - lo < 1e-6f)
                    continue;
                float x = a.X + (yc - a.Y) * (b.X - a.X) / (b.Y - a.Y);
                left = Math.Min(left, x);
                right = Math.Max(right, x);
            }
            if (left > right)
                continue;
            left = Math.Max(0, left);
            right = Math.Min(width, right);
            if (right > left)
                result.Add((y, left, right));
        }
        return result;
    }
}
