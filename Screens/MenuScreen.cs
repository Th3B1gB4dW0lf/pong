using System;
using Raylib_cs;

namespace PongGameV2.Screens;

public class MenuScreen : IScreen
{
    private enum MenuOption { Start, HowToPlay, Settings, Exit }

    private static readonly MenuOption[] Options = [MenuOption.Start, MenuOption.HowToPlay, MenuOption.Settings, MenuOption.Exit];

    private int _selected;

    // Animation
    private float _time;
    private readonly float[] _itemAnimOffsets = new float[Options.Length];

    public MenuScreen()
    {
        _selected = 0;
        _time = 0f;
    }

    public ScreenAction Update(float dt)
    {
        _time += dt;

        // Animate menu item offsets
        for (int i = 0; i < Options.Length; i++)
        {
            float targetOffset = i == _selected ? 20f : 0f;
            _itemAnimOffsets[i] += (targetOffset - _itemAnimOffsets[i]) * dt * 12f;
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
                MenuOption.Start => ScreenAction.SelectMode,
                MenuOption.HowToPlay => ScreenAction.OpenHowToPlay,
                MenuOption.Settings => ScreenAction.OpenSettings,
                MenuOption.Exit => ScreenAction.ExitApp,
                _ => ScreenAction.None,
            };
        }

        return ScreenAction.None;
    }

    public void Draw()
    {
        Raylib.ClearBackground(GameSettings.Background);

        int vw = GameSettings.VirtualWidth;
        int vh = GameSettings.VirtualHeight;

        // Title with subtle float
        float titleY = 55 + MathF.Sin(_time * 1.5f) * 5f;
        const string title = "PONG";
        int titleW = Raylib.MeasureText(title, 80);
        // Title glow
        for (int i = 3; i >= 1; i--)
        {
            byte alpha = (byte)(25 / i);
            Raylib.DrawText(title,
                vw / 2 - titleW / 2 + i,
                (int)titleY + i,
                80, new Color((byte)80, (byte)200, (byte)255, alpha));
        }
        Raylib.DrawText(title, vw / 2 - titleW / 2, (int)titleY, 80, GameSettings.BallColor);

        // Subtitle
        const string subtitle = "V2";
        int subW = Raylib.MeasureText(subtitle, 24);
        Raylib.DrawText(subtitle,
            vw / 2 + titleW / 2 - subW + 12,
            (int)titleY + 58,
            24, GameSettings.LeftPaddle);

        // Menu items — centered vertically between title block (~160) and footer (~426)
        const int startY = 210;
        const int itemSpacing = 42;

        for (int i = 0; i < Options.Length; i++)
        {
            string label = Options[i] switch
            {
                MenuOption.Start => "START",
                MenuOption.HowToPlay => "HOW TO PLAY",
                MenuOption.Settings => "SETTINGS",
                MenuOption.Exit => "EXIT",
                _ => "",
            };

            bool isSelected = i == _selected;
            int fontSize = isSelected ? 30 : 24;
            Color color = isSelected ? GameSettings.BallColor : GameSettings.Line;

            int textW = Raylib.MeasureText(label, fontSize);
            int x = vw / 2 - textW / 2 + (int)_itemAnimOffsets[i];
            int y = startY + i * itemSpacing;

            // Selected glow
            if (isSelected)
            {
                for (int g = 2; g >= 1; g--)
                {
                    byte alpha = (byte)(30 / g);
                    Raylib.DrawText(label, x + g, y + g, fontSize,
                        new Color((byte)80, (byte)200, (byte)255, alpha));
                }

                // Arrow indicator
                string arrow = ">";
                int arrowW = Raylib.MeasureText(arrow, fontSize);
                float arrowPulse = MathF.Sin(_time * 5f) * 4f;
                Raylib.DrawText(arrow, x - arrowW - 14 + (int)arrowPulse, y, fontSize, GameSettings.LeftPaddle);
            }

            Raylib.DrawText(label, x, y, fontSize, color);
        }

        // Footer
        const string footer = "UP/DOWN to navigate  |  ENTER to select";
        int footerW = Raylib.MeasureText(footer, 14);
        Raylib.DrawText(footer,
            vw / 2 - footerW / 2,
            vh - 24,
            14, GameSettings.Line);
    }
}
