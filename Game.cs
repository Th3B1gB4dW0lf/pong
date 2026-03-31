using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PongGameV2;

public class Game
{
    private readonly Paddle _leftPaddle;
    private readonly Paddle _rightPaddle;
    private readonly AiPaddle? _aiPaddle; // non-null when VsCpu
    private readonly Ball _ball;
    private readonly ScreenShake _shake;
    private readonly GameMode _mode;
    private readonly int _winScore;

    private int _leftScore;
    private int _rightScore;
    private float _countdownTimer;
    private int _lastCountdownValue;
    private bool _serveRight;
    private bool _gameOver;
    private bool _paused;

    // Power-ups
    private readonly List<PowerUp> _powerUps = new();
    private float _powerUpSpawnTimer;
    private int _lastHitSide; // -1 = left paddle hit last, +1 = right paddle hit last

    private const float PowerUpSpawnInterval = 6f;
    private const int MaxPowerUps = 2;
    private const float PowerUpEffectDuration = 10f;

    // Fireworks for victory screen
    private readonly List<Firework> _fireworks = new();
    private float _fireworkSpawnTimer;
    private float _gameOverTime;

    public Game(GameMode mode = GameMode.VsPlayer, Difficulty difficulty = Difficulty.Normal, int winScore = 7)
    {
        _mode = mode;
        _winScore = winScore;

        float leftX = GameSettings.PaddleMargin + GameSettings.PaddleWidth / 2f;
        float rightX = GameSettings.VirtualWidth - GameSettings.PaddleMargin - GameSettings.PaddleWidth / 2f;

        _leftPaddle = new Paddle(leftX, GameSettings.LeftPaddle,
            KeyboardKey.W, KeyboardKey.S, KeyboardKey.Q, KeyboardKey.E, facingRight: true);

        if (mode == GameMode.VsCpu)
        {
            var ai = new AiPaddle(rightX, GameSettings.RightPaddle, difficulty);
            _rightPaddle = ai;
            _aiPaddle = ai;
        }
        else
        {
            _rightPaddle = new Paddle(rightX, GameSettings.RightPaddle,
                KeyboardKey.Up, KeyboardKey.Down, KeyboardKey.Period, KeyboardKey.Slash, facingRight: false);
        }

        _ball = new Ball();
        _shake = new ScreenShake();

        Reset();
    }

    public void Reset()
    {
        _leftPaddle.Reset();
        _rightPaddle.Reset();
        _ball.Reset();
        _powerUps.Clear();
        _powerUpSpawnTimer = PowerUpSpawnInterval * 0.5f;

        _leftScore = 0;
        _rightScore = 0;
        _serveRight = true;
        _countdownTimer = 1.5f;
        _lastCountdownValue = -1;
        _gameOver = false;
        _paused = false;
        _lastHitSide = 0;
        _shake.Stop();
        _fireworks.Clear();
        _gameOverTime = 0f;
        _fireworkSpawnTimer = 0f;
    }

