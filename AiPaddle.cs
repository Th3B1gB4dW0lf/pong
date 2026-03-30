using System;
using Raylib_cs;

namespace PongGameV2;

public class AiPaddle : Paddle
{
    private readonly float _reactionSpeed;
    private readonly float _offsetRange;
    private readonly float _recalcBase;
    private readonly float _deadZone;

    // Swing AI parameters
    private readonly float _swingChance;        // probability per opportunity
    private readonly float _swingCheckInterval;  // how often to consider swinging
    private float _swingCheckTimer;

    private float _targetOffset;
    private float _recalcTimer;

    public AiPaddle(float x, Color color, Difficulty difficulty)
        : base(x, color, KeyboardKey.Null, KeyboardKey.Null, facingRight: false)
    {
        (_reactionSpeed, _offsetRange, _recalcBase, _deadZone) = difficulty switch
        {
            Difficulty.Easy   => (0.38f, 0.9f, 0.6f, 14f),
            Difficulty.Normal => (0.60f, 0.6f, 0.4f, 8f),
            Difficulty.Hard   => (0.85f, 0.25f, 0.2f, 4f),
            _ => (0.60f, 0.6f, 0.4f, 8f),
        };

        (_swingChance, _swingCheckInterval) = difficulty switch
        {
            Difficulty.Easy   => (0.15f, 1.2f),
            Difficulty.Normal => (0.35f, 0.7f),
            Difficulty.Hard   => (0.65f, 0.4f),
            _ => (0.35f, 0.7f),
        };
    }

    public void UpdateAi(float dt, Ball ball)
    {
        _recalcTimer -= dt;
        if (_recalcTimer <= 0f)
        {
            _targetOffset = (Random.Shared.NextSingle() - 0.5f) * GameSettings.PaddleHeight * _offsetRange;
            _recalcTimer = _recalcBase + Random.Shared.NextSingle() * 0.3f;
        }

        float targetY;
        if (ball.Velocity.X > 0)
        {
            targetY = ball.Position.Y + _targetOffset;
        }
        else
        {
            targetY = GameSettings.VirtualHeight / 2f;
        }

        float diff = targetY - Y;
        float maxMove = GameSettings.PaddleSpeed * _reactionSpeed * dt;

        if (MathF.Abs(diff) > _deadZone)
        {
            MoveY(diff > 0 ? MathF.Min(diff, maxMove) : MathF.Max(diff, -maxMove));
        }

        // AI swing decision
        UpdateAiSwing(dt, ball);

        // Animate swing + size effect timer
        UpdateSwing(dt);
        UpdateSizeEffect(dt);
    }

    private void UpdateAiSwing(float dt, Ball ball)
    {
        _swingCheckTimer -= dt;
        if (_swingCheckTimer > 0f) return;
        _swingCheckTimer = _swingCheckInterval;

        // Only swing when ball is approaching and fairly close
        if (ball.Velocity.X <= 0) return;

        float distX = X - ball.Position.X;
        if (distX < 0 || distX > 180f) return;

        // Ball should be roughly aligned with paddle vertically
        float halfH = EffectivePaddleHeight / 2f;
        float ballRelY = ball.Position.Y - Y;
        if (MathF.Abs(ballRelY) > halfH + 15f) return;

        // Roll the dice
        if (Random.Shared.NextSingle() > _swingChance) return;

        // Choose direction: upper swing if ball is above center, lower if below
        int swingDir = ballRelY < 0 ? 1 : -1;
        TriggerSwing(swingDir);
    }
}
