using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using SecretTomb.Core.Engine;
using SecretTomb.Core.World;

namespace SecretTomb.Core.Input;

/// <summary>
/// Gathers keyboard, mouse and touch into a single per-frame view used by every scene.
/// Taps and clicks are reported in virtual (Oric) pixels. During play on a phone or tablet the
/// touches drive the on-screen stick and buttons (<see cref="TouchControls"/>).
/// </summary>
public sealed class InputManager
{
    private readonly bool _isMobile;
    private KeyboardState _prevKeys;
    private MouseState _prevMouse;
    private bool _prevBack;
    private readonly List<Vector2> _taps = new();
    private readonly List<char> _typed = new();

    public InputManager(bool isMobile)
    {
        _isMobile = isMobile;
        if (_isMobile)
            TouchPanel.EnabledGestures = GestureType.None;
    }

    public bool IsMobile => _isMobile;

    /// <summary>The on-screen stick and buttons (phones and tablets, during play).</summary>
    public TouchControls Touch { get; } = new();

    /// <summary>Set by the game scene: touches drive the on-screen controls.</summary>
    public bool PlayControls { get; set; }

    /// <summary>What the player asks the explorer to do this frame.</summary>
    public Controls Game => _game;

    private Controls _game;

    /// <summary>Mouse position in virtual pixels (desktop), for highlighting.</summary>
    public Vector2? Pointer { get; private set; }

    public bool ConfirmPressed { get; private set; }
    public bool BackPressed { get; private set; }
    public bool PausePressed { get; private set; }
    public bool UpPressed { get; private set; }
    public bool DownPressed { get; private set; }
    public bool LeftPressed { get; private set; }
    public bool RightPressed { get; private set; }
    public bool LanguagePressed { get; private set; }
    public bool VolumePressed { get; private set; }
    public bool InstructionsPressed { get; private set; }

    /// <summary>New touches / clicks this frame, in virtual pixels.</summary>
    public IReadOnlyList<Vector2> Taps => _taps;

    /// <summary>Letters / digits typed this frame (name entry).</summary>
    public IReadOnlyList<char> Typed => _typed;

    public void Update(Func<Vector2, Vector2> toVirtual, bool isActive, Rectangle screen, Rectangle destination)
    {
        _taps.Clear();
        _typed.Clear();

        var keys = Keyboard.GetState();
        // Android reports its Back button as a game pad Back button; nothing else is read from pads.
        bool back = GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.Back);
        bool Down(Keys k) => isActive && keys.IsKeyDown(k);
        bool Hit(Keys k) => isActive && keys.IsKeyDown(k) && !_prevKeys.IsKeyDown(k);

        // --- walking ---
        float mx = 0, my = 0;
        if (Down(Keys.Left) || Down(Keys.A)) mx -= 1;
        if (Down(Keys.Right) || Down(Keys.D)) mx += 1;
        if (Down(Keys.Up) || Down(Keys.W)) my -= 1;
        if (Down(Keys.Down) || Down(Keys.S)) my += 1;

        // --- menus ---
        LeftPressed = Hit(Keys.Left);
        RightPressed = Hit(Keys.Right);
        UpPressed = Hit(Keys.Up);
        DownPressed = Hit(Keys.Down);
        ConfirmPressed = Hit(Keys.Space) || Hit(Keys.Enter);
        BackPressed = Hit(Keys.Escape) || (back && !_prevBack);
        PausePressed = Hit(Keys.P);
        LanguagePressed = Hit(Keys.L);
        VolumePressed = Hit(Keys.V);
        InstructionsPressed = Hit(Keys.I);

        // --- play: fire, aim, jump, action ---
        var c = new Controls
        {
            Fire = Down(Keys.Space) || Down(Keys.LeftControl) || Down(Keys.RightControl),
            Jump = Down(Keys.X) || Down(Keys.LeftShift) || Down(Keys.RightShift),
            Action = Down(Keys.E) || Down(Keys.Enter) || Down(Keys.C),
        };
        if (Hit(Keys.I)) c.Aim = Dir.Up;
        else if (Hit(Keys.K)) c.Aim = Dir.Down;
        else if (Hit(Keys.J)) c.Aim = Dir.Left;
        else if (Hit(Keys.L)) c.Aim = Dir.Right;

        // --- mouse (desktop only) ---
        Pointer = null;
        if (!_isMobile)
        {
            var mouse = Mouse.GetState();
            var v = toVirtual(new Vector2(mouse.X, mouse.Y));
            bool inside = v.X >= 0 && v.Y >= 0 && v.X < Graphics.Canvas.Width && v.Y < Graphics.Canvas.Height;
            if (inside && isActive)
                Pointer = v;
            if (isActive && inside && mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                _taps.Add(v);
            _prevMouse = mouse;
        }

        // --- touch ---
        var touches = TouchPanel.GetState();
        Touch.Layout(screen, destination);
        if (PlayControls && _isMobile)
        {
            Touch.Update(touches);
            if (Touch.Move.LengthSquared() > 0)
            {
                mx = Touch.Move.X;
                my = Touch.Move.Y;
            }
            c.Fire |= Touch.Fire;
            c.Jump |= Touch.Jump;
            c.Action |= Touch.Action;
            PausePressed |= Touch.PausePressed;
            foreach (var t in Touch.FreeTaps)
                _taps.Add(toVirtual(t));
        }
        else
        {
            Touch.Release();
            int pressed = 0;
            foreach (var t in touches)
            {
                if (t.State != TouchLocationState.Pressed)
                    continue;
                pressed++;
                _taps.Add(toVirtual(t.Position));
            }
            if (touches.Count >= 3 && pressed > 0)
                PausePressed = true;
        }

        c.Move = new Vector2(mx, my);
        _game = c;

        // --- typed characters for name entry ---
        foreach (var k in keys.GetPressedKeys())
        {
            if (_prevKeys.IsKeyDown(k) || !isActive)
                continue;
            if (k >= Keys.A && k <= Keys.Z)
                _typed.Add((char)('A' + (k - Keys.A)));
            else if (k >= Keys.D0 && k <= Keys.D9)
                _typed.Add((char)('0' + (k - Keys.D0)));
            else if (k == Keys.Back)
                _typed.Add('\b');
        }

        _prevKeys = keys;
        _prevBack = back;
    }
}
