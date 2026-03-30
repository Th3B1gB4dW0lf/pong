using System;
using Raylib_cs;

namespace PongGameV2.Screens;

public class ModeSelectScreen : IScreen
{
    private static readonly GameMode[] Modes = [GameMode.VsCpu, GameMode.VsPlayer];

    private int _selected;
    private float _time;
    private readonly float[] _itemAnimOffsets = new float[Modes.Length];

    public GameMode SelectedMode => Modes[_selected];

    public ScreenAction Update(float dt)
    {
        _time += dt;

        for (int i = 0; i < Modes.Length; i++)
        {
            float target = i == _selected ? 20f : 0f;
            _itemAnimOffsets[i] += (target - _itemAnimOffsets[i]) * dt * 12f;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            _selected = (_selected - 1 + Modes.Length) % Modes.Length;
            SoundManager.Play(SoundManager.MenuMove);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            _selected = (_selected + 1) % Modes.Length;
            SoundManager.Play(SoundManager.MenuMove);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            SoundManager.Play(SoundManager.MenuSelect);
            return Modes[_selected] == GameMode.VsCpu
                ? ScreenAction.SelectDifficulty
                : ScreenAction.SelectScore;
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

        // Title
        const string title = "SELECT MODE";
        int titleW = Raylib.MeasureText(title, 40);
        Raylib.DrawText(title, vw / 2 - titleW / 2, 115, 40, GameSettings.BallColor);

        // Mode items — centered between title (~155) and footer (~426)
        const int startY = 200;
        const int spacing = 75;

        for (int i = 0; i < Modes.Length; i++)
        {
            string label = Modes[i] switch
            {
                GameMode.VsPlayer => "2 PLAYERS",
                GameMode.VsCpu => "1 PLAYER",
                _ => "",
            };

            string desc = Modes[i] switch
            {
                GameMode.VsPlayer => "W/S + Q/E swing  vs  Up/Down + ./? swing",
                GameMode.VsCpu => "W/S + Q/E swing  vs  Computer",
                _ => "",
            };

            bool isSelected = i == _selected;
            int fontSize = isSelected ? 30 : 24;
            Color color = isSelected ? GameSettings.BallColor : GameSettings.Line;

            int textW = Raylib.MeasureText(label, fontSize);
            int x = vw / 2 - textW / 2 + (int)_itemAnimOffsets[i];
            int y = startY + i * spacing;

            if (isSelected)
            {
                // Glow
                for (int g = 2; g >= 1; g--)
                {
                    byte alpha = (byte)(30 / g);
                    Raylib.DrawText(label, x + g, y + g, fontSize,
                        new Color((byte)80, (byte)200, (byte)255, alpha));
                }

                // Arrow
                string arrow = ">";
                int arrowW = Raylib.MeasureText(arrow, fontSize);
                float arrowPulse = MathF.Sin(_time * 5f) * 4f;
                Raylib.DrawText(arrow, x - arrowW - 14 + (int)arrowPulse, y, fontSize, GameSettings.LeftPaddle);

                // Description below selected item
                int descW = Raylib.MeasureText(desc, 14);
                Raylib.DrawText(desc,
                    vw / 2 - descW / 2 + (int)_itemAnimOffsets[i],
                    y + fontSize + 8, 14, GameSettings.Line);
            }

            Raylib.DrawText(label, x, y, fontSize, color);
        }

        // Footer
        const string footer = "ENTER to select  |  ESC to go back";
        int footerW = Raylib.MeasureText(footer, 14);
        Raylib.DrawText(footer,
            vw / 2 - footerW / 2,
            vh - 24,
            14, GameSettings.Line);

    }
}
