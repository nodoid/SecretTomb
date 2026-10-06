using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SecretTomb.Core.Graphics;
using SecretTomb.Core.Localization;
using SecretTomb.Core.World;

namespace SecretTomb.Core.Engine;

/// <summary>
/// Draws the tomb around the explorer, as the original did with its multidirectional scroll:
/// a title bar, the view, and a status bar.
/// </summary>
public static class TombRenderer
{
    public const float TopBar = 12f;
    public const float ViewTop = TopBar;
    public const float ViewBottom = ViewTop + Adventure.ViewHeight;   // 200
    public const int S = Tomb.TileSize;

    private static readonly Color Gold = new(255, 214, 96);
    private static readonly Color Bar = new(20, 22, 48);

    /// <summary>Converts a point in the tomb to the screen.</summary>
    public static Vector2 ToScreen(Adventure a, Vector2 world) =>
        world - a.Camera + new Vector2(Adventure.ViewWidth / 2, ViewTop + Adventure.ViewHeight / 2);

    /// <summary>Converts a point on the screen to the tomb.</summary>
    public static Vector2 ToWorld(Adventure a, Vector2 screen) =>
        screen + a.Camera - new Vector2(Adventure.ViewWidth / 2, ViewTop + Adventure.ViewHeight / 2);

    public static void Draw(Canvas c, Adventure a, int ticks, bool mobile, bool demo = false)
    {
        var shake = Vector2.Zero;
        if (a.Shake > 0)
        {
            float k = Math.Min(2.5f, a.Shake / 6f);
            shake = new Vector2(MathF.Sin(ticks * 2.3f) * k, MathF.Cos(ticks * 3.1f) * k);
        }
        c.Fill(0, ViewTop, Canvas.Width, Adventure.ViewHeight, Color.Black);
        c.Offset = shake;
        DrawWorld(c, a, ticks);
        c.Offset = Vector2.Zero;
        DrawLighting(c, a, ticks);
        DrawTopBar(c, a, ticks, demo);
        DrawStatusBar(c, a, ticks);
        DrawMessage(c, a, mobile);
        if (a.Reading is { } id)
            DrawInscription(c, a, id, mobile);
    }

    // ---------------------------------------------------------------- the world

    private static bool IsRaised(Tile t) => t is Tile.Wall or Tile.Secret or Tile.SkullWall or Tile.Door or Tile.JadeDoor
        or Tile.BookGate or Tile.TwinGate or Tile.Tablet or Tile.Rosetta or Tile.Lever or Tile.Launcher;

    private static bool IsOutsideGround(Tile t) => t is Tile.Grass or Tile.Tree;

    private static void DrawWorld(Canvas c, Adventure a, int ticks)
    {
        var tomb = a.Tomb;
        var origin = ToScreen(a, Vector2.Zero);
        int x0 = Math.Max(0, (int)MathF.Floor(-origin.X / S) - 1);
        int x1 = Math.Min(tomb.Width - 1, (int)MathF.Floor((Canvas.Width - origin.X) / S) + 1);
        int y0 = Math.Max(0, (int)MathF.Floor((ViewTop - origin.Y) / S) - 1);
        int y1 = Math.Min(tomb.Height - 1, (int)MathF.Floor((ViewBottom - origin.Y) / S) + 1);
        int waterFrame = ticks / 6 % 6;

        // 1. The ground.
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var t = tomb[x, y];
                var r = new RectF(origin.X + x * S, origin.Y + y * S, S, S);
                int h = Tomb.Hash(x, y);
                switch (t)
                {
                    case Tile.Grass:
                    case Tile.Tree:
                        c.Draw(h % 2 == 0 ? "Grass0" : "Grass1", r.Inflate(0.05f));
                        break;
                    case Tile.Water:
                        c.Draw("Water" + waterFrame, r.Inflate(0.05f));
                        break;
                    case Tile.Chasm:
                    case Tile.Rail:
                        c.Draw("Chasm", r.Inflate(0.05f));
                        break;
                    case Tile.Pit:
                        c.Draw("Pit", r);
                        break;
                    case Tile.Cracked:
                        c.Draw("Cracked", r);
                        break;
                    case Tile.SunStone:
                        c.Draw("SunStone", r);
                        break;
                    case Tile.Plate:
                        c.Draw("Plate", r);
                        break;
                    case Tile.Teleporter:
                        c.Draw("Teleporter", r);
                        break;
                    case Tile.Stairs:
                        c.Draw("Stairs", r);
                        break;
                    case Tile.Gateway:
                        c.Draw("Gateway", r);
                        break;
                    default:
                        if (!IsRaised(t))
                            c.Draw("Floor" + h % 4, r.Inflate(0.05f));
                        break;
                }
            }

