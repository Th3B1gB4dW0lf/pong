using System;
using System.Numerics;
using Raylib_cs;

namespace PongGameV2.Screens;

public class HowToPlayScreen : IScreen
{
    private int _page;
    private const int TotalPages = 4;
    private float _time;
    private float _pageTransition; // 0 = stable, animates on page change

    public ScreenAction Update(float dt)
    {
        _time += dt;
        _pageTransition = MathF.Max(0, _pageTransition - dt * 5f);

        if (Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            if (_page < TotalPages - 1)
            {
                _page++;
                _pageTransition = 1f;
                SoundManager.Play(SoundManager.MenuMove);
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Left))
        {
            if (_page > 0)
            {
                _page--;
                _pageTransition = 1f;
                SoundManager.Play(SoundManager.MenuMove);
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            return ScreenAction.BackToMenu;

        return ScreenAction.None;
    }

    public void Draw()
    {
        Raylib.ClearBackground(GameSettings.Background);

        int vw = GameSettings.VirtualWidth;
        int vh = GameSettings.VirtualHeight;

        // Content area with fade-in
        float alpha = 1f - _pageTransition;
        byte contentAlpha = (byte)(alpha * 255);

        switch (_page)
        {
            case 0: DrawPageBasics(vw, vh, contentAlpha); break;
            case 1: DrawPageSwinging(vw, vh, contentAlpha); break;
            case 2: DrawPagePowerUps(vw, vh, contentAlpha); break;
            case 3: DrawPageGoal(vw, vh, contentAlpha); break;
        }

        // Page indicator dots
        int dotY = vh - 44;
        int totalDotsW = TotalPages * 18;
        int dotStartX = vw / 2 - totalDotsW / 2;
        for (int i = 0; i < TotalPages; i++)
        {
            bool active = i == _page;
            Color dotColor = active ? GameSettings.LeftPaddle : GameSettings.Line;
            float r = active ? 5f : 3f;
            Raylib.DrawCircleV(new Vector2(dotStartX + i * 18 + 9, dotY), r, dotColor);
        }

        // Footer
        string footer = _page == 0
            ? "ENTER / \u2192 next  |  ESC back to menu"
            : _page == TotalPages - 1
                ? "\u2190 prev  |  ESC back to menu"
                : "\u2190 prev  |  \u2192 next  |  ESC back to menu";
        int footerW = Raylib.MeasureText(footer, 14);
        Raylib.DrawText(footer, vw / 2 - footerW / 2, vh - 22, 14, GameSettings.Line);
    }

    // ── Page 1: The Basics ──────────────────────────────────

    private void DrawPageBasics(int vw, int vh, byte a)
    {
        Color title = WithAlpha(GameSettings.BallColor, a);
        Color text = WithAlpha(GameSettings.Text, a);
        Color dim = WithAlpha(GameSettings.Line, a);
        int cx = vw / 2;

        DrawPageTitle(cx, "THE BASICS", title);

        // Mini field illustration
        int fieldX = cx - 140;
        int fieldY = 80;
        int fieldW = 280;
        int fieldH = 140;

        // Field background
        Raylib.DrawRectangle(fieldX, fieldY, fieldW, fieldH, WithAlpha(new Color((byte)25, (byte)25, (byte)40, (byte)255), a));
        Raylib.DrawRectangleLines(fieldX, fieldY, fieldW, fieldH, dim);

        // Center line
        for (int dy = 0; dy < fieldH; dy += 10)
            Raylib.DrawRectangle(cx - 1, fieldY + dy, 2, 5, dim);

        // Left paddle (animated)
        float p1Y = fieldY + fieldH / 2f + MathF.Sin(_time * 2f) * 30f;
        Color p1Color = WithAlpha(GameSettings.LeftPaddle, a);
        Raylib.DrawRectangleRounded(new Rectangle(fieldX + 12, p1Y - 18, 6, 36), 0.3f, 4, p1Color);

        // Right paddle (animated)
        float p2Y = fieldY + fieldH / 2f + MathF.Sin(_time * 2f + 1f) * 25f;
        Color p2Color = WithAlpha(GameSettings.RightPaddle, a);
        Raylib.DrawRectangleRounded(new Rectangle(fieldX + fieldW - 18, p2Y - 18, 6, 36), 0.3f, 4, p2Color);

        // Ball (animated bouncing)
        float ballX = cx + MathF.Sin(_time * 1.5f) * 80f;
        float ballY = fieldY + fieldH / 2f + MathF.Cos(_time * 2.3f) * 40f;
        Raylib.DrawCircleV(new Vector2(ballX, ballY), 5, WithAlpha(Color.White, a));

        // Key labels
        int labelY = fieldY + fieldH + 18;

        // P1 keys
        DrawMiniKey(fieldX + 10, labelY, "W", p1Color);
        DrawMiniKey(fieldX + 35, labelY, "S", p1Color);
        Raylib.DrawText("P1 Move", fieldX + 60, labelY + 2, 14, text);

        // P2 keys
        DrawMiniKey(fieldX + fieldW - 50, labelY, "\u2191", p2Color);
        DrawMiniKey(fieldX + fieldW - 25, labelY, "\u2193", p2Color);
        Raylib.DrawText("P2 Move", fieldX + fieldW - 115, labelY + 2, 14, text);

        // Description
        int descY = labelY + 36;
        DrawCenteredText(cx, descY, "Move your paddle up and down", 16, text);
        DrawCenteredText(cx, descY + 22, "to hit the ball back to your opponent.", 16, text);
        DrawCenteredText(cx, descY + 52, "If the ball passes your paddle,", 16, dim);
        DrawCenteredText(cx, descY + 72, "the other player scores a point!", 16, dim);
    }

    // ── Page 2: Swinging ────────────────────────────────────

    private void DrawPageSwinging(int vw, int vh, byte a)
    {
        Color title = WithAlpha(GameSettings.BallColor, a);
        Color text = WithAlpha(GameSettings.Text, a);
        Color dim = WithAlpha(GameSettings.Line, a);
        Color accent = WithAlpha(new Color((byte)255, (byte)200, (byte)60, (byte)255), a);
        int cx = vw / 2;

        DrawPageTitle(cx, "SWING ATTACK", title);

        // Show paddle with swing animation
        int demoX = cx;
        int demoY = 145;

        // Paddle with swing rotation
        float swingAngle = MathF.Sin(_time * 3f) * 20f;
        Rlgl.PushMatrix();
        Rlgl.Translatef(demoX - 80, demoY, 0);
        Rlgl.Rotatef(swingAngle, 0, 0, 1);
        Rlgl.Translatef(-(demoX - 80), -demoY, 0);
        Raylib.DrawRectangleRounded(
            new Rectangle(demoX - 80 - 5, demoY - 30, 10, 60),
            0.3f, 4, WithAlpha(GameSettings.LeftPaddle, a));
        Rlgl.PopMatrix();

        // Ball in fire mode
        float fireFlicker = 0.85f + (MathF.Sin(_time * 8f) * 0.5f + 0.5f) * 0.15f;
        Vector2 fireBallPos = new(demoX + 40, demoY);
        // Fire glow
        Raylib.DrawCircleV(fireBallPos, 16f * fireFlicker, WithAlpha(new Color((byte)255, (byte)120, (byte)20, (byte)40), a));
        Raylib.DrawCircleV(fireBallPos, 10f, WithAlpha(new Color((byte)255, (byte)240, (byte)180, (byte)255), a));
        Raylib.DrawCircleV(fireBallPos, 6f, WithAlpha(Color.White, a));

        // Arrow showing speed
        for (int i = 0; i < 3; i++)
        {
            float ax = demoX + 60 + i * 14;
            byte arrowA = (byte)(a * (0.3f + i * 0.3f));
            Raylib.DrawTriangle(
                new Vector2(ax + 8, demoY),
                new Vector2(ax, demoY + 5),
                new Vector2(ax, demoY - 5),
                WithAlpha(accent, arrowA));
        }

        // Key labels
        int keyY = demoY + 50;
        DrawMiniKey(cx - 110, keyY, "Q", WithAlpha(GameSettings.LeftPaddle, a));
        Raylib.DrawText("Upper Swing (P1)", cx - 82, keyY + 2, 14, text);
        DrawMiniKey(cx - 110, keyY + 26, "E", WithAlpha(GameSettings.LeftPaddle, a));
        Raylib.DrawText("Lower Swing (P1)", cx - 82, keyY + 28, 14, text);

        DrawMiniKey(cx + 50, keyY, ".", WithAlpha(GameSettings.RightPaddle, a));
        Raylib.DrawText("Upper (P2)", cx + 78, keyY + 2, 14, text);
        DrawMiniKey(cx + 50, keyY + 26, "/", WithAlpha(GameSettings.RightPaddle, a));
        Raylib.DrawText("Lower (P2)", cx + 78, keyY + 28, 14, text);

        // Description
        int descY = keyY + 66;
        DrawCenteredText(cx, descY, "Time your swing to hit the ball with extra power!", 16, text);
        DrawCenteredText(cx, descY + 24, "Swing hits send the ball faster and", 16, dim);
        DrawCenteredText(cx, descY + 44, "set it on FIRE for 2 seconds!", 16, accent);
    }

    // ── Page 3: Power-Ups ───────────────────────────────────

    private void DrawPagePowerUps(int vw, int vh, byte a)
    {
        Color title = WithAlpha(GameSettings.BallColor, a);
        Color text = WithAlpha(GameSettings.Text, a);
        Color dim = WithAlpha(GameSettings.Line, a);
        Color green = WithAlpha(new Color((byte)80, (byte)220, (byte)80, (byte)255), a);
        Color red = WithAlpha(new Color((byte)255, (byte)80, (byte)80, (byte)255), a);
        int cx = vw / 2;

        DrawPageTitle(cx, "POWER-UPS", title);

        int startY = 78;
        int spacing = 82;

        // Power-up entries
        DrawPowerUpEntry(cx, startY, green,
            DrawMiniUpArrow, "GROW", "Increases YOUR paddle size by 30%", a);

        DrawPowerUpEntry(cx, startY + spacing, red,
            DrawMiniDownArrow, "SHRINK", "Decreases OPPONENT paddle size by 30%", a);

        DrawPowerUpEntry(cx, startY + spacing * 2, green,
            DrawMiniRightArrow, "SPEED UP", "Increases YOUR paddle speed by 30%", a);

        DrawPowerUpEntry(cx, startY + spacing * 3, red,
            DrawMiniLeftArrow, "SLOW DOWN", "Decreases OPPONENT paddle speed by 30%", a);

        // Bottom text
        int descY = startY + spacing * 4 + 8;
        DrawCenteredText(cx, descY, "Hit the ball through a power-up to collect it!", 15, text);
        DrawCenteredText(cx, descY + 20, "Effects last 10 seconds.", 15, dim);
    }

    private void DrawPowerUpEntry(int cx, int y, Color col,
        Action<int, int, Color> drawIcon, string name, string desc, byte a)
    {
        Color text = WithAlpha(GameSettings.Text, a);
        Color dim = WithAlpha(GameSettings.Line, a);

        // Icon circle
        int iconX = cx - 160;
        int iconY = y + 12;
        float pulse = 0.9f + MathF.Sin(_time * 4f) * 0.1f;
        float r = 12f * pulse;

        // Glow
        for (int i = 3; i >= 1; i--)
        {
            byte ga = (byte)Math.Min((int)30 / i, a);
            Raylib.DrawCircleV(new Vector2(iconX, iconY), r + i * 4f, new Color(col.R, col.G, col.B, ga));
        }
        // Background
        byte bgA = (byte)Math.Min(200, (int)a);
        Color bg = col.R > 200
            ? new Color((byte)60, (byte)20, (byte)20, bgA)
            : new Color((byte)20, (byte)60, (byte)20, bgA);
        Raylib.DrawCircleV(new Vector2(iconX, iconY), r, bg);
        Raylib.DrawCircleLinesV(new Vector2(iconX, iconY), r, col);

        // Arrow icon
        drawIcon(iconX, iconY, col);

        // Name and description
        Raylib.DrawText(name, cx - 130, y + 2, 20, col);
        Raylib.DrawText(desc, cx - 130, y + 24, 14, dim);
    }

    // ── Page 4: Goal ────────────────────────────────────────

    private void DrawPageGoal(int vw, int vh, byte a)
    {
        Color title = WithAlpha(GameSettings.BallColor, a);
        Color text = WithAlpha(GameSettings.Text, a);
        Color dim = WithAlpha(GameSettings.Line, a);
        Color gold = WithAlpha(new Color((byte)255, (byte)220, (byte)50, (byte)255), a);
        int cx = vw / 2;

        DrawPageTitle(cx, "WIN THE MATCH", title);

        // Trophy graphic
        int trophyX = cx;
        int trophyY = 130;

        // Trophy cup
        Color trophyColor = WithAlpha(new Color((byte)255, (byte)200, (byte)50, (byte)255), a);
        float trophyPulse = 1f + MathF.Sin(_time * 2f) * 0.04f;

        // Cup body
        Raylib.DrawRectangleRounded(
            new Rectangle(trophyX - 22 * trophyPulse, trophyY - 20 * trophyPulse, 44 * trophyPulse, 35 * trophyPulse),
            0.3f, 6, trophyColor);
        // Cup base/stem
        Raylib.DrawRectangle((int)(trophyX - 6 * trophyPulse), (int)(trophyY + 15 * trophyPulse), (int)(12 * trophyPulse), (int)(12 * trophyPulse), trophyColor);
        Raylib.DrawRectangleRounded(
            new Rectangle(trophyX - 16 * trophyPulse, trophyY + 25 * trophyPulse, 32 * trophyPulse, 6 * trophyPulse),
            0.4f, 4, trophyColor);
        // Cup handles
        Raylib.DrawRing(
            new Vector2(trophyX - 22 * trophyPulse, trophyY), 6, 9, 90, 270, 12,
            trophyColor);
        Raylib.DrawRing(
            new Vector2(trophyX + 22 * trophyPulse, trophyY), 6, 9, -90, 90, 12,
            trophyColor);

        // Star
        float starAngle = _time * 0.5f;
        DrawStar(trophyX, trophyY - 2, 8f * trophyPulse, starAngle,
            WithAlpha(new Color((byte)100, (byte)60, (byte)0, (byte)255), a));

        // Glow behind trophy
        Raylib.DrawCircleV(new Vector2(trophyX, trophyY + 5), 50f, WithAlpha(new Color((byte)255, (byte)200, (byte)50, (byte)15), a));

        // Score display example
        int scoreY = trophyY + 60;
        string scoreEx = "First to reach the target score wins!";
        DrawCenteredText(cx, scoreY, scoreEx, 16, text);

        // Score options illustration
        int optY = scoreY + 35;
        string[] scores = ["3", "5", "7"];
        int totalW = scores.Length * 50;
        int startX = cx - totalW / 2;
        for (int i = 0; i < scores.Length; i++)
        {
            int sx = startX + i * 50;
            bool highlight = i == 1; // highlight 5
            Color numColor = highlight ? gold : dim;
            int fontSize = highlight ? 36 : 28;
            int sw = Raylib.MeasureText(scores[i], fontSize);
            Raylib.DrawText(scores[i], sx + 25 - sw / 2, optY, fontSize, numColor);
        }
        DrawCenteredText(cx, optY + 42, "Choose 3, 5, or 7 points before the match.", 14, dim);

        // Tips
        int tipY = optY + 72;
        Raylib.DrawLineEx(new Vector2(cx - 150, tipY - 8), new Vector2(cx + 150, tipY - 8), 1, dim);
        DrawCenteredText(cx, tipY, "TIPS", 18, gold);
        DrawCenteredText(cx, tipY + 24, "Use swings to surprise your opponent!", 14, text);
        DrawCenteredText(cx, tipY + 42, "Collect power-ups to gain an advantage.", 14, text);
        DrawCenteredText(cx, tipY + 60, "The ball speeds up with every hit - stay alert!", 14, dim);
    }

    // ── Helpers ──────────────────────────────────────────────

    private static void DrawPageTitle(int cx, string text, Color color)
    {
        int w = Raylib.MeasureText(text, 32);
        Raylib.DrawText(text, cx - w / 2, 20, 32, color);

        // Underline
        Raylib.DrawLineEx(
            new Vector2(cx - w / 2f - 10, 56),
            new Vector2(cx + w / 2f + 10, 56),
            2f, color);
    }

    private static void DrawCenteredText(int cx, int y, string text, int fontSize, Color color)
    {
        int w = Raylib.MeasureText(text, fontSize);
        Raylib.DrawText(text, cx - w / 2, y, fontSize, color);
    }

    private static void DrawMiniKey(int x, int y, string key, Color color)
    {
        int keyW = Raylib.MeasureText(key, 14);
        int boxW = Math.Max(keyW + 10, 22);
        Raylib.DrawRectangleRounded(
            new Rectangle(x, y, boxW, 20), 0.3f, 4,
            new Color((byte)40, (byte)40, (byte)60, (byte)255));
        Raylib.DrawRectangleRoundedLines(
            new Rectangle(x, y, boxW, 20), 0.3f, 4, color);
        Raylib.DrawText(key, x + (boxW - keyW) / 2, y + 3, 14, color);
    }

    private static void DrawMiniUpArrow(int cx, int cy, Color col)
    {
        Raylib.DrawTriangle(
            new Vector2(cx, cy - 6), new Vector2(cx - 4, cy + 1), new Vector2(cx + 4, cy + 1), col);
        Raylib.DrawRectangle(cx - 1, cy + 1, 3, 4, col);
    }

    private static void DrawMiniDownArrow(int cx, int cy, Color col)
    {
        Raylib.DrawRectangle(cx - 1, cy - 5, 3, 4, col);
        Raylib.DrawTriangle(
            new Vector2(cx, cy + 6), new Vector2(cx + 4, cy - 1), new Vector2(cx - 4, cy - 1), col);
    }

    private static void DrawMiniRightArrow(int cx, int cy, Color col)
    {
        Raylib.DrawRectangle(cx - 5, cy - 1, 6, 3, col);
        Raylib.DrawTriangle(
            new Vector2(cx + 6, cy), new Vector2(cx + 1, cy + 4), new Vector2(cx + 1, cy - 4), col);
    }

    private static void DrawMiniLeftArrow(int cx, int cy, Color col)
    {
        Raylib.DrawRectangle(cx - 1, cy - 1, 6, 3, col);
        Raylib.DrawTriangle(
            new Vector2(cx - 6, cy), new Vector2(cx - 1, cy - 4), new Vector2(cx - 1, cy + 4), col);
    }

    private static void DrawStar(int cx, int cy, float size, float rotation, Color color)
    {
        // Simple 5-pointed star
        for (int i = 0; i < 5; i++)
        {
            float angle1 = rotation + i * MathF.PI * 2f / 5f - MathF.PI / 2f;
            float angle2 = rotation + (i + 2) * MathF.PI * 2f / 5f - MathF.PI / 2f;
            Vector2 p1 = new(cx + MathF.Cos(angle1) * size, cy + MathF.Sin(angle1) * size);
            Vector2 p2 = new(cx + MathF.Cos(angle2) * size, cy + MathF.Sin(angle2) * size);
            Raylib.DrawLineEx(p1, p2, 2f, color);
        }
    }

    private static Color WithAlpha(Color c, byte a)
    {
        byte effectiveA = (byte)Math.Min((int)c.A, (int)a);
        return new Color(c.R, c.G, c.B, effectiveA);
    }
}
