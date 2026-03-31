using System;
using Raylib_cs;

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
        int centerX = vw / 2;

        const string title = "CONTROLS";
        int titleW = Raylib.MeasureText(title, 40);
        Raylib.DrawText(title, centerX - titleW / 2, 30, 40, GameSettings.BallColor);

        // Player 1 section
        int p1X = centerX - 200;
        int p2X = centerX + 60;
        int headerY = 85;

        // Divider line
        Raylib.DrawLineEx(
            new System.Numerics.Vector2(centerX - 10, headerY - 5),
            new System.Numerics.Vector2(centerX - 10, vh - 50),
            1f, GameSettings.Line);

        // Player 1
        DrawPlayerHeader(p1X, headerY, "PLAYER 1", GameSettings.LeftPaddle);
        int y = headerY + 35;
        DrawKeyBinding(p1X, ref y, "Move Up", "W");
        DrawKeyBinding(p1X, ref y, "Move Down", "S");
        DrawKeyBinding(p1X, ref y, "Upper Swing", "Q");
        DrawKeyBinding(p1X, ref y, "Lower Swing", "E");

        // Player 2
        DrawPlayerHeader(p2X, headerY, "PLAYER 2", GameSettings.RightPaddle);
        y = headerY + 35;
        DrawKeyBinding(p2X, ref y, "Move Up", "\u2191");  // ↑
        DrawKeyBinding(p2X, ref y, "Move Down", "\u2193"); // ↓
        DrawKeyBinding(p2X, ref y, "Upper Swing", ".");
        DrawKeyBinding(p2X, ref y, "Lower Swing", "/");

        // General controls
        int genY = 280;
        Raylib.DrawLineEx(
            new System.Numerics.Vector2(centerX - 200, genY - 10),
            new System.Numerics.Vector2(centerX + 200, genY - 10),
            1f, GameSettings.Line);

        const string genTitle = "GENERAL";
        int genTitleW = Raylib.MeasureText(genTitle, 22);
        Raylib.DrawText(genTitle, centerX - genTitleW / 2, genY, 22, GameSettings.BallColor);

        int gx = centerX - 140;
        int gy = genY + 30;
        DrawKeyBinding(gx, ref gy, "Pause", "SPACE");
        DrawKeyBinding(gx, ref gy, "Restart (Game Over)", "ENTER / SPACE");
        DrawKeyBinding(gx, ref gy, "Back / Quit", "ESC");
        DrawKeyBinding(gx, ref gy, "Navigate Menus", "\u2191 \u2193 + ENTER");

        // Footer
        const string footer = "ESC to go back";
        int footerW = Raylib.MeasureText(footer, 14);
        Raylib.DrawText(footer, centerX - footerW / 2, vh - 24, 14, GameSettings.Line);
    }

    private static void DrawPlayerHeader(int x, int y, string text, Color color)
    {
        // Small paddle icon
        Raylib.DrawRectangleRounded(new Rectangle(x - 2, y + 2, 6, 20), 0.4f, 4, color);

        int tw = Raylib.MeasureText(text, 22);
        Raylib.DrawText(text, x + 12, y, 22, color);
    }

    private static void DrawKeyBinding(int x, ref int y, string action, string key)
    {
        int fontSize = 16;
        Color actionColor = GameSettings.Text;
        Color keyColor = new((byte)120, (byte)200, (byte)255, (byte)255);

        Raylib.DrawText(action, x, y, fontSize, actionColor);

        // Draw key in a box
        int keyW = Raylib.MeasureText(key, fontSize);
        int boxX = x + 140;
        int boxW = keyW + 12;

        Raylib.DrawRectangleRounded(
            new Rectangle(boxX - 2, y - 2, boxW, fontSize + 4),
            0.3f, 4, new Color((byte)40, (byte)40, (byte)60, (byte)255));
        Raylib.DrawRectangleRoundedLines(
            new Rectangle(boxX - 2, y - 2, boxW, fontSize + 4),
            0.3f, 4, GameSettings.Line);
        Raylib.DrawText(key, boxX + 4, y, fontSize, keyColor);

        y += 28;
    }
}