        // 2. Shading and things lying on the ground.
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var t = tomb[x, y];
                if (IsRaised(t))
                    continue;
                var r = new RectF(origin.X + x * S, origin.Y + y * S, S, S);
                bool chasmLike = t is Tile.Chasm or Tile.Rail;
                if (chasmLike && tomb[x, y - 1] is not (Tile.Chasm or Tile.Rail))
                    c.Draw("ChasmLip", new RectF(r.X, r.Y, S, 10));
                if (t == Tile.Rail)
                    c.Draw("Rails", r);
                if (t == Tile.Water)
                {
                    if (tomb[x, y - 1] != Tile.Water)
                        c.Fade(r.X, r.Y, S, 7, new Color(0, 10, 30) * 0.7f, 0);
                    if (tomb[x - 1, y] != Tile.Water)
                        c.Fade(r.X, r.Y, 5, S, new Color(0, 10, 30) * 0.5f, 3);
                }
                if (IsOutsideGround(t) || t == Tile.Gateway)
                    continue;
                // Walls cast their shadow down and to the right.
                if (IsRaised(tomb[x, y - 1]) || tomb[x, y - 1] == Tile.Tree)
                    c.Fade(r.X, r.Y, S, 8, Color.Black * 0.55f, 0);
                if (IsRaised(tomb[x - 1, y]))
                    c.Fade(r.X, r.Y, 6, S, Color.Black * 0.4f, 3);

