using System;
using Raylib_cs;
using PongGameV2.Audio;
using PongGameV2.Core;

namespace PongGameV2.Screens;

public class GraphicsSettingsScreen : IScreen
{
    private enum Row { DisplayMode, Resolution, Player1Color, Player2Color }

    private static readonly Row[] Rows = [Row.DisplayMode, Row.Resolution, Row.Player1Color, Row.Player2Color];

    private int _selectedRow;
    private int _resolutionIndex;
    private int _leftColorIndex;
    private int _rightColorIndex;
    private bool _fullscreen;
    private float _time;

    public GraphicsSettingsScreen()
    {
        _fullscreen = GameSettings.IsFullscreen;

        var res = GameSettings.Resolutions;
        for (int i = 0; i < res.Length; i++)
            if (res[i].W == GameSettings.ScreenWidth && res[i].H == GameSettings.ScreenHeight)
                _resolutionIndex = i;

        _leftColorIndex = FindColorIndex(GameSettings.LeftPaddle);
        _rightColorIndex = FindColorIndex(GameSettings.RightPaddle);
    }

    private static int FindColorIndex(Color c)
    {
        var presets = GameSettings.ColorPresets;
        for (int i = 0; i < presets.Length; i++)
            if (presets[i].Color.R == c.R && presets[i].Color.G == c.G && presets[i].Color.B == c.B)
                return i;
        return 0;
    }

    public ScreenAction Update(float dt)
    {
        _time += dt;
        var presets = GameSettings.ColorPresets;
        var res = GameSettings.Resolutions;

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

        bool changed = false;

        if (Raylib.IsKeyPressed(KeyboardKey.Left))
        {
            switch (Rows[_selectedRow])
            {
                case Row.DisplayMode:
                    _fullscreen = !_fullscreen;
                    break;
                case Row.Resolution:
                    _resolutionIndex = (_resolutionIndex - 1 + res.Length) % res.Length;
                    break;
                case Row.Player1Color:
                    _leftColorIndex = (_leftColorIndex - 1 + presets.Length) % presets.Length;
                    break;
                case Row.Player2Color:
                    _rightColorIndex = (_rightColorIndex - 1 + presets.Length) % presets.Length;
                    break;
            }
            changed = true;
            SoundManager.Play(SoundManager.MenuMove);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Right))
        {
            switch (Rows[_selectedRow])
            {
                case Row.DisplayMode:
                    _fullscreen = !_fullscreen;
                    break;
                case Row.Resolution:
                    _resolutionIndex = (_resolutionIndex + 1) % res.Length;
                    break;
                case Row.Player1Color:
                    _leftColorIndex = (_leftColorIndex + 1) % presets.Length;
                    break;
                case Row.Player2Color:
                    _rightColorIndex = (_rightColorIndex + 1) % presets.Length;
                    break;
            }
            changed = true;
            SoundManager.Play(SoundManager.MenuMove);
        }

        if (changed)
        {
            GameSettings.IsFullscreen = _fullscreen;
            GameSettings.ScreenWidth = res[_resolutionIndex].W;
            GameSettings.ScreenHeight = res[_resolutionIndex].H;
            GameSettings.ApplyResolution();
            GameSettings.LeftPaddle = presets[_leftColorIndex].Color;
            GameSettings.RightPaddle = presets[_rightColorIndex].Color;
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

        // Title
        const string title = "GRAPHICS";
        int titleW = Raylib.MeasureText(title, 36);
        Raylib.DrawText(title, centerX - titleW / 2, 28, 36, GameSettings.BallColor);

        // 5 rows, label left / value right, vertically balanced
        const int startY = 82;
        const int rowH = 44;
        var presets = GameSettings.ColorPresets;
        var res = GameSettings.Resolutions;

        for (int i = 0; i < Rows.Length; i++)
        {
            int y = startY + i * rowH;
            bool isSelected = i == _selectedRow;
            Color labelColor = isSelected ? GameSettings.Text : GameSettings.Line;
            Color valueColor = isSelected ? GameSettings.BallColor : GameSettings.Line;
            int fontSize = isSelected ? 22 : 20;

            string label = Rows[i] switch
            {
                Row.DisplayMode => "DISPLAY",
                Row.Resolution => "RESOLUTION",
                Row.Player1Color => "P1 COLOR",
                Row.Player2Color => "P2 COLOR",
                _ => "",
            };

            string value = Rows[i] switch
            {
                Row.DisplayMode => _fullscreen ? "FULLSCREEN" : "WINDOWED",
                Row.Resolution => $"{res[_resolutionIndex].W} x {res[_resolutionIndex].H}",
                Row.Player1Color => presets[_leftColorIndex].Name,
                Row.Player2Color => presets[_rightColorIndex].Name,
                _ => "",
            };

            Color? swatchColor = Rows[i] switch
            {
                Row.Player1Color => presets[_leftColorIndex].Color,
                Row.Player2Color => presets[_rightColorIndex].Color,
                _ => null,
            };

            // Label (left-aligned within center area)
            int labelX = centerX - 170;
            Raylib.DrawText(label, labelX, y, fontSize, labelColor);

            // Value (right-aligned within center area)
            int valueW = Raylib.MeasureText(value, fontSize);
            int valueX = centerX + 170 - valueW;

            // Arrows for selected row
            if (isSelected)
            {
                float pulse = MathF.Sin(_time * 4f) * 3f;
                Raylib.DrawText("<", valueX - 26 + (int)pulse, y, fontSize, GameSettings.LeftPaddle);
                Raylib.DrawText(">", valueX + valueW + 8 - (int)pulse, y, fontSize, GameSettings.LeftPaddle);
            }

            Raylib.DrawText(value, valueX, y, fontSize, valueColor);

            // Color swatch
            if (swatchColor.HasValue)
            {
                int swatchSize = isSelected ? 18 : 14;
                int swatchX = valueX + valueW + (isSelected ? 34 : 12);
                int swatchY = y + (fontSize - swatchSize) / 2;
                Raylib.DrawRectangleRounded(
                    new Rectangle(swatchX, swatchY, swatchSize, swatchSize),
                    0.3f, 4, swatchColor.Value);
            }
        }

        // Preview area
        int previewY = startY + Rows.Length * rowH + 14;
        string previewLabel = "PREVIEW";
        int plW = Raylib.MeasureText(previewLabel, 14);
        Raylib.DrawText(previewLabel, centerX - plW / 2, previewY, 14, GameSettings.Line);

        int pY = previewY + 20;
        Raylib.DrawRectangleRounded(
            new Rectangle(centerX - 70, pY, 10, 40), 0.3f, 4, GameSettings.LeftPaddle);
        Raylib.DrawCircle(centerX, pY + 20, 7, GameSettings.BallColor);
        Raylib.DrawRectangleRounded(
            new Rectangle(centerX + 60, pY, 10, 40), 0.3f, 4, GameSettings.RightPaddle);

        // Footer
        const string footer = "UP/DOWN select  |  LEFT/RIGHT change  |  ESC back";
        int footerW = Raylib.MeasureText(footer, 14);
        Raylib.DrawText(footer,
            centerX - footerW / 2,
            vh - 24,
            14, GameSettings.Line);
    }
}
