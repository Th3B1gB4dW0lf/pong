using System;
using Raylib_cs;

namespace PongGameV2.Screens;

public class SettingsScreen : IScreen
{
    private enum SettingsOption { Graphics, Sound }

    private static readonly SettingsOption[] Options = [SettingsOption.Graphics, SettingsOption.Sound];

    private int _selected;
    private float _time;
    private readonly float[] _itemAnimOffsets = new float[Options.Length];

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
            return Options[_selected] switch
            {
                SettingsOption.Graphics => ScreenAction.OpenGraphics,
                SettingsOption.Sound => ScreenAction.OpenSound,
                _ => ScreenAction.None,
            };
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

        const string title = "SETTINGS";
        int titleW = Raylib.MeasureText(title, 40);
        Raylib.DrawText(title, vw / 2 - titleW / 2, 140, 40, GameSettings.BallColor);

        const int startY = 220;
        const int spacing = 50;

        for (int i = 0; i < Options.Length; i++)
        {
            string label = Options[i] switch
            {
                SettingsOption.Graphics => "GRAPHICS",
                SettingsOption.Sound => "SOUND",
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
                for (int g = 2; g >= 1; g--)
                {
                    byte alpha = (byte)(30 / g);
                    Raylib.DrawText(label, x + g, y + g, fontSize,
                        new Color((byte)80, (byte)200, (byte)255, alpha));
                }

                string arrow = ">";
                int arrowW = Raylib.MeasureText(arrow, fontSize);
                float arrowPulse = MathF.Sin(_time * 5f) * 4f;
                Raylib.DrawText(arrow, x - arrowW - 14 + (int)arrowPulse, y, fontSize, GameSettings.LeftPaddle);
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