    public void Update(float dt)
    {
        if (_gameOver)
        {
            // Update fireworks
            _gameOverTime += dt;
            UpdateFireworks(dt);

            if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
                Reset();
            return;
        }

        // Pause toggle (not during countdown or game over)
        if (Raylib.IsKeyPressed(KeyboardKey.Space) && _countdownTimer <= 0f)
        {
            _paused = !_paused;
            if (_paused)
                SoundManager.Play(SoundManager.CountdownTick);
        }

        if (_paused)
            return;

        _leftPaddle.Update(dt);

        if (_aiPaddle != null)
            _aiPaddle.UpdateAi(dt, _ball);
        else
            _rightPaddle.Update(dt);

        // Countdown before serve
        if (_countdownTimer > 0f)
        {
            int displayValue = (int)MathF.Ceiling(_countdownTimer);
            if (displayValue != _lastCountdownValue)
            {
                _lastCountdownValue = displayValue;
                SoundManager.Play(displayValue > 0 ? SoundManager.CountdownTick : SoundManager.CountdownGo);
            }

            _countdownTimer -= dt;
            if (_countdownTimer <= 0f)
            {
                SoundManager.Play(SoundManager.CountdownGo);
                _ball.Serve(_serveRight);
            }
            return;
        }

        // Ball update
        if (_ball.Update(dt, out int scoringSide))
        {
            OnScore(scoringSide);
            return;
        }

        if (_ball.WallBounced)
            SoundManager.Play(SoundManager.WallBounce);

        // Paddle collisions
        if (_ball.Velocity.X < 0)
        {
            var pos = _ball.Position;
            var vel = _ball.Velocity;
            var speed = _ball.Speed;
            if (_leftPaddle.CheckCollision(ref pos, ref vel, ref speed))
            {
                _ball.Position = pos;
                _ball.Velocity = vel;
                _ball.Speed = speed;
                _ball.SetHitBy(_leftPaddle.Color, _leftPaddle.LastHitWasSwing);
                _lastHitSide = -1;
                _shake.Trigger(_leftPaddle.LastHitWasSwing ? 14f : 6f);
                SoundManager.Play(_leftPaddle.LastHitWasSwing ? SoundManager.SwingHit : SoundManager.PaddleHit);
            }
        }
        else
        {
            var pos = _ball.Position;
            var vel = _ball.Velocity;
            var speed = _ball.Speed;
            if (_rightPaddle.CheckCollision(ref pos, ref vel, ref speed))
            {
                _ball.Position = pos;
                _ball.Velocity = vel;
                _ball.Speed = speed;
                _ball.SetHitBy(_rightPaddle.Color, _rightPaddle.LastHitWasSwing);
                _lastHitSide = 1;
                _shake.Trigger(_rightPaddle.LastHitWasSwing ? 14f : 6f);
                SoundManager.Play(_rightPaddle.LastHitWasSwing ? SoundManager.SwingHit : SoundManager.PaddleHit);
            }
        }

        // Power-up spawning
        UpdatePowerUpSpawning(dt);

        // Power-up collection
        UpdatePowerUpCollection();

        // Power-up despawn
        for (int i = _powerUps.Count - 1; i >= 0; i--)
        {
            if (_powerUps[i].IsExpired)
                _powerUps.RemoveAt(i);
        }

        // Update power-ups
        foreach (var pu in _powerUps)
            pu.Update(dt);

        _ball.UpdateTrail();
        _shake.Update(dt);
    }

    private void UpdatePowerUpSpawning(float dt)
    {
        _powerUpSpawnTimer -= dt;
        if (_powerUpSpawnTimer <= 0f && _powerUps.Count < MaxPowerUps)
        {
            SpawnPowerUp();
            _powerUpSpawnTimer = PowerUpSpawnInterval + Random.Shared.NextSingle() * 3f;
        }
    }

    private void SpawnPowerUp()
    {
        float margin = 80f;
        float x = GameSettings.VirtualWidth * 0.3f + Random.Shared.NextSingle() * GameSettings.VirtualWidth * 0.4f;
        float y = margin + Random.Shared.NextSingle() * (GameSettings.VirtualHeight - margin * 2);

        float roll = Random.Shared.NextSingle();
        var type = roll < 0.25f ? PowerUpType.GrowSelf
                 : roll < 0.5f ? PowerUpType.ShrinkEnemy
                 : roll < 0.75f ? PowerUpType.SpeedBoost
                 : PowerUpType.SlowEnemy;
        _powerUps.Add(new PowerUp(new Vector2(x, y), type));
    }

    private void UpdatePowerUpCollection()
    {
        if (_lastHitSide == 0) return;

        for (int i = _powerUps.Count - 1; i >= 0; i--)
        {
            if (_powerUps[i].CheckBallCollision(_ball.Position))
            {
                ApplyPowerUp(_powerUps[i].Type);
                _powerUps.RemoveAt(i);
                SoundManager.Play(SoundManager.MenuSelect);
            }
        }
    }

    private void ApplyPowerUp(PowerUpType type)
    {
        Paddle collector = _lastHitSide < 0 ? _leftPaddle : _rightPaddle;
        Paddle opponent = _lastHitSide < 0 ? _rightPaddle : _leftPaddle;

        switch (type)
        {
            case PowerUpType.GrowSelf:
                collector.ApplySizeEffect(1.3f, PowerUpEffectDuration);
                break;
            case PowerUpType.ShrinkEnemy:
                opponent.ApplySizeEffect(0.7f, PowerUpEffectDuration);
                break;
            case PowerUpType.SpeedBoost:
                collector.ApplySpeedEffect(1.3f, PowerUpEffectDuration);
                break;
            case PowerUpType.SlowEnemy:
                opponent.ApplySpeedEffect(0.7f, PowerUpEffectDuration);
                break;
        }
    }

    private void OnScore(int scoringSide)
    {
        if (scoringSide > 0)
        {
            _rightScore++;
            _serveRight = false;
        }
        else
        {
            _leftScore++;
            _serveRight = true;
        }

        _shake.Trigger(12f);

        if (_leftScore >= _winScore || _rightScore >= _winScore)
        {
            _gameOver = true;
            _shake.Stop();
            SoundManager.Play(SoundManager.GameOver);
        }
        else
        {
            SoundManager.Play(SoundManager.Score);
            _countdownTimer = 1.0f;
            _ball.Reset();
            _lastHitSide = 0;
        }
    }

