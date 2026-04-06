using System;
using Raylib_cs;
using PongGameV2.Audio;
using PongGameV2.Core;
using PongGameV2.Screens;

namespace PongGameV2;

static class Program
{
    static void Main()
    {
        Raylib.InitWindow(GameSettings.ScreenWidth, GameSettings.ScreenHeight, "Pong V2");
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(0);
        SoundManager.Init();

        // Render target at virtual resolution — everything draws here, then gets scaled to window
        var target = Raylib.LoadRenderTexture(GameSettings.VirtualWidth, GameSettings.VirtualHeight);

        IScreen currentScreen = new MenuScreen();
        bool running = true;

        // Track selections across screens
        GameMode selectedMode = GameMode.VsPlayer;
        Difficulty selectedDifficulty = Difficulty.Normal;

        while (running && !Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();
            SoundManager.UpdateMusic();
            var action = currentScreen.Update(dt);

            switch (action)
            {
                case ScreenAction.SelectMode:
                    currentScreen = new ModeSelectScreen();
                    break;
                case ScreenAction.SelectDifficulty:
                    if (currentScreen is ModeSelectScreen ms1)
                        selectedMode = ms1.SelectedMode;
                    currentScreen = new DifficultyScreen();
                    break;
                case ScreenAction.SelectScore:
                    if (currentScreen is ModeSelectScreen ms2)
                        selectedMode = ms2.SelectedMode;
                    else if (currentScreen is DifficultyScreen ds)
                        selectedDifficulty = ds.SelectedDifficulty;
                    currentScreen = new ScoreSelectScreen();
                    break;
                case ScreenAction.StartGame:
                    int winScore = currentScreen is ScoreSelectScreen ss ? ss.SelectedScore : 7;
                    currentScreen = new GameScreen(selectedMode, selectedDifficulty, winScore);
                    SoundManager.SetInGame(true);
                    break;
                case ScreenAction.OpenSettings:
                    currentScreen = new SettingsScreen();
                    break;
                case ScreenAction.OpenGraphics:
                    currentScreen = new GraphicsSettingsScreen();
                    break;
                case ScreenAction.OpenSound:
                    currentScreen = new SoundSettingsScreen();
                    break;
                case ScreenAction.OpenControls:
                    currentScreen = new ControlsScreen();
                    break;
                case ScreenAction.OpenHowToPlay:
                    currentScreen = new HowToPlayScreen();
                    break;
                case ScreenAction.BackToMenu:
                    selectedMode = GameMode.VsPlayer;
                    selectedDifficulty = Difficulty.Normal;
                    currentScreen = new MenuScreen();
                    SoundManager.SetInGame(false);
                    break;
                case ScreenAction.ExitApp:
                    running = false;
                    break;
            }

            // Draw scene to virtual-resolution render target
            Raylib.BeginTextureMode(target);
            currentScreen.Draw();
            Raylib.EndTextureMode();

            // Scale render target to fill the window (letterboxed)
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            // Use actual window size (may differ from ScreenWidth/Height in fullscreen)
            int winW = Raylib.GetScreenWidth();
            int winH = Raylib.GetScreenHeight();

            float scaleX = (float)winW / GameSettings.VirtualWidth;
            float scaleY = (float)winH / GameSettings.VirtualHeight;
            float scale = MathF.Min(scaleX, scaleY);

            float drawW = GameSettings.VirtualWidth * scale;
            float drawH = GameSettings.VirtualHeight * scale;
            float offsetX = (winW - drawW) / 2f;
            float offsetY = (winH - drawH) / 2f;

            // RenderTexture is flipped vertically in OpenGL, so we use negative height in source rect
            Raylib.DrawTexturePro(
                target.Texture,
                new Rectangle(0, 0, GameSettings.VirtualWidth, -GameSettings.VirtualHeight),
                new Rectangle(offsetX, offsetY, drawW, drawH),
                System.Numerics.Vector2.Zero,
                0f,
                Color.White);

            Raylib.EndDrawing();
        }

        Raylib.UnloadRenderTexture(target);
        SoundManager.Shutdown();
        Raylib.CloseWindow();
    }
}
