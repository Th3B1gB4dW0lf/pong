using Raylib_cs;
using PongGameV2.Core;
using PongGameV2.Gameplay;

namespace PongGameV2.Screens;

public class GameScreen : IScreen
{
    private readonly Game _game;

    public GameScreen(GameMode mode, Difficulty difficulty = Difficulty.Normal, int winScore = 7)
    {
        _game = new Game(mode, difficulty, winScore);
    }

    public ScreenAction Update(float dt)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            return ScreenAction.BackToMenu;

        _game.Update(dt);
        return ScreenAction.None;
    }

    public void Draw()
    {
        _game.Draw();
    }
}