    // ── Fireworks ────────────────────────────────────────────

    private void UpdateFireworks(float dt)
    {
        _fireworkSpawnTimer -= dt;
        if (_fireworkSpawnTimer <= 0f)
        {
            // Spawn a new firework burst
            float x = 100f + Random.Shared.NextSingle() * (GameSettings.VirtualWidth - 200f);
            float y = 60f + Random.Shared.NextSingle() * (GameSettings.VirtualHeight - 180f);
            bool leftWon = _leftScore >= _winScore;
            Color winColor = leftWon ? GameSettings.LeftPaddle : GameSettings.RightPaddle;

            // Mix winner color with random bright colors
            Color burstColor;
            float r = Random.Shared.NextSingle();
            if (r < 0.4f)
                burstColor = winColor;
            else if (r < 0.6f)
                burstColor = new Color((byte)255, (byte)220, (byte)50, (byte)255);  // gold
            else if (r < 0.75f)
                burstColor = new Color((byte)255, (byte)100, (byte)50, (byte)255);  // orange
            else if (r < 0.9f)
                burstColor = new Color((byte)100, (byte)220, (byte)255, (byte)255); // cyan
            else
                burstColor = new Color((byte)255, (byte)255, (byte)255, (byte)255); // white

            _fireworks.Add(new Firework(new Vector2(x, y), burstColor));
            // Play firework sound
            if (Random.Shared.NextSingle() < 0.6f)
                SoundManager.Play(SoundManager.FireworkBurst);
            else
                SoundManager.Play(SoundManager.FireworkCrackle);
            _fireworkSpawnTimer = 0.15f + Random.Shared.NextSingle() * 0.35f;
        }

        for (int i = _fireworks.Count - 1; i >= 0; i--)
        {
            _fireworks[i].Update(dt);
            if (_fireworks[i].IsDead)
                _fireworks.RemoveAt(i);
        }
    }

    // ── Draw ─────────────────────────────────────────────────

    public void Draw()
    {
        Raylib.ClearBackground(GameSettings.Background);

        Raylib.BeginMode2D(_shake.GetCamera());

        Field.Draw();
        Field.DrawScores(_leftScore, _rightScore);

        // Draw power-ups
        foreach (var pu in _powerUps)
            pu.Draw();

        _leftPaddle.Draw();
        _rightPaddle.Draw();
        _ball.Draw();

        // Countdown text
        if (_countdownTimer > 0f && !_gameOver)
        {
            int count = (int)MathF.Ceiling(_countdownTimer);
            string text = count > 0 ? count.ToString() : "GO";
            int textW = Raylib.MeasureText(text, 60);
            Raylib.DrawText(text,
                GameSettings.VirtualWidth / 2 - textW / 2,
                GameSettings.VirtualHeight / 2 - 50,
                60, GameSettings.Text);
        }

        Raylib.EndMode2D();

        // Pause overlay
        if (_paused && !_gameOver)
            DrawPaused();

        if (_gameOver)
            DrawGameOver();

        Raylib.DrawFPS(8, 8);
    }

    private void DrawPaused()
    {
        Raylib.DrawRectangle(0, 0, GameSettings.VirtualWidth, GameSettings.VirtualHeight,
            new Color((byte)0, (byte)0, (byte)0, (byte)140));

        const string text = "PAUSED";
        int w = Raylib.MeasureText(text, 50);
        Raylib.DrawText(text, GameSettings.VirtualWidth / 2 - w / 2,
            GameSettings.VirtualHeight / 2 - 40, 50, GameSettings.Text);

        const string sub = "Press SPACE to resume  |  ESC to quit";
        int sw = Raylib.MeasureText(sub, 18);
        Raylib.DrawText(sub, GameSettings.VirtualWidth / 2 - sw / 2,
            GameSettings.VirtualHeight / 2 + 20, 18, GameSettings.Line);
    }

