using System;
using Raylib_cs;

namespace PongGameV2.Screens;

public class SoundSettingsScreen : IScreen
{
    private enum Row { Volume, Music }

    private static readonly Row[] Rows = [Row.Volume, Row.Music];

    private int _selectedRow;
    private float _time;

    public ScreenAction Update(float dt)
    {
        _time += dt;

        if (Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            _selectedRow = (_selectedRow - 1 + Rows.Length) % Rows.Length;
            SoundManager.Play(SoundManager.MenuMove);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            _selectedRow = (_selectedRow + 1) % Rows.Length;
            SoundManager.Play(SoundManager.MenuMove);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Left))
        {
            switch (Rows[_selectedRow])
            {
                case Row.Volume:
                    GameSettings.Volume = Math.Max(0, GameSettings.Volume - 1);
                    break;
                case Row.Music:
                    GameSettings.MusicEnabled = !GameSettings.MusicEnabled;
                    break;
            }
            SoundManager.Play(SoundManager.MenuMove);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Right))
        {
            switch (Rows[_selectedRow])
            {
                case Row.Volume:
                    GameSettings.Volume = Math.Min(10, GameSettings.Volume + 1);
                    break;
                case Row.Music:
                    GameSettings.MusicEnabled = !GameSettings.MusicEnabled;
                    break;
            }
            SoundManager.Play(SoundManager.MenuMove);
        }

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

        const string title = "SOUND";
        int titleW = Raylib.MeasureText(title, 40);
        Raylib.DrawText(title, centerX - titleW / 2, 90, 40, GameSettings.BallColor);

        // --- Volume row --- centered in available space
        {
            bool isSelected = _selectedRow == 0;
            Color labelColor = isSelected ? GameSettings.Text : GameSettings.Line;

            const string label = "VOLUME";
            int labelW = Raylib.MeasureText(label, 22);
            int y = 165;
            Raylib.DrawText(label, centerX - labelW / 2, y, 22, labelColor);

            int barY = y + 32;
            int totalBarW = 320;
            int barX = centerX - totalBarW / 2;
            int segmentW = totalBarW / 10;
            int segmentH = 22;
            int gap = 4;

            for (int i = 0; i < 10; i++)
            {
                bool filled = i < GameSettings.Volume;
                int sx = barX + i * segmentW + gap / 2;
                int sw = segmentW - gap;

                if (filled)
                {
                    float t = (float)i / 9;
                    byte r = (byte)(80 + t * 175);
                    byte g = (byte)(220 - t * 140);
                    Color segColor = new(r, g, (byte)80, (byte)255);
                    Raylib.DrawRectangleRounded(new Rectangle(sx, barY, sw, segmentH), 0.3f, 4, segColor);
                }
                else
                {
                    Raylib.DrawRectangleRounded(new Rectangle(sx, barY, sw, segmentH), 0.3f, 4, GameSettings.Line);
                }
            }

            // Value
            string val = GameSettings.Volume.ToString();
            int valW = Raylib.MeasureText(val, 28);
            Raylib.DrawText(val, centerX - valW / 2, barY + 32, 28, isSelected ? GameSettings.BallColor : GameSettings.Line);

            // Arrows
            if (isSelected)
            {
                float pulse = MathF.Sin(_time * 4f) * 3f;
                Raylib.DrawText("<", barX - 26 + (int)pulse, barY, 22, GameSettings.LeftPaddle);
                Raylib.DrawText(">", barX + totalBarW + 8 - (int)pulse, barY, 22, GameSettings.LeftPaddle);
            }
        }

        // --- Music row ---
        {
            bool isSelected = _selectedRow == 1;
            Color labelColor = isSelected ? GameSettings.Text : GameSettings.Line;
            Color valueColor = isSelected ? GameSettings.BallColor : GameSettings.Line;
            int fontSize = 22;

            const string label = "MUSIC";
            int y = 300;
            int labelW = Raylib.MeasureText(label, fontSize);
            Raylib.DrawText(label, centerX - 80, y, fontSize, labelColor);

            string value = GameSettings.MusicEnabled ? "ON" : "OFF";
            int valueW2 = Raylib.MeasureText(value, fontSize);
            int valueX = centerX + 80 - valueW2;
            Raylib.DrawText(value, valueX, y, fontSize, valueColor);

            if (isSelected)
            {
                float pulse = MathF.Sin(_time * 4f) * 3f;
                Raylib.DrawText("<", valueX - 26 + (int)pulse, y, fontSize, GameSettings.LeftPaddle);
                Raylib.DrawText(">", valueX + valueW2 + 8 - (int)pulse, y, fontSize, GameSettings.LeftPaddle);
            }
        }

        // Footer
        const string footer = "UP/DOWN select  |  LEFT/RIGHT change  |  ESC back";
        int footerW = Raylib.MeasureText(footer, 14);
        Raylib.DrawText(footer,
            centerX - footerW / 2,
            vh - 24,
            14, GameSettings.Line);
    }
}
