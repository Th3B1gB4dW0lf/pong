using System;
using System.Numerics;
using Raylib_cs;

namespace PongGameV2;

public enum PowerUpType
{
    GrowSelf,    // +25% paddle size for 10s
    ShrinkEnemy, // -25% opponent paddle size for 10s
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
            float blinkRate = 6f + (3f - remaining) * 4f; // blink faster as time runs out
            float blink = MathF.Sin(_time * blinkRate * MathF.PI);
            globalAlpha = blink > 0 ? (byte)255 : (byte)60;
        }

        if (Type == PowerUpType.GrowSelf)
        {
            // Green up-arrow icon
            Color col = new((byte)80, (byte)220, (byte)80, globalAlpha);

            // Outer glow
            for (int i = 3; i >= 1; i--)
            {
                byte a = (byte)(30 / i < globalAlpha ? 30 / i : globalAlpha);
                Raylib.DrawCircleV(Position, r + i * 4f, new Color(col.R, col.G, col.B, a));
            }

            Raylib.DrawCircleV(Position, r, new Color((byte)20, (byte)60, (byte)20, (byte)(globalAlpha < 200 ? globalAlpha : 200)));
            Raylib.DrawCircleLinesV(Position, r, col);

            // Up arrow symbol
            int cx = (int)Position.X;
            int cy = (int)Position.Y;
            Raylib.DrawTriangle(
                new Vector2(cx, cy - 6),
                new Vector2(cx - 5, cy + 1),
                new Vector2(cx + 5, cy + 1),
                col);
            Raylib.DrawRectangle(cx - 2, cy + 1, 4, 5, col);
        }
        else
        {
            // Red down-arrow icon
            Color col = new((byte)255, (byte)80, (byte)80, globalAlpha);

            for (int i = 3; i >= 1; i--)
            {
                byte a = (byte)(30 / i < globalAlpha ? 30 / i : globalAlpha);
                Raylib.DrawCircleV(Position, r + i * 4f, new Color(col.R, col.G, col.B, a));
            }

            Raylib.DrawCircleV(Position, r, new Color((byte)60, (byte)20, (byte)20, (byte)(globalAlpha < 200 ? globalAlpha : 200)));
            Raylib.DrawCircleLinesV(Position, r, col);

            // Down arrow symbol
            int cx = (int)Position.X;
            int cy = (int)Position.Y;
            Raylib.DrawTriangle(
                new Vector2(cx - 5, cy - 1),
                new Vector2(cx + 5, cy - 1),
                new Vector2(cx, cy + 6),
                col);
            Raylib.DrawRectangle(cx - 2, cy - 6, 4, 5, col);
        }
    }
}