    private void DrawGameOver()
    {
        // Dark overlay
        Raylib.DrawRectangle(0, 0, GameSettings.VirtualWidth, GameSettings.VirtualHeight,
            new Color((byte)0, (byte)0, (byte)0, (byte)180));

        // Draw fireworks behind text
        foreach (var fw in _fireworks)
            fw.Draw();

        bool leftWon = _leftScore >= _winScore;
        string winner = leftWon ? "PLAYER 1 WINS!" : "PLAYER 2 WINS!";
        Color winColor = leftWon ? GameSettings.LeftPaddle : GameSettings.RightPaddle;

        // Pulsing winner text
        float pulse = 1f + MathF.Sin(_gameOverTime * 3f) * 0.05f;
        int fontSize = (int)(50 * pulse);

        // Glow layers behind text
        int w1 = Raylib.MeasureText(winner, fontSize);
        int textX = GameSettings.VirtualWidth / 2 - w1 / 2;
        int textY = GameSettings.VirtualHeight / 2 - 60;

        for (int g = 4; g >= 1; g--)
        {
            byte alpha = (byte)(50 / g);
            Color glow = new(winColor.R, winColor.G, winColor.B, alpha);
            Raylib.DrawText(winner, textX - g, textY - g, fontSize, glow);
            Raylib.DrawText(winner, textX + g, textY + g, fontSize, glow);
            Raylib.DrawText(winner, textX - g, textY + g, fontSize, glow);
            Raylib.DrawText(winner, textX + g, textY - g, fontSize, glow);
        }

        // Main winner text with bright color
        Raylib.DrawText(winner, textX, textY, fontSize, winColor);

        // Score display
        string scoreText = $"{_leftScore} - {_rightScore}";
        int scoreW = Raylib.MeasureText(scoreText, 36);
        Raylib.DrawText(scoreText, GameSettings.VirtualWidth / 2 - scoreW / 2,
            textY + fontSize + 10, 36, GameSettings.Text);

        // Restart hint (fading in after a short delay)
        if (_gameOverTime > 1.5f)
        {
            float fadeAlpha = MathF.Min((_gameOverTime - 1.5f) * 2f, 1f);
            byte a = (byte)(fadeAlpha * 255);
            string restart = "Press ENTER or SPACE to restart";
            int w2 = Raylib.MeasureText(restart, 22);
            Raylib.DrawText(restart, GameSettings.VirtualWidth / 2 - w2 / 2,
                textY + fontSize + 56, 22, new Color(GameSettings.Text.R, GameSettings.Text.G, GameSettings.Text.B, a));
        }
    }
}

// ── Firework particle system ──────────────────────────────

public class Firework
{
    private readonly List<FireworkParticle> _particles = new();
    private float _age;

    public bool IsDead => _particles.Count == 0 && _age > 0.1f;

    public Firework(Vector2 center, Color color)
    {
        int count = 20 + Random.Shared.Next(20);
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
            float speed = 60f + Random.Shared.NextSingle() * 180f;
            float lifetime = 0.6f + Random.Shared.NextSingle() * 0.8f;
            float size = 1.5f + Random.Shared.NextSingle() * 2.5f;

            // Slight color variation
            byte rVar = (byte)Math.Clamp(color.R + Random.Shared.Next(-30, 30), 0, 255);
            byte gVar = (byte)Math.Clamp(color.G + Random.Shared.Next(-30, 30), 0, 255);
            byte bVar = (byte)Math.Clamp(color.B + Random.Shared.Next(-30, 30), 0, 255);

            _particles.Add(new FireworkParticle
            {
                Position = center,
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                Color = new Color(rVar, gVar, bVar, (byte)255),
                Lifetime = lifetime,
                MaxLifetime = lifetime,
                Size = size,
            });
        }
    }

    public void Update(float dt)
    {
        _age += dt;
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Position += p.Velocity * dt;
            p.Velocity *= 1f - 2.5f * dt; // drag
            p.Velocity.Y += 40f * dt;      // gravity
            p.Lifetime -= dt;
            _particles[i] = p;

            if (p.Lifetime <= 0f)
                _particles.RemoveAt(i);
        }
    }

    public void Draw()
    {
        foreach (var p in _particles)
        {
            float t = p.Lifetime / p.MaxLifetime;
            byte alpha = (byte)(t * 255);
            float size = p.Size * (0.3f + t * 0.7f);

            Color c = new(p.Color.R, p.Color.G, p.Color.B, alpha);

            // Glow
            byte glowAlpha = (byte)(t * 60);
            Raylib.DrawCircleV(p.Position, size * 3f, new Color(p.Color.R, p.Color.G, p.Color.B, glowAlpha));

            // Core
            Raylib.DrawCircleV(p.Position, size, c);

            // Bright center
            if (t > 0.5f)
            {
                byte wa = (byte)((t - 0.5f) * 2f * 200);
                Raylib.DrawCircleV(p.Position, size * 0.4f, new Color((byte)255, (byte)255, (byte)255, wa));
            }
        }
    }
}

public struct FireworkParticle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Color Color;
    public float Lifetime;
    public float MaxLifetime;
    public float Size;
}
