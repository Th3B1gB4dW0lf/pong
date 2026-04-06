using System;
using System.Numerics;
using Raylib_cs;
using PongGameV2.Core;

namespace PongGameV2.Screens;

public class ControlsScreen : IScreen
{
    private float _time;

    public ScreenAction Update(float dt)
    {
        _time += dt;

        if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Enter))
            return ScreenAction.OpenSettings;

        return ScreenAction.None;
    }

    public void Draw()
    {
        Raylib.ClearBackground(GameSettings.Background);

        int vw = GameSettings.VirtualWidth;
        int vh = GameSettings.VirtualHeight;
        int cx = vw / 2;

        const string title = "CONTROLS";
        int titleW = Raylib.MeasureText(title, 36);
        Raylib.DrawText(title, cx - titleW / 2, 20, 36, GameSettings.BallColor);

        // ── Player columns ──
        int p1X = 60;
        int p2X = cx + 30;
        int headerY = 70;

        // Vertical divider between players
        Raylib.DrawLineEx(new Vector2(cx + 10, headerY - 5), new Vector2(cx + 10, 225), 1f, GameSettings.Line);

        // Player 1
        DrawPlayerHeader(p1X, headerY, "PLAYER 1", GameSettings.LeftPaddle);
        int y1 = headerY + 30;
        DrawKeyRow(p1X, y1, "Move Up", "W", GameSettings.LeftPaddle); y1 += 26;
        DrawKeyRow(p1X, y1, "Move Down", "S", GameSettings.LeftPaddle); y1 += 26;
        DrawKeyRow(p1X, y1, "Upper Swing", "Q", GameSettings.LeftPaddle); y1 += 26;
        DrawKeyRow(p1X, y1, "Lower Swing", "E", GameSettings.LeftPaddle);

        // Player 2
        DrawPlayerHeader(p2X, headerY, "PLAYER 2", GameSettings.RightPaddle);
        int y2 = headerY + 30;
        DrawKeyRowWithArrow(p2X, y2, "Move Up", true, GameSettings.RightPaddle); y2 += 26;
        DrawKeyRowWithArrow(p2X, y2, "Move Down", false, GameSettings.RightPaddle); y2 += 26;
        DrawKeyRow(p2X, y2, "Upper Swing", ".", GameSettings.RightPaddle); y2 += 26;
        DrawKeyRow(p2X, y2, "Lower Swing", "/", GameSettings.RightPaddle);

        // ── General section ──
        int genY = 240;
        Raylib.DrawLineEx(new Vector2(60, genY), new Vector2(vw - 60, genY), 1f, GameSettings.Line);

        const string genTitle = "GENERAL";
        int genTitleW = Raylib.MeasureText(genTitle, 22);
        Raylib.DrawText(genTitle, cx - genTitleW / 2, genY + 10, 22, GameSettings.BallColor);

        // Single centered column for general controls
        int gx = cx - 120;
        int gy = genY + 38;
        DrawKeyRow(gx, gy, "Pause", "SPACE", GameSettings.Text); gy += 26;
        DrawKeyRow(gx, gy, "Restart", "ENTER", GameSettings.Text); gy += 26;
        DrawKeyRow(gx, gy, "Back / Quit", "ESC", GameSettings.Text); gy += 26;
        DrawKeyRowWithUpDown(gx, gy, "Navigate Menus", GameSettings.Text);

        // Footer
        const string footer = "ESC to go back";
        int footerW = Raylib.MeasureText(footer, 14);
        Raylib.DrawText(footer, cx - footerW / 2, vh - 24, 14, GameSettings.Line);
    }

    private static void DrawPlayerHeader(int x, int y, string text, Color color)
    {
        // Small paddle icon
        Raylib.DrawRectangleRounded(new Rectangle(x, y + 2, 5, 18), 0.4f, 4, color);
        Raylib.DrawText(text, x + 14, y, 20, color);
    }

    private static void DrawKeyRow(int x, int y, string action, string key, Color accentColor)
    {
        int fontSize = 15;
        Raylib.DrawText(action, x, y, fontSize, GameSettings.Text);

        int keyW = Raylib.MeasureText(key, fontSize);
        int boxX = x + 160;
        int boxW = keyW + 14;
        int boxH = fontSize + 6;

        Raylib.DrawRectangleRounded(
            new Rectangle(boxX, y - 3, boxW, boxH),
            0.3f, 4, new Color((byte)35, (byte)35, (byte)55, (byte)255));
        Raylib.DrawRectangleRoundedLines(
            new Rectangle(boxX, y - 3, boxW, boxH),
            0.3f, 4, GameSettings.Line);
        Raylib.DrawText(key, boxX + 7, y, fontSize,
            new Color(accentColor.R, accentColor.G, accentColor.B, (byte)220));
    }

    /// <summary>Draw an up/down arrow glyph instead of unicode text.</summary>
    private static void DrawKeyRowWithArrow(int x, int y, string action, bool up, Color accentColor)
    {
        int fontSize = 15;
        Raylib.DrawText(action, x, y, fontSize, GameSettings.Text);

        int boxX = x + 160;
        int boxW = 28;
        int boxH = fontSize + 6;
        int arrowCx = boxX + boxW / 2;
        int arrowCy = y + boxH / 2 - 3;

        Raylib.DrawRectangleRounded(
            new Rectangle(boxX, y - 3, boxW, boxH),
            0.3f, 4, new Color((byte)35, (byte)35, (byte)55, (byte)255));
        Raylib.DrawRectangleRoundedLines(
            new Rectangle(boxX, y - 3, boxW, boxH),
            0.3f, 4, GameSettings.Line);

        Color col = new(accentColor.R, accentColor.G, accentColor.B, (byte)220);
        if (up)
        {
            // Up arrow glyph
            Raylib.DrawTriangle(
                new Vector2(arrowCx, arrowCy - 4),
                new Vector2(arrowCx - 5, arrowCy + 2),
                new Vector2(arrowCx + 5, arrowCy + 2), col);
            Raylib.DrawRectangle(arrowCx - 2, arrowCy + 2, 4, 5, col);
        }
        else
        {
            // Down arrow glyph
            Raylib.DrawRectangle(arrowCx - 2, arrowCy - 4, 4, 5, col);
            Raylib.DrawTriangle(
                new Vector2(arrowCx, arrowCy + 5),
                new Vector2(arrowCx + 5, arrowCy - 1),
                new Vector2(arrowCx - 5, arrowCy - 1), col);
        }
    }

    /// <summary>Draw UP/DOWN + ENTER key row using arrow glyphs.</summary>
    private static void DrawKeyRowWithUpDown(int x, int y, string action, Color accentColor)
    {
        int fontSize = 15;
        Raylib.DrawText(action, x, y, fontSize, GameSettings.Text);

        int boxX = x + 160;
        int boxH = fontSize + 6;
        Color col = new(accentColor.R, accentColor.G, accentColor.B, (byte)220);
        Color bg = new((byte)35, (byte)35, (byte)55, (byte)255);

        // Up arrow box
        Raylib.DrawRectangleRounded(new Rectangle(boxX, y - 3, 22, boxH), 0.3f, 4, bg);
        Raylib.DrawRectangleRoundedLines(new Rectangle(boxX, y - 3, 22, boxH), 0.3f, 4, GameSettings.Line);
        int acx = boxX + 11, acy = y + boxH / 2 - 3;
        Raylib.DrawTriangle(new Vector2(acx, acy - 3), new Vector2(acx - 4, acy + 2), new Vector2(acx + 4, acy + 2), col);
        Raylib.DrawRectangle(acx - 1, acy + 2, 3, 3, col);

        // Down arrow box
        int bx = boxX + 26;
        Raylib.DrawRectangleRounded(new Rectangle(bx, y - 3, 22, boxH), 0.3f, 4, bg);
        Raylib.DrawRectangleRoundedLines(new Rectangle(bx, y - 3, 22, boxH), 0.3f, 4, GameSettings.Line);
        int bcx = bx + 11;
        Raylib.DrawRectangle(bcx - 1, acy - 3, 3, 3, col);
        Raylib.DrawTriangle(new Vector2(bcx, acy + 4), new Vector2(bcx + 4, acy - 1), new Vector2(bcx - 4, acy - 1), col);
    }
}
