using System;
using Raylib_cs;

namespace PongGameV2.Screens;

public class SoundSettingsScreen : IScreen
{
    private enum Row { Master, Sfx, Music, MusicToggle }

    private static readonly Row[] Rows = [Row.Master, Row.Sfx, Row.Music, Row.MusicToggle];

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
                case Row.Master:
                    GameSettings.MasterVolume = Math.Max(0, GameSettings.MasterVolume - 1);
                    break;
                case Row.Sfx:
                    GameSettings.SfxVolume = Math.Max(0, GameSettings.SfxVolume - 1);
                    break;
                case Row.Music:
                    GameSettings.MusicVolume = Math.Max(0, GameSettings.MusicVolume - 1);
                    break;
                case Row.MusicToggle:
                    GameSettings.MusicEnabled = !GameSettings.MusicEnabled;
                    break;
            }
            SoundManager.Play(SoundManager.MenuMove);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Right))
        {
            switch (Rows[_selectedRow])
            {
                case Row.Master:
                    GameSettings.MasterVolume = Math.Min(10, GameSettings.MasterVolume + 1);
                    break;
                case Row.Sfx:
                    GameSettings.SfxVolume = Math.Min(10, GameSettings.SfxVolume + 1);
                    break;
                case Row.Music:
                    GameSettings.MusicVolume = Math.Min(10, GameSettings.MusicVolume + 1);
                    break;
                case Row.MusicToggle:
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
        Raylib.DrawText(title, centerX - titleW / 2, 50, 40, GameSettings.BallColor);

        // Draw three volume sliders and a music toggle
        DrawVolumeSlider(centerX, 110, "MASTER", GameSettings.MasterVolume, _selectedRow == 0);
        DrawVolumeSlider(centerX, 185, "SFX", GameSettings.SfxVolume, _selectedRow == 1);
        DrawVolumeSlider(centerX, 260, "MUSIC", GameSettings.MusicVolume, _selectedRow == 2);
        DrawMusicToggle(centerX, 340, _selectedRow == 3);

        // Footer
        const string footer = "UP/DOWN select  |  LEFT/RIGHT change  |  ESC back";
        int footerW = Raylib.MeasureText(footer, 14);
        Raylib.DrawText(footer, centerX - footerW / 2, vh - 24, 14, GameSettings.Line);
    }

    private void DrawVolumeSlider(int centerX, int y, string label, int value, bool isSelected)
    {
        Color labelColor = isSelected ? GameSettings.Text : GameSettings.Line;

        int labelW = Raylib.MeasureText(label, 20);
        Raylib.DrawText(label, centerX - labelW / 2, y, 20, labelColor);

        int barY = y + 26;
        int totalBarW = 300;
        int barX = centerX - totalBarW / 2;
        int segmentW = totalBarW / 10;
        int segmentH = 18;
        int gap = 4;

        for (int i = 0; i < 10; i++)
        {
            bool filled = i < value;
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
        string val = value.ToString();
        int valW = Raylib.MeasureText(val, 22);
        Raylib.DrawText(val, centerX - valW / 2, barY + 24, 22, isSelected ? GameSettings.BallColor : GameSettings.Line);

        // Arrows
        if (isSelected)
        {
            float pulse = MathF.Sin(_time * 4f) * 3f;
            Raylib.DrawText("<", barX - 24 + (int)pulse, barY, 18, GameSettings.LeftPaddle);
            Raylib.DrawText(">", barX + totalBarW + 6 - (int)pulse, barY, 18, GameSettings.LeftPaddle);
        }
    }

    private void DrawMusicToggle(int centerX, int y, bool isSelected)
    {
        Color labelColor = isSelected ? GameSettings.Text : GameSettings.Line;
        Color valueColor = isSelected ? GameSettings.BallColor : GameSettings.Line;
        int fontSize = 20;

        const string label = "MUSIC";
        Raylib.DrawText(label, centerX - 80, y, fontSize, labelColor);

        string value = GameSettings.MusicEnabled ? "ON" : "OFF";
        int valueW = Raylib.MeasureText(value, fontSize);
        int valueX = centerX + 80 - valueW;
        Raylib.DrawText(value, valueX, y, fontSize, valueColor);

        if (isSelected)
        {
            float pulse = MathF.Sin(_time * 4f) * 3f;
            Raylib.DrawText("<", valueX - 26 + (int)pulse, y, fontSize, GameSettings.LeftPaddle);
            Raylib.DrawText(">", valueX + valueW + 8 - (int)pulse, y, fontSize, GameSettings.LeftPaddle);
        }
    }
}
