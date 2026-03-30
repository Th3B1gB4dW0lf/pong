using System;
using Raylib_cs;

namespace PongGameV2.Screens;

public class DifficultyScreen : IScreen
{
    private static readonly Difficulty[] Options = [Difficulty.Easy, Difficulty.Normal, Difficulty.Hard];

    private int _selected = 1; // default to Normal
    private float _time;
    private readonly float[] _itemAnimOffsets = new float[Options.Length];

    public Difficulty SelectedDifficulty => Options[_selected];

    public ScreenAction Update(float dt)
    {
        _time += dt;

        for (int i = 0; i < Options.Length; i++)
        {
            float target = i == _selected ? 20f : 0f;
            _itemAnimOffsets[i] += (target - _itemAnimOffsets[i]) * dt * 12f;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            _selected = (_selected - 1 + Options.Length) % Options.Length;
            SoundManager.Play(SoundManager.MenuMove);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            _selected = (_selected + 1) % Options.Length;
            SoundManager.Play(SoundManager.MenuMove);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            SoundManager.Play(SoundManager.MenuSelect);
            return ScreenAction.SelectScore;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            return ScreenAction.SelectMode;

        return ScreenAction.None;
    }

    public void Draw()
    {
        Raylib.ClearBackground(GameSettings.Background);

        int vw = GameSettings.VirtualWidth;
        int vh = GameSettings.VirtualHeight;

        const string title = "SELECT DIFFICULTY";
        int titleW = Raylib.MeasureText(title, 40);
        Raylib.DrawText(title, vw / 2 - titleW / 2, 100, 40, GameSettings.BallColor);

        // 3 items with desc — centered between title (~140) and footer (~426)
        const int startY = 185;
        const int spacing = 60;

        for (int i = 0; i < Options.Length; i++)
        {
            string label = Options[i] switch
            {
                Difficulty.Easy => "EASY",
                Difficulty.Normal => "NORMAL",
                Difficulty.Hard => "HARD",
                _ => "",
            };

            Color accentColor = Options[i] switch
            {
                Difficulty.Easy => new Color((byte)100, (byte)220, (byte)100, (byte)255),
                Difficulty.Normal => new Color((byte)220, (byte)200, (byte)80, (byte)255),
                Difficulty.Hard => new Color((byte)255, (byte)80, (byte)80, (byte)255),
                _ => GameSettings.BallColor,
            };

            string desc = Options[i] switch
            {
                Difficulty.Easy => "Relaxed pace  -  the CPU takes it easy",
                Difficulty.Normal => "Fair challenge  -  balanced opponent",
                Difficulty.Hard => "Intense  -  the CPU plays to win",
                _ => "",
            };

            bool isSelected = i == _selected;
            int fontSize = isSelected ? 30 : 24;
            Color color = isSelected ? accentColor : GameSettings.Line;

            int textW = Raylib.MeasureText(label, fontSize);
            int x = vw / 2 - textW / 2 + (int)_itemAnimOffsets[i];
            int y = startY + i * spacing;

            if (isSelected)
            {
                for (int g = 2; g >= 1; g--)
                {
                    byte alpha = (byte)(30 / g);
                    Raylib.DrawText(label, x + g, y + g, fontSize,
                        new Color(accentColor.R, accentColor.G, accentColor.B, alpha));
                }

                string arrow = ">";
                int arrowW = Raylib.MeasureText(arrow, fontSize);
                float arrowPulse = MathF.Sin(_time * 5f) * 4f;
                Raylib.DrawText(arrow, x - arrowW - 14 + (int)arrowPulse, y, fontSize, accentColor);

                int descW = Raylib.MeasureText(desc, 14);
                Raylib.DrawText(desc,
                    vw / 2 - descW / 2 + (int)_itemAnimOffsets[i],
                    y + fontSize + 6, 14, GameSettings.Line);
            }

            Raylib.DrawText(label, x, y, fontSize, color);
        }

        const string footer = "ENTER to select  |  ESC to go back";
        int footerW = Raylib.MeasureText(footer, 14);
        Raylib.DrawText(footer,
            vw / 2 - footerW / 2,
            vh - 24,
            14, GameSettings.Line);

    }
}
