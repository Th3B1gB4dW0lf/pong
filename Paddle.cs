using System;
using System.Numerics;
using Raylib_cs;

namespace PongGameV2;

public class Paddle
{
    public float Y { get; protected set; }
    public float X { get; }
    public Color Color { get; }

    private readonly KeyboardKey _upKey;
    private readonly KeyboardKey _downKey;
    private readonly KeyboardKey _upperSwingKey;
    private readonly KeyboardKey _lowerSwingKey;
    private readonly bool _facingRight;

    // Swing state
    private float _swingTimer;
    private float _swingCooldown;
    private int _swingDir; // +1 = upper, -1 = lower

    private const float SwingDuration = 0.25f;
    private const float SwingCooldownTime = 0.45f;
    private const float SwingMaxAngle = 25f;
    private const float SwingSpeedBonus = 50f;
    private const float SwingAngleBias = 0.4f;

    // Size modifier from power-ups
    public float SizeMultiplier { get; private set; } = 1f;
    private float _sizeEffectTimer;

    public float EffectivePaddleHeight => GameSettings.PaddleHeight * SizeMultiplier;

    public float SwingAngle { get; private set; }
    public bool IsSwingActive => _swingTimer > SwingDuration * 0.35f;
    public bool LastHitWasSwing { get; private set; }

    public Paddle(float x, Color color, KeyboardKey upKey, KeyboardKey downKey,
                  KeyboardKey upperSwingKey = KeyboardKey.Null, KeyboardKey lowerSwingKey = KeyboardKey.Null,
                  bool facingRight = true)
    {
        X = x;
        Color = color;
        _upKey = upKey;
        _downKey = downKey;
        _upperSwingKey = upperSwingKey;
        _lowerSwingKey = lowerSwingKey;
        _facingRight = facingRight;
        Reset();
    }

    public void Reset()
    {
        Y = GameSettings.VirtualHeight / 2f;
        _swingTimer = 0f;
        _swingCooldown = 0f;
        _swingDir = 0;
        SwingAngle = 0f;
        SizeMultiplier = 1f;
        _sizeEffectTimer = 0f;
    }

    public void ApplySizeEffect(float multiplier, float duration)
    {
        SizeMultiplier = multiplier;
        _sizeEffectTimer = duration;
    }

    public virtual void Update(float dt)
    {
        if (Raylib.IsKeyDown(_upKey))
            Y -= GameSettings.PaddleSpeed * dt;
        if (Raylib.IsKeyDown(_downKey))
            Y += GameSettings.PaddleSpeed * dt;

        ClampY();
        UpdateSwing(dt);
        UpdateSizeEffect(dt);
    }

    protected void UpdateSizeEffect(float dt)
    {
        if (_sizeEffectTimer > 0f)
        {
            _sizeEffectTimer -= dt;
            if (_sizeEffectTimer <= 0f)
            {
                SizeMultiplier = 1f;
                _sizeEffectTimer = 0f;
            }
        }
    }

    protected void UpdateSwing(float dt)
    {
        _swingCooldown -= dt;

        if (_swingTimer > 0f)
        {
            _swingTimer -= dt;
            float progress = 1f - (_swingTimer / SwingDuration);
            float curve = MathF.Sin(progress * MathF.PI);
            float maxAngle = SwingMaxAngle * (_facingRight ? 1f : -1f) * _swingDir;
            SwingAngle = maxAngle * curve;

            if (_swingTimer <= 0f)
            {
                SwingAngle = 0f;
                _swingDir = 0;
            }
        }
        else if (_swingCooldown <= 0f)
        {
            if (_upperSwingKey != KeyboardKey.Null && Raylib.IsKeyPressed(_upperSwingKey))
                StartSwing(1);
            else if (_lowerSwingKey != KeyboardKey.Null && Raylib.IsKeyPressed(_lowerSwingKey))
                StartSwing(-1);
        }
    }

    private void StartSwing(int direction)
    {
        _swingDir = direction;
        _swingTimer = SwingDuration;
        _swingCooldown = SwingCooldownTime;
        SoundManager.Play(SoundManager.Swing);
    }

