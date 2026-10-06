using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace SecretTomb.Core.Input;

/// <summary>
/// The touch screen controls used during play: a stick under the left thumb (it appears wherever
/// the thumb comes down on the left side), and the FIRE, JUMP and ACTION buttons under the right.
/// They sit beside the game in the space a wide screen leaves free, or over its edges on a
/// squarer one. Positions are in screen (device) pixels.
/// </summary>
public sealed class TouchControls
{
    public enum Button
    {
        Fire,
        Jump,
        Action,
        Pause,
    }

    private readonly Dictionary<int, Button> _buttonTouches = new();
    private readonly List<Vector2> _freeTaps = new();
    private int _stickTouch = -1;
    private Vector2 _stickOrigin;
    private Vector2 _stickAt;
    private readonly bool[] _down = new bool[4];
    private readonly bool[] _prevDown = new bool[4];
    private Texture2D _disc;

    public Rectangle Screen { get; private set; }
    public float ButtonRadius { get; private set; }
    public float StickRadius { get; private set; }
    public Vector2 StickHome { get; private set; }

    public Vector2 Move { get; private set; }
    public bool Fire => _down[(int)Button.Fire];
    public bool Jump => _down[(int)Button.Jump];
    public bool Action => _down[(int)Button.Action];
    public bool PausePressed => _down[(int)Button.Pause] && !_prevDown[(int)Button.Pause];

    /// <summary>Touches that landed on nothing (used to close panels).</summary>
    public IReadOnlyList<Vector2> FreeTaps => _freeTaps;

    public Vector2 Centre(Button b)
    {
        float r = ButtonRadius, m = Screen.Height * 0.045f;
        var fire = new Vector2(Screen.Right - m - r * 1.25f, Screen.Bottom - m - r * 1.35f);
        return b switch
        {
            Button.Fire => fire,
            Button.Jump => fire + new Vector2(-r * 2.35f, r * 0.45f),
            Button.Action => fire + new Vector2(-r * 0.35f, -r * 2.35f),
            _ => new Vector2(Screen.Right - m - r * 0.6f, Screen.Top + m + r * 0.6f),
        };
    }

    public float Radius(Button b) => b == Button.Pause ? ButtonRadius * 0.6f : b == Button.Fire ? ButtonRadius * 1.15f : ButtonRadius;

    public void Layout(Rectangle screen, Rectangle destination)
    {
        Screen = screen;
        ButtonRadius = screen.Height * 0.105f;
        StickRadius = screen.Height * 0.15f;
        float m = screen.Height * 0.045f;
        float bar = destination.X - screen.X;
        StickHome = bar > StickRadius * 2.4f
            ? new Vector2(screen.X + bar / 2, screen.Bottom - m - StickRadius * 1.4f)
            : new Vector2(screen.X + m + StickRadius * 1.2f, screen.Bottom - m - StickRadius * 1.4f);
    }

    public void Release()
    {
        _buttonTouches.Clear();
        _stickTouch = -1;
        Move = Vector2.Zero;
        Array.Clear(_down);
        Array.Clear(_prevDown);
    }

    public void Update(TouchCollection touches)
    {
        Array.Copy(_down, _prevDown, _down.Length);
        Array.Clear(_down);
        _freeTaps.Clear();
        bool stickSeen = false;

        foreach (var t in touches)
        {
            var p = t.Position;
            bool ended = t.State == TouchLocationState.Released || t.State == TouchLocationState.Invalid;
            if (t.State == TouchLocationState.Pressed)
            {
                var hit = ButtonAt(p);
                if (hit is { } b)
                    _buttonTouches[t.Id] = b;
                else if (p.X < Screen.X + Screen.Width * 0.42f && _stickTouch < 0)
                {
                    _stickTouch = t.Id;
                    _stickOrigin = ClampOrigin(p);
                    _stickAt = p;
                }
                else
                    _freeTaps.Add(p);
            }

            if (t.Id == _stickTouch)
            {
                if (ended)
                    _stickTouch = -1;
                else
                {
                    stickSeen = true;
                    _stickAt = p;
                }
                continue;
            }

            if (_buttonTouches.TryGetValue(t.Id, out var held))
            {
                if (ended)
                    _buttonTouches.Remove(t.Id);
                else
                {
                    // Sliding a thumb between buttons moves the press with it.
                    var now = ButtonAt(p, 1.25f) ?? held;
                    _buttonTouches[t.Id] = now;
                    _down[(int)now] = true;
                }
            }
        }

        if (!stickSeen)
            _stickTouch = -1;
        if (_stickTouch >= 0)
        {
            var d = (_stickAt - _stickOrigin) / StickRadius;
            float len = d.Length();
            Move = len < 0.18f ? Vector2.Zero : (len > 1 ? d / len : d);
        }
        else
            Move = Vector2.Zero;
    }

