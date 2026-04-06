using Raylib_cs;

namespace PongGameV2.Core;

public static class GameSettings
{
    // Virtual (design) resolution — all game logic and drawing uses these
    public const int VirtualWidth = 800;
    public const int VirtualHeight = 450;

    // Window (mutable — changed via Graphics settings)
    public static int ScreenWidth { get; set; } = 1280;
    public static int ScreenHeight { get; set; } = 720;

    // Paddle
    public const float PaddleWidth = 14f;
    public const float PaddleHeight = 90f;
    public const float PaddleSpeed = 420f;
    public const float PaddleMargin = 30f;

    // Ball
    public const float BallRadius = 10f;
    public const float BallStartSpeed = 350f;
    public const float BallMaxSpeed = 700f;
    public const float BallAcceleration = 25f;

    // Rules
    public const int WinScore = 7;

    // Colors (mutable — changed via Graphics settings)
    public static Color LeftPaddle { get; set; } = new(80, 200, 255, 255);
    public static Color RightPaddle { get; set; } = new(255, 100, 120, 255);
    public static Color BallColor { get; set; } = new(255, 255, 255, 255);

    // Colors (fixed UI)
    public static readonly Color Background = new(18, 18, 30, 255);
    public static readonly Color Line = new(50, 50, 80, 255);
    public static readonly Color Score = new(60, 60, 100, 255);
    public static readonly Color Text = new(200, 200, 230, 255);

    // Display mode
    public static bool IsFullscreen { get; set; }

    // Sound — three independent volume controls
    public static int MasterVolume { get; set; } = 7;  // 0-10, scales both SFX and Music
    public static int SfxVolume { get; set; } = 7;     // 0-10
    public static int MusicVolume { get; set; } = 7;   // 0-10
    public static bool MusicEnabled { get; set; } = true;

    /// <summary>Effective SFX volume (0.0 - 1.0), combining Master and SFX sliders.</summary>
    public static float EffectiveSfxVolume => (MasterVolume / 10f) * (SfxVolume / 10f);

    /// <summary>Effective music volume (0.0 - 1.0), combining Master and Music sliders.</summary>
    public static float EffectiveMusicVolume => (MasterVolume / 10f) * (MusicVolume / 10f);

    // Available resolutions
    public static readonly (int W, int H)[] Resolutions =
    [
        (800, 450),
        (960, 540),
        (1280, 720),
        (1600, 900),
        (1920, 1080),
        (2560, 1440),
        (3840, 2160),
    ];

    // Available color presets for paddles / ball
    public static readonly (string Name, Color Color)[] ColorPresets =
    [
        ("Cyan",    new Color((byte)80,  (byte)200, (byte)255, (byte)255)),
        ("Red",     new Color((byte)255, (byte)100, (byte)120, (byte)255)),
        ("Green",   new Color((byte)100, (byte)220, (byte)100, (byte)255)),
        ("Yellow",  new Color((byte)240, (byte)220, (byte)80,  (byte)255)),
        ("Purple",  new Color((byte)180, (byte)100, (byte)255, (byte)255)),
        ("Orange",  new Color((byte)255, (byte)160, (byte)50,  (byte)255)),
        ("Pink",    new Color((byte)255, (byte)130, (byte)200, (byte)255)),
        ("White",   new Color((byte)255, (byte)255, (byte)255, (byte)255)),
    ];

    public static void ApplyResolution()
    {
        if (IsFullscreen)
        {
            int monitor = Raylib.GetCurrentMonitor();
            int mw = Raylib.GetMonitorWidth(monitor);
            int mh = Raylib.GetMonitorHeight(monitor);
            Raylib.SetWindowSize(mw, mh);
            if (!Raylib.IsWindowFullscreen())
                Raylib.ToggleFullscreen();
        }
        else
        {
            if (Raylib.IsWindowFullscreen())
                Raylib.ToggleFullscreen();
            Raylib.SetWindowSize(ScreenWidth, ScreenHeight);

            // Center window on current monitor
            int monitor = Raylib.GetCurrentMonitor();
            int mw = Raylib.GetMonitorWidth(monitor);
            int mh = Raylib.GetMonitorHeight(monitor);
            Raylib.SetWindowPosition((mw - ScreenWidth) / 2, (mh - ScreenHeight) / 2);
        }
    }
}
