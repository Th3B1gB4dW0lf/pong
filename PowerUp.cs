using System;
using System.Numerics;
using Raylib_cs;

namespace PongGameV2;

public enum PowerUpType
{
    GrowSelf,      // +30% paddle size for 10s
    ShrinkEnemy,   // -30% opponent paddle size for 10s
    SpeedBoost,    // +30% paddle speed for 10s
    SlowEnemy,     // -30% opponent paddle speed for 10s
}

public class PowerUp
{
    public Vector2 Position { get; }
    public PowerUpType Type { get; }
    public bool Collected { get; set; }

    private float _time;
    private float _lifetime;
    private const float Radius = 12f;
    private const float MaxLifetime = 10f;

    public bool IsExpired => _lifetime >= MaxLifetime;

    public PowerUp(Vector2 position, PowerUpType type)
    {
        Position = position;
        Type = type;
    }

    public void Update(float dt)
    {
        _time += dt;
        _lifetime += dt;
    }

    public float RemainingLife => MathF.Max(0f, MaxLifetime - _lifetime);

    public bool CheckBallCollision(Vector2 ballPos)
    {
        if (Collected) return false;
        float dx = ballPos.X - Position.X;
        float dy = ballPos.Y - Position.Y;
        float dist = dx * dx + dy * dy;
        float r = Radius + GameSettings.BallRadius;
        return dist < r * r;
    }

    public void Draw()
    {
        if (Collected) return;

        float pulse = 0.9f + MathF.Sin(_time * 4f) * 0.1f;
        float r = Radius * pulse;

        // Blink when about to expire (last 3 seconds)
        float remaining = RemainingLife;
        byte globalAlpha = 255;
        if (remaining < 3f)
        {
            float blinkRate = 6f + (3f - remaining) * 4f;
            float blink = MathF.Sin(_time * blinkRate * MathF.PI);
            globalAlpha = blink > 0 ? (byte)255 : (byte)60;
        }

        int cx = (int)Position.X;
        int cy = (int)Position.Y;

        switch (Type)
        {
            case PowerUpType.GrowSelf:
                DrawIcon(r, globalAlpha,
                    new Color((byte)80, (byte)220, (byte)80, globalAlpha),
                    new Color((byte)20, (byte)60, (byte)20, (byte)(globalAlpha < 200 ? globalAlpha : 200)));
                // Up arrow (green)
                DrawUpArrow(cx, cy, new Color((byte)80, (byte)220, (byte)80, globalAlpha));
                break;

            case PowerUpType.ShrinkEnemy:
                DrawIcon(r, globalAlpha,
                    new Color((byte)255, (byte)80, (byte)80, globalAlpha),
                    new Color((byte)60, (byte)20, (byte)20, (byte)(globalAlpha < 200 ? globalAlpha : 200)));
                // Down arrow (red)
                DrawDownArrow(cx, cy, new Color((byte)255, (byte)80, (byte)80, globalAlpha));
                break;

            case PowerUpType.SpeedBoost:
                DrawIcon(r, globalAlpha,
                    new Color((byte)80, (byte)220, (byte)80, globalAlpha),
                    new Color((byte)20, (byte)60, (byte)20, (byte)(globalAlpha < 200 ? globalAlpha : 200)));
                // Right arrow (green)
                DrawRightArrow(cx, cy, new Color((byte)80, (byte)220, (byte)80, globalAlpha));
                break;

            case PowerUpType.SlowEnemy:
                DrawIcon(r, globalAlpha,
                    new Color((byte)255, (byte)80, (byte)80, globalAlpha),
                    new Color((byte)60, (byte)20, (byte)20, (byte)(globalAlpha < 200 ? globalAlpha : 200)));
                // Left arrow (red)
                DrawLeftArrow(cx, cy, new Color((byte)255, (byte)80, (byte)80, globalAlpha));
                break;
        }
    }

    private void DrawIcon(float r, byte globalAlpha, Color col, Color bg)
    {
        // Outer glow
        for (int i = 3; i >= 1; i--)
        {
            byte a = (byte)(30 / i < globalAlpha ? 30 / i : globalAlpha);
            Raylib.DrawCircleV(Position, r + i * 4f, new Color(col.R, col.G, col.B, a));
        }

        // Background circle
        Raylib.DrawCircleV(Position, r, bg);
        // Border
        Raylib.DrawCircleLinesV(Position, r, col);
    }

    private static void DrawUpArrow(int cx, int cy, Color col)
    {
        // Triangle pointing up
        Raylib.DrawTriangle(
            new Vector2(cx, cy - 7),
            new Vector2(cx - 5, cy + 1),
            new Vector2(cx + 5, cy + 1),
            col);
        // Stem
        Raylib.DrawRectangle(cx - 2, cy + 1, 4, 5, col);
    }

    private static void DrawDownArrow(int cx, int cy, Color col)
    {
        // Stem first (on top)
        Raylib.DrawRectangle(cx - 2, cy - 6, 4, 5, col);
        // Triangle pointing down - vertices must be in counter-clockwise order for Raylib
        Raylib.DrawTriangle(
            new Vector2(cx, cy + 7),
            new Vector2(cx + 5, cy - 1),
            new Vector2(cx - 5, cy - 1),
            col);
    }

    private static void DrawRightArrow(int cx, int cy, Color col)
    {
        // Stem
        Raylib.DrawRectangle(cx - 6, cy - 2, 7, 4, col);
        // Triangle pointing right - counter-clockwise
        Raylib.DrawTriangle(
            new Vector2(cx + 7, cy),
            new Vector2(cx + 1, cy + 5),
            new Vector2(cx + 1, cy - 5),
            col);
    }

    private static void DrawLeftArrow(int cx, int cy, Color col)
    {
        // Stem
        Raylib.DrawRectangle(cx - 1, cy - 2, 7, 4, col);
        // Triangle pointing left - counter-clockwise
        Raylib.DrawTriangle(
            new Vector2(cx - 7, cy),
            new Vector2(cx - 1, cy - 5),
            new Vector2(cx - 1, cy + 5),
            col);
    }
}