    private Vector2 ClampOrigin(Vector2 p)
    {
        float r = StickRadius;
        return new Vector2(Math.Clamp(p.X, Screen.X + r, Screen.Right - r), Math.Clamp(p.Y, Screen.Y + r, Screen.Bottom - r));
    }

    private Button? ButtonAt(Vector2 p, float slack = 1.15f)
    {
        foreach (Button b in Enum.GetValues<Button>())
            if (Vector2.Distance(p, Centre(b)) <= Radius(b) * slack)
                return b;
        return null;
    }

    /// <summary>Draws the controls over the finished frame.</summary>
    public void Draw(SpriteBatch batch, GraphicsDevice device, Func<string, Texture2D> art)
    {
        _disc ??= BuildDisc(device);
        var stickCentre = _stickTouch >= 0 ? _stickOrigin : StickHome;
        DrawDisc(batch, stickCentre, StickRadius, new Color(20, 20, 40) * 0.45f);
        DrawRing(batch, stickCentre, StickRadius, new Color(255, 214, 96) * 0.55f);
        var knob = stickCentre + Move * StickRadius * 0.75f;
        DrawDisc(batch, knob, StickRadius * 0.45f, new Color(255, 214, 96) * (_stickTouch >= 0 ? 0.75f : 0.4f));

        foreach (Button b in Enum.GetValues<Button>())
        {
            var c = Centre(b);
            float r = Radius(b);
            bool down = _down[(int)b];
            var fill = b switch
            {
                Button.Fire => new Color(200, 40, 40),
                Button.Jump => new Color(40, 110, 200),
                Button.Action => new Color(40, 150, 80),
                _ => new Color(60, 60, 70),
            };
            DrawDisc(batch, c, r, fill * (down ? 0.85f : 0.5f));
            DrawRing(batch, c, r, Color.White * (down ? 0.9f : 0.5f));
            var icon = art(b switch
            {
                Button.Fire => "Pistol",
                Button.Jump => "BtnJump",
                Button.Action => "BtnAct",
                _ => "BtnPause",
            });
            if (icon != null)
            {
                float s = r * 1.2f / Math.Max(icon.Width, icon.Height);
                batch.Draw(icon, c, null, Color.White * (down ? 1f : 0.85f), 0f, new Vector2(icon.Width / 2f, icon.Height / 2f), s,
                    SpriteEffects.None, 0f);
            }
        }
    }

    private void DrawDisc(SpriteBatch batch, Vector2 c, float r, Color colour) =>
        batch.Draw(_disc, new Rectangle((int)(c.X - r), (int)(c.Y - r), (int)(r * 2), (int)(r * 2)), new Rectangle(0, 0, 128, 128), colour);

    private void DrawRing(SpriteBatch batch, Vector2 c, float r, Color colour) =>
        batch.Draw(_disc, new Rectangle((int)(c.X - r), (int)(c.Y - r), (int)(r * 2), (int)(r * 2)), new Rectangle(128, 0, 128, 128), colour);

    /// <summary>A filled disc and a ring, side by side, with soft edges.</summary>
    private static Texture2D BuildDisc(GraphicsDevice device)
    {
        const int n = 128;
        var data = new Color[n * 2 * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                float fill = Math.Clamp((1f - d) * n / 2, 0, 1);
                float ring = Math.Clamp((1f - d) * n / 2, 0, 1) * Math.Clamp((d - 0.9f) * n / 2, 0, 1);
                data[y * n * 2 + x] = new Color(fill, fill, fill, fill);
                data[y * n * 2 + n + x] = new Color(ring, ring, ring, ring);
            }
        var tex = new Texture2D(device, n * 2, n);
        tex.SetData(data);
        return tex;
    }
}