    /// <summary>
    /// Allows AI to trigger a swing programmatically.
    /// </summary>
    public void TriggerSwing(int direction)
    {
        if (_swingCooldown <= 0f && _swingTimer <= 0f)
            StartSwing(direction);
    }

    protected void MoveY(float delta)
    {
        Y += delta;
        ClampY();
    }

    private void ClampY()
    {
        float half = EffectivePaddleHeight / 2f;
        Y = Math.Clamp(Y, half, GameSettings.VirtualHeight - half);
    }

    public bool CheckCollision(ref Vector2 ballPos, ref Vector2 ballVel, ref float ballSpeed)
    {
        LastHitWasSwing = false;

        float halfW = GameSettings.PaddleWidth / 2f;
        float halfH = EffectivePaddleHeight / 2f;

        float closestX = Math.Clamp(ballPos.X, X - halfW, X + halfW);
        float closestY = Math.Clamp(ballPos.Y, Y - halfH, Y + halfH);

        float dx = ballPos.X - closestX;
        float dy = ballPos.Y - closestY;

        if (dx * dx + dy * dy >= GameSettings.BallRadius * GameSettings.BallRadius)
            return false;

        float hitNorm = (ballPos.Y - Y) / halfH;

        if (IsSwingActive)
        {
            LastHitWasSwing = true;
            // Upper swing biases upward (-), lower swing biases downward (+)
            hitNorm += _swingDir * -SwingAngleBias;
            hitNorm = Math.Clamp(hitNorm, -1.3f, 1.3f);
            ballSpeed = MathF.Min(ballSpeed + GameSettings.BallAcceleration + SwingSpeedBonus, GameSettings.BallMaxSpeed + 100f);
        }
        else
        {
            ballSpeed = MathF.Min(ballSpeed + GameSettings.BallAcceleration, GameSettings.BallMaxSpeed);
        }

        float angle = hitNorm * MathF.PI / 3.5f;
        float dir = ballVel.X < 0 ? 1f : -1f;
        ballVel = new Vector2(MathF.Cos(angle) * dir, MathF.Sin(angle)) * ballSpeed;

        ballPos.X = X + (halfW + GameSettings.BallRadius + 1) * dir;
        return true;
    }

    public void Draw()
    {
        float halfW = GameSettings.PaddleWidth / 2f;
        float effH = EffectivePaddleHeight;
        float halfH = effH / 2f;
        float x = X - halfW;
        float y = Y - halfH;

        bool rotated = SwingAngle != 0f;
        if (rotated)
        {
            Rlgl.PushMatrix();
            Rlgl.Translatef(X, Y, 0);
            Rlgl.Rotatef(SwingAngle, 0, 0, 1);
            Rlgl.Translatef(-X, -Y, 0);
        }

        // Size effect visual indicator
        Color drawColor = Color;
        if (SizeMultiplier > 1f)
        {
            // Green tint when enlarged
            drawColor = new Color(
                (byte)Math.Min(Color.R / 2 + 40, 255),
                (byte)Math.Min(Color.G / 2 + 160, 255),
                (byte)Math.Min(Color.B / 2 + 40, 255),
                (byte)255);
        }
        else if (SizeMultiplier < 1f)
        {
            // Red tint when shrunk
            drawColor = new Color(
                (byte)Math.Min(Color.R / 2 + 180, 255),
                (byte)Math.Min(Color.G / 3 + 30, 255),
                (byte)Math.Min(Color.B / 3 + 30, 255),
                (byte)255);
        }

        // Glow layers
        byte glowBoost = (byte)(IsSwingActive ? 20 : 0);
        for (int i = 3; i >= 1; i--)
        {
            float expand = i * 4f;
            byte alpha = (byte)Math.Min(30 / i + glowBoost, 255);
            Color glow = new(drawColor.R, drawColor.G, drawColor.B, alpha);
            Raylib.DrawRectangleRounded(
                new Rectangle(x - expand, y - expand,
                    GameSettings.PaddleWidth + expand * 2,
                    effH + expand * 2),
                0.3f, 6, glow);
        }

        Raylib.DrawRectangleRounded(
            new Rectangle(x, y, GameSettings.PaddleWidth, effH),
            0.3f, 6, drawColor);

        if (rotated)
            Rlgl.PopMatrix();
    }
}