                switch (t)
                {
                    case Tile.Skeleton:
                        bool pistol = false;
                        foreach (var item in a.Items)
                            if (item.Kind == ItemKind.Pistol && !item.Taken && item.Cell == new Point(x, y))
                                pistol = true;
                        c.Draw(pistol ? "SkeletonPistol" : "Skeleton", r);
                        if (pistol)
                            c.Glow(r.X + 22.5f, r.Y + 15, 4 + MathF.Sin(ticks * 0.15f), new Color(80, 255, 230) * 0.6f);
                        break;
                    case Tile.DoorOpen:
                        c.Draw("DoorOpen", r);
                        break;
                    case Tile.Portcullis:
                        if (a.PortcullisOpen)
                            c.Draw("Portcullis", new RectF(r.X, r.Y - 17, S, S), Color.White * 0.9f);
                        else
                            c.Draw("Portcullis", r);
                        break;
                    case Tile.Fountain:
                        c.Draw("Fountain", r);
                        c.Glow(r.X + 12, r.Y + 12, 8 + MathF.Sin(ticks * 0.08f) * 1.5f, new Color(120, 255, 255) * 0.35f);
                        break;
                    case Tile.Sarcophagus:
                        c.Draw("Sarcophagus", r);
                        c.Glow(r.X + 12, r.Y + 10, 14 + MathF.Sin(ticks * 0.06f) * 2, new Color(90, 255, 200) * 0.18f);
                        break;
                    case Tile.SarcophagusOpen:
                        c.Draw("SarcophagusOpen", r);
                        break;
                    case Tile.Teleporter:
                        float pulse = 0.5f + 0.5f * MathF.Sin(ticks * 0.1f);
                        c.Glow(r.X + 12, r.Y + 12, 10 + pulse * 3, new Color(60, 255, 230) * (0.3f + 0.2f * pulse));
                        break;
                    case Tile.SunStone when a.Checkpoint == new Point(x, y):
                        c.Glow(r.X + 12, r.Y + 12, 13, new Color(255, 200, 80) * (0.3f + 0.1f * MathF.Sin(ticks * 0.1f)));
                        break;
                }
            }

        // 3. Walls, doors and carvings.
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var t = tomb[x, y];
                if (!IsRaised(t))
                    continue;
                var r = new RectF(origin.X + x * S, origin.Y + y * S, S, S);
                int h = Tomb.Hash(x, y);
                switch (t)
                {
                    case Tile.Wall:
                        int v = h % 20;
                        c.Draw(v < 12 ? "Wall0" : v < 16 ? "Wall1" : "Wall2", r.Inflate(0.05f));
                        break;
                    case Tile.Secret:
                        c.Draw("Secret", r.Inflate(0.05f));
                        break;
                    case Tile.SkullWall:
                        c.Draw("SkullWall", r.Inflate(0.05f));
                        break;
                    case Tile.Door:
                        c.Draw("Door", r);
                        break;
                    case Tile.JadeDoor:
                        c.Draw("JadeDoor", r);
                        break;
                    case Tile.BookGate:
                        c.Draw("BookGate", r);
                        break;
                    case Tile.TwinGate:
                        c.Draw("TwinGate", r);
                        break;
                    case Tile.Tablet:
                        c.Draw("Tablet", r);
                        break;
                    case Tile.Lever:
                        c.Draw(a.PortcullisOpen ? "LeverDown" : "LeverUp", r);
                        break;
                    case Tile.Launcher:
                        var dir = (Dir)tomb.Param(x, y);
                        float rot = dir switch { Dir.Left => MathF.PI / 2, Dir.Up => MathF.PI, Dir.Right => -MathF.PI / 2, _ => 0 };
                        c.DrawRotated("Launcher", r.Center, S + 0.1f, S + 0.1f, rot);
                        break;
                    case Tile.Rosetta when tomb.Param(x, y) == 0:
                        DrawRosetta(c, a, new RectF(r.X, r.Y, S * 3, S));
                        break;
                }
            }

        // 4. Treasure and objects.
        foreach (var item in a.Items)
        {
            if (item.Kind == ItemKind.Pistol || (item.Taken && !item.Opened))
                continue;
            var p = ToScreen(a, item.Centre);
            if (p.X < -20 || p.X > Canvas.Width + 20 || p.Y < -20 || p.Y > ViewBottom + 20)
                continue;
            if (item.Kind == ItemKind.Chest)
            {
                c.Draw(item.Opened ? "ChestOpen" : "Chest", new RectF(p.X - 12, p.Y - 12, S, S));
                continue;
            }
            float bob = MathF.Sin(ticks * 0.08f + item.Cell.X) * 1.2f;
            string art = item.Kind switch
            {
                ItemKind.Ammo => "Ammo",
                ItemKind.Gem => "Gem",
                ItemKind.JadeKey => "JadeKey",
                ItemKind.Book => "Book",
                ItemKind.Idol => "Idol",
                _ => "Helmet",
            };
            c.Shadow(p.X, p.Y + 6, 10, 4, 0.5f);
            if (item.Kind is ItemKind.Gem or ItemKind.Idol or ItemKind.JadeKey)
                c.Glow(p.X, p.Y + bob - 2, 9, new Color(255, 240, 160) * (0.18f + 0.08f * MathF.Sin(ticks * 0.12f)));
            c.Draw(art, new RectF(p.X - 7, p.Y - 9 + bob, 14, 14));
        }

        // 5. Figures, back to front.
        var figures = new List<(float y, Action draw)>();
        foreach (var m in a.Monsters)
            if (m.Alive)
                figures.Add((m.Pos.Y, () => DrawMonster(c, a, m, ticks)));
        foreach (var b in a.Boulders)
            if (!b.Gone)
                figures.Add((b.Pos.Y, () =>
                {
                    var p = ToScreen(a, b.Pos);
                    c.Shadow(p.X + 2, p.Y + 8, 22, 8, 0.6f);
                    c.DrawRotated("Boulder", p, 22, 22, b.Angle);
                }));
        foreach (var cart in a.Carts)
            figures.Add((cart.Pos.Y - 1, () =>
            {
                var p = ToScreen(a, cart.Pos);
                c.Shadow(p.X, p.Y + 4, 22, 10, 0.5f);
                c.DrawRotated("Cart", p, 20, 24, cart.Vertical ? 0 : MathF.PI / 2);
            }));
        figures.Add((a.Pos.Y, () => DrawExplorer(c, a, ticks)));
        figures.Sort((p, q) => p.y.CompareTo(q.y));
        foreach (var f in figures)
            f.draw();

        // 6. Laser bolts, darts and sparks.
        foreach (var b in a.Bolts)
        {
            var p = ToScreen(a, b.Pos) + new Vector2(0, -6);
            var tail = p - b.Dir.Vector() * 10;
            c.Glow(p.X, p.Y, 6, new Color(60, 255, 230) * 0.5f);
            c.Line(tail, p, new Color(60, 255, 230) * 0.8f, 2.2f);
            c.Line(tail + b.Dir.Vector() * 3, p, Color.White, 0.9f);
        }
        foreach (var d in a.Darts)
        {
            var p = ToScreen(a, d.Pos) + new Vector2(0, -4);
            var tail = p - d.Dir.Vector() * 7;
            c.Line(tail, p, new Color(120, 80, 40), 1.0f);
            c.Line(p - d.Dir.Vector() * 1.6f, p, new Color(210, 220, 230), 1.2f);
            c.Line(tail, tail + d.Dir.Vector() * 2.2f, new Color(230, 60, 40), 1.8f);
        }
        foreach (var p in a.Particles)
        {
            var s = ToScreen(a, p.Pos);
            float life = 1f - (float)p.Life / p.Max;
            if (p.Glow)
                c.Glow(s.X, s.Y, p.Size * 2f, p.Color * life);
            else
                c.Fill(s.X - p.Size / 2, s.Y - p.Size / 2, p.Size, p.Size, p.Color * life);
        }

        // 7. The jungle canopy over everything.
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (tomb[x, y] == Tile.Tree)
                {
                    var centre = new Vector2(origin.X + x * S + S / 2f, origin.Y + y * S + S / 2f);
                    float sway = MathF.Sin(ticks * 0.03f + x) * 0.03f;
                    c.DrawRotated(Tomb.Hash(x, y) % 2 == 0 ? "Tree0" : "Tree1", centre, 40, 40, sway + (Tomb.Hash(x, y) % 7) * 0.5f);
                }
    }

    private static void DrawRosetta(Canvas c, Adventure a, RectF r)
    {
        c.Draw("Rosetta", r);
        string text = Strings.Inscription(0);
        float scale = Math.Min(0.62f, (r.W - 10) / (text.Length * Glyphs.Size));
        c.GlyphText(r.X + (r.W - text.Length * Glyphs.Size * scale) / 2, r.Y + 6, text, new Color(90, 50, 20), scale);
        float ts = Math.Min(0.55f, (r.W - 10) / Canvas.TextWidth(text));
        c.Text(r.X + (r.W - Canvas.TextWidth(text, ts)) / 2, r.Y + 13.5f, text, new Color(110, 60, 20), ts);
    }

    private static void DrawMonster(Canvas c, Adventure a, Monster m, int ticks)
    {
        var p = ToScreen(a, m.Pos);
        if (p.X < -30 || p.X > Canvas.Width + 30 || p.Y < -30 || p.Y > ViewBottom + 40)
            return;
        int frame = m.Anim / (m.Kind == MonsterKind.Guardian ? 14 : 10) % 2;
        bool mirror = m.Facing == Dir.Left;
        var tint = m.Stun > 0 && m.Stun / 4 % 2 == 0 ? new Color(255, 200, 200) : Color.White;
        switch (m.Kind)
        {
            case MonsterKind.Ghoul:
                c.Draw("Ghoul" + frame, new RectF(p.X - 11, p.Y - 16, 22, 24), tint, mirror);
                break;
            case MonsterKind.Guardian:
                c.Draw("Guardian" + frame, new RectF(p.X - 13, p.Y - 19, 26, 28), tint, mirror);
                break;
            default:
                c.GlowEllipse(p.X, p.Y + 1, 12, 6, new Color(0, 20, 40) * 0.5f);
                c.Draw("Fish" + frame, new RectF(p.X - 9, p.Y - 5.5f, 18, 11), tint * 0.95f, m.Facing == Dir.Left);
                break;
        }
    }

    private static void DrawExplorer(Canvas c, Adventure a, int ticks)
    {
        var p = ToScreen(a, a.Pos);
        if (a.Invulnerable > 0 && a.Invulnerable / 4 % 2 == 0 && a.Dying == DeathCause.None)
            return;
        bool side = a.Facing is Dir.Left or Dir.Right;
        bool mirror = a.Facing == Dir.Left;
        int frame = a.Walking ? a.WalkTicks / 8 % 2 : 0;
        string name = a.Facing switch
        {
            Dir.Up => "HeroUp",
            Dir.Down => "HeroDown",
            _ => "HeroSide",
        };

        if (a.Dying != DeathCause.None)
        {
            float t = Math.Min(1f, a.DeathTicks / 50f);
            switch (a.Dying)
            {
                case DeathCause.Pit:
                case DeathCause.Chasm:
                case DeathCause.Collapse:
                    float k = 1 - t;
                    if (k > 0.02f)
                        c.DrawRotated(name + "0", p + new Vector2(0, -6 * k), 18 * k, 26 * k, t * 6, null, mirror);
                    return;
                case DeathCause.Youth:
                    float y = 1 - t * 0.95f;
                    c.Glow(p.X, p.Y - 6, 16 * (1 - t) + 4, new Color(160, 255, 255) * 0.4f);
                    c.DrawRotated(name + "0", p + new Vector2(0, -6 * y), 18 * y, 26 * y, 0, Color.White * (1 - t * 0.8f), mirror);
                    return;
                case DeathCause.Drowned:
                    c.DrawTop("SwimDown", new RectF(p.X - 9, p.Y - 13 + t * 10, 18, 26), Math.Max(0.05f, 0.6f - t * 0.6f), Color.White * (1 - t));
                    return;
                default:
                    c.Shadow(p.X, p.Y + 6, 22, 7, 0.5f);
                    c.DrawRotated(name + "0", p + new Vector2(0, -2), 18, 26, MathF.PI / 2 * Math.Min(1, t * 3), new Color(255, 160, 150), mirror);
                    return;
            }
        }

        if (a.Riding != null)
        {
            c.DrawTop(name + "0", new RectF(p.X - 9, p.Y - 22, 18, 26), 0.6f, null, mirror);
            return;
        }
        if (a.Swimming)
        {
            string swim = a.Facing switch { Dir.Up => "SwimUp", Dir.Down => "SwimDown", _ => "SwimSide" };
            float bob = MathF.Sin(ticks * 0.15f) * 0.8f;
            c.GlowEllipse(p.X, p.Y - 1, 13, 5, new Color(200, 240, 255) * 0.35f);
            c.DrawTop(swim, new RectF(p.X - 9, p.Y - 14 + bob, 18, 26), 0.58f, null, mirror);
            float ring = ticks % 40 / 40f;
            c.GlowEllipse(p.X, p.Y + 1, 8 + ring * 8, 3 + ring * 3, new Color(220, 250, 255) * (0.3f * (1 - ring)));
            return;
        }

        float lift = a.JumpHeight;
        c.Shadow(p.X, p.Y + 6, 16 - lift * 0.5f, 6 - lift * 0.2f, 0.55f);
        var rect = new RectF(p.X - 9, p.Y - 19 - lift, 18, 26);
        if (!a.Outside)
            c.Glow(p.X, p.Y - 6 - lift, 22, new Color(255, 190, 110) * 0.10f);
        c.Draw(name + frame, rect, null, side && mirror);
    }

    // ---------------------------------------------------------------- light, bars and messages

    private static void DrawLighting(Canvas c, Adventure a, int ticks)
    {
        var p = ToScreen(a, a.Pos);
        if (!a.Outside)
        {
            // Torchlight: darker towards the edges of the view, flickering slightly.
            float flicker = 0.92f + 0.08f * MathF.Sin(ticks * 0.37f) * MathF.Sin(ticks * 0.11f);
            float dark = 0.62f * flicker;
            c.Fade(0, ViewTop, Canvas.Width, 46, Color.Black * dark, 0);
            c.Fade(0, ViewBottom - 46, Canvas.Width, 46, Color.Black * dark, 2);
            c.Fade(0, ViewTop, 60, Adventure.ViewHeight, Color.Black * dark, 3);
            c.Fade(Canvas.Width - 60, ViewTop, 60, Adventure.ViewHeight, Color.Black * dark, 1);
            c.Glow(p.X, p.Y - 4, 60, new Color(255, 170, 90) * 0.05f);
        }
        if (a.Swimming)
            c.Fill(0, ViewTop, Canvas.Width, Adventure.ViewHeight, new Color(0, 40, 90) * 0.18f);
        if (a.Flash > 0)
            c.Fill(0, ViewTop, Canvas.Width, Adventure.ViewHeight, new Color(200, 255, 250) * (a.Flash / 50f));
        if (a.Dying != DeathCause.None)
            c.Fill(0, ViewTop, Canvas.Width, Adventure.ViewHeight, new Color(90, 0, 0) * Math.Min(0.35f, a.DeathTicks / 120f));
    }

    private static void DrawTopBar(Canvas c, Adventure a, int ticks, bool demo)
    {
        c.Gradient(0, 0, Canvas.Width, TopBar, new Color(40, 30, 80), new Color(14, 12, 34));
        c.Fill(0, TopBar - 1, Canvas.Width, 1, new Color(190, 140, 50));
        string title = Strings.Subtitle;
        c.TextShadowed((Canvas.Width - Canvas.TextWidth(title)) / 2, 2, title, Gold);
        if (demo && ticks / 25 % 2 == 0)
        {
            string d = Strings.Demo;
            c.Fill(Canvas.Width - d.Length * 6 - 6, 1, d.Length * 6 + 4, 9, new Color(200, 30, 30));
            c.Text(Canvas.Width - d.Length * 6 - 4, 2, d, Color.White);
        }
        // Air, when the explorer is holding his breath.
        if ((a.Swimming && !a.HasHelmet) || a.Air < Adventure.MaxAir - 0.5f)
        {
            float w = 60, x = Canvas.Width - w - 6, y = ViewTop + 4;
            c.Fill(x - 22, y - 1, w + 25, 9, Color.Black * 0.55f);
            c.Text(x - 20, y, Strings.Air, new Color(180, 230, 255));
            float f = a.Air / Adventure.MaxAir;
            var col = f > 0.3f ? new Color(80, 200, 255) : ticks / 5 % 2 == 0 ? new Color(255, 70, 50) : new Color(255, 200, 60);
            c.Frame(x, y, w, 7, new Color(180, 230, 255), 0.6f);
            c.Fill(x + 1, y + 1, (w - 2) * f, 5, col);
        }
    }

    private static void DrawStatusBar(Canvas c, Adventure a, int ticks)
    {
        float y = ViewBottom;
        c.Gradient(0, y, Canvas.Width, Canvas.Height - y, new Color(44, 36, 26), new Color(16, 12, 10));
        c.Fill(0, y, Canvas.Width, 1, new Color(190, 140, 50));
        c.Fill(0, y + 1, Canvas.Width, 0.6f, new Color(255, 220, 120) * 0.5f);
        float iy = y + 6;

        // Lives.
        // Lives (up to five on Easy, so they close up to fit).
        float step = a.StartLives > 3 ? 7.5f : 12f;
        for (int i = 0; i < a.StartLives; i++)
            c.Draw("Life", new RectF(4 + i * step, iy, 12, 12), i < a.Lives ? Color.White : Color.White * 0.18f);

        // Laser and shots.
        float lx = 44;
        c.Draw("Pistol", new RectF(lx, iy, 12, 12), a.HasPistol ? Color.White : Color.White * 0.18f);
        string shots = a.HasPistol ? a.Ammo.ToString("D2") : "--";
        var shotInk = a.HasPistol && a.Ammo <= 3 && ticks / 10 % 2 == 0 ? new Color(255, 90, 60) : new Color(140, 255, 230);
        c.Text(lx + 14, iy + 2.5f, shots, shotInk);

        // What the explorer carries.
        float x = 80;
        void Slot(string art, bool have)
        {
            c.Fill(x - 0.5f, iy - 1.5f, 15, 15, Color.Black * 0.35f);
            if (have)
                c.Draw(art, new RectF(x, iy - 1, 14, 14));
            else
                c.Draw(art, new RectF(x, iy - 1, 14, 14), Color.Black * 0.6f);
            x += 17;
        }
        Slot("JadeKey", a.HasKey);
        Slot("Book", a.HasBook);
        Slot("Idol", a.Idols >= 1);
        Slot("Idol", a.Idols >= 2);
        Slot("Helmet", a.HasHelmet);
        Slot("Stone", a.HasStone);
        if (a.HasStone)
            c.Glow(x - 17 + 7, iy + 6, 9, new Color(120, 255, 255) * (0.25f + 0.15f * MathF.Sin(ticks * 0.15f)));

        // Score.
        string score = a.Score.ToString("D6");
        c.Text(Canvas.Width - Canvas.TextWidth(score) - 4, iy + 4, score, Gold);
        c.Text(Canvas.Width - Canvas.TextWidth(Strings.Score) - 4, iy - 4, Strings.Score, new Color(200, 170, 120));
    }

    /// <summary>Breaks text into lines of at most <paramref name="width"/> characters.</summary>
    public static List<string> Wrap(string text, int width)
    {
        var lines = new List<string>();
        var line = "";
        foreach (var word in text.Split(' '))
        {
            if (line.Length == 0)
                line = word;
            else if (line.Length + 1 + word.Length <= width)
                line += " " + word;
            else
            {
                lines.Add(line);
                line = word;
            }
        }
        if (line.Length > 0)
            lines.Add(line);
        return lines;
    }

    private static void DrawMessage(Canvas c, Adventure a, bool mobile)
    {
        if (a.Reading != null)
            return;
        if (a.Message != null)
        {
            var lines = Wrap(a.Message(), 38);
            float h = lines.Count * 9 + 5;
            float y = ViewBottom - h - 3;
            float alpha = Math.Min(1f, a.MessageTicks / 15f);
            c.Fill(4, y, Canvas.Width - 8, h, new Color(10, 8, 24) * (0.78f * alpha));
            c.Frame(4, y, Canvas.Width - 8, h, new Color(190, 140, 50) * alpha, 0.6f);
            for (int i = 0; i < lines.Count; i++)
                c.TextCenteredShadowed(y + 3 + i * 9, lines[i], Color.White * alpha);
            return;
        }
        var hint = a.ActionHint();
        if (hint != null)
        {
            string text = Strings.HintKey(mobile) + hint();
            float w = Canvas.TextWidth(text) + 6;
            float x = Canvas.Width - w - 4, y = ViewBottom - 13;
            c.Fill(x, y, w, 10, new Color(10, 8, 24) * 0.7f);
            c.Frame(x, y, w, 10, new Color(190, 140, 50) * 0.8f, 0.5f);
            c.Text(x + 3, y + 1, text, Gold);
        }
    }

    /// <summary>The panel shown while reading an inscription.</summary>
    private static void DrawInscription(Canvas c, Adventure a, int id, bool mobile)
    {
        float x = 12, y = 40, w = Canvas.Width - 24, h = 130;
        c.Fill(0, ViewTop, Canvas.Width, Adventure.ViewHeight, Color.Black * 0.55f);
        c.Draw("Tablet", new RectF(x - 4, y - 4, w + 8, h + 8), new Color(255, 240, 210));
        c.Fill(x + 6, y + 6, w - 12, h - 12, new Color(236, 214, 160) * 0.9f);
        c.Frame(x + 6, y + 6, w - 12, h - 12, new Color(120, 70, 30), 0.8f);
        var ink = new Color(70, 36, 14);
        c.TextCentered(y + 11, Strings.InscriptionHeader, new Color(140, 60, 30));

        // The signs, wrapped to fit (each is 8 wide at scale 1).
        var glyphLines = Wrap(Strings.InscriptionGlyphs(id), 24);
        float gy = y + 25;
        foreach (var line in glyphLines)
        {
            c.GlyphText((Canvas.Width - line.Length * Glyphs.Size) / 2f, gy, line, ink);
            gy += 11;
        }

        gy += 4;
        bool canRead = a.KnowsScript || id == 0;
        var text = canRead ? Strings.Inscription(id) : Strings.CannotRead;
        foreach (var line in Wrap(text, 34))
        {
            c.TextCentered(gy, line, canRead ? new Color(30, 30, 110) : new Color(130, 40, 30));
            gy += 9;
        }
        if (id == 0)
        {
            gy += 3;
            foreach (var line in Wrap(Strings.RosettaNote, 34))
            {
                c.TextCentered(gy, line, new Color(30, 110, 50));
                gy += 9;
            }
        }
        if (a.ReadingTicks > 15)
            c.TextCentered(y + h - 13, Strings.CloseReading(mobile), new Color(140, 60, 30));
    }
}
