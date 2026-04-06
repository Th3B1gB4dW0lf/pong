using Raylib_cs;
using PongGameV2.Core;

namespace PongGameV2.Gameplay;

public static class Field
{
    public static void Draw()
    {
        const int dashHeight = 12;
        const int gap = 10;
        for (int y = 0; y < GameSettings.VirtualHeight; y += dashHeight + gap)
        {
            Raylib.DrawRectangle(GameSettings.VirtualWidth / 2 - 2, y, 4, dashHeight, GameSettings.Line);
        }

        Raylib.DrawCircleLines(GameSettings.VirtualWidth / 2, GameSettings.VirtualHeight / 2, 60, GameSettings.Line);
    }

    public static void DrawScores(int leftScore, int rightScore)
    {
        const int fontSize = 120;

        string ls = leftScore.ToString();
        int lsW = Raylib.MeasureText(ls, fontSize);
        Raylib.DrawText(ls, GameSettings.VirtualWidth / 4 - lsW / 2, 30, fontSize, GameSettings.Score);

        string rs = rightScore.ToString();
        int rsW = Raylib.MeasureText(rs, fontSize);
        Raylib.DrawText(rs, 3 * GameSettings.VirtualWidth / 4 - rsW / 2, 30, fontSize, GameSettings.Score);
    }
}
