using System;
using System.Numerics;
using Raylib_cs;
using PongGameV2.Core;

namespace PongGameV2.Gameplay;

public class Ball
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Speed { get; set; }

    public Color CurrentColor { get; set; }

    public float FireTimer { get; set; }
    private const float FireDuration = 2.0f;
    public bool IsOnFire => FireTimer > 0f;

    private const int TrailLength = 16;
    private readonly Vector2[] _trail = new Vector2[TrailLength];
    private readonly Color[] _trailColors = new Color[TrailLength];
    private int _trailIndex;
    private int _trailTimer;

    public Ball()
    {
        Reset();
    }

    public void Reset()
    {
        Position = new Vector2(GameSettings.VirtualWidth / 2f, GameSettings.VirtualHeight / 2f);
        Velocity = Vector2.Zero;
        Speed = GameSettings.BallStartSpeed;
        CurrentColor = Color.White;
        FireTimer = 0f;

        for (int i = 0; i < _trail.Length; i++)
        {
            _trail[i] = Position;
            _trailColors[i] = Color.White;
        }

        _trailIndex = 0;
        _trailTimer = 0;
    }

    public void SetHitBy(Color playerColor, bool wasSwing)
    {
        CurrentColor = playerColor;
        if (wasSwing)
            FireTimer = FireDuration;
    }

    public void Serve(bool toRight)
    {
        float angle = (Random.Shared.NextSingle() - 0.5f) * 1.2f;
        float dir = toRight ? 1f : -1f;
        Velocity = new Vector2(MathF.Cos(angle) * dir, MathF.Sin(angle)) * Speed;
    }

    public bool WallBounced { get; private set; }

    public bool Update(float dt, out int scoringSide)
    {
        scoringSide = 0;
        WallBounced = false;
        Position += Velocity * dt;

        if (FireTimer > 0f)
            FireTimer -= dt;

        if (Position.Y - GameSettings.BallRadius < 0)
        {
            Position.Y = GameSettings.BallRadius;
            Velocity.Y = MathF.Abs(Velocity.Y);
            WallBounced = true;
        }
        else if (Position.Y + GameSettings.BallRadius > GameSettings.VirtualHeight)
        {
            Position.Y = GameSettings.VirtualHeight - GameSettings.BallRadius;
            Velocity.Y = -MathF.Abs(Velocity.Y);
            WallBounced = true;
        }

        if (Position.X < -GameSettings.BallRadius * 2)
        {
            scoringSide = 1;
            return true;
        }

        if (Position.X > GameSettings.VirtualWidth + GameSettings.BallRadius * 2)
        {
            scoringSide = -1;
            return true;
        }

        return false;
    }

    public void UpdateTrail()
    {
        _trailTimer++;
        if (_trailTimer % 2 == 0)
        {
            _trail[_trailIndex] = Position;
            _trailColors[_trailIndex] = IsOnFire ? GetFireColor() : CurrentColor;
            _trailIndex = (_trailIndex + 1) % TrailLength;
        }
    }

    private Color GetFireColor()
    {
        float r = Random.Shared.NextSingle();
        if (r < 0.33f) return new Color((byte)255, (byte)100, (byte)20, (byte)255);
        if (r < 0.66f) return new Color((byte)255, (byte)200, (byte)40, (byte)255);
        return new Color((byte)255, (byte)50, (byte)30, (byte)255);
    }

    public void Draw()
    {
        var bc = CurrentColor;

        for (int i = 0; i < TrailLength; i++)
        {
            int idx = (_trailIndex + i) % TrailLength;
            float t = (float)i / TrailLength;

            if (IsOnFire)
            {
                var fc = _trailColors[idx];
                byte alpha = (byte)(t * 160);
                float radius = GameSettings.BallRadius * t * 1.2f;
                Raylib.DrawCircleV(_trail[idx], radius, new Color(fc.R, fc.G, fc.B, alpha));
                if (t > 0.3f)
                {
                    float sparkRadius = radius * 0.5f;
                    byte sparkAlpha = (byte)(t * 80);
                    Raylib.DrawCircleV(_trail[idx], sparkRadius,
                        new Color((byte)255, (byte)255, (byte)100, sparkAlpha));
                }
            }
            else
            {
                byte alpha = (byte)(t * 80);
                float radius = GameSettings.BallRadius * t * 0.8f;
                Raylib.DrawCircleV(_trail[idx], radius, new Color(bc.R, bc.G, bc.B, alpha));
            }
        }

        if (IsOnFire)
        {
            float fireIntensity = MathF.Min(FireTimer / (FireDuration * 0.3f), 1f);
            float flicker = 0.85f + Random.Shared.NextSingle() * 0.15f;

            for (int i = 4; i >= 1; i--)
            {
                float r = GameSettings.BallRadius + i * 7f * flicker;
                byte a = (byte)(35 * fireIntensity / i);
                Raylib.DrawCircleV(Position, r, new Color((byte)255, (byte)120, (byte)20, a));
            }
            for (int i = 2; i >= 1; i--)
            {
                float r = GameSettings.BallRadius + i * 4f;
                byte a = (byte)(50 * fireIntensity / i);
                Raylib.DrawCircleV(Position, r, new Color((byte)255, (byte)220, (byte)60, a));
            }

            Raylib.DrawCircleV(Position, GameSettings.BallRadius,
                new Color((byte)255, (byte)240, (byte)180, (byte)255));
            Raylib.DrawCircleV(Position, GameSettings.BallRadius * 0.6f, Color.White);
        }
        else
        {
            for (int i = 3; i >= 1; i--)
            {
                float r = GameSettings.BallRadius + i * 6f;
                byte a = (byte)(40 / i);
                Raylib.DrawCircleV(Position, r, new Color(bc.R, bc.G, bc.B, a));
            }

            Raylib.DrawCircleV(Position, GameSettings.BallRadius, bc);
        }
    }
}
