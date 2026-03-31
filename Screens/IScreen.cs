namespace PongGameV2.Screens;

public enum ScreenAction
{
    None,
    SelectMode,
    SelectDifficulty,
    SelectScore,
    StartGame,
    OpenSettings,
    OpenGraphics,
    OpenSound,
    OpenControls,
    OpenHowToPlay,
    BackToMenu,
    ExitApp,
}

public interface IScreen
{
    ScreenAction Update(float dt);
    void Draw();
}
