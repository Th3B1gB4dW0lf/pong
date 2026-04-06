using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PongGameV2.Audio;
using PongGameV2.Core;

namespace PongGameV2.Gameplay;

public class Game
{
    private readonly Paddle _leftPaddle;
    private readonly Paddle _rightPaddle;
    private readonly AiPaddle? _aiPaddle;
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
    private int _lastHitSide;

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
            _gameOverTime += dt;
            UpdateFireworks(dt);

            if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
                Reset();
            return;
        }

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

        if (_ball.Update(dt, out int scoringSide))
        {
            OnScore(scoringSide);
            return;
        }

        if (_ball.WallBounced)
            SoundManager.Play(SoundManager.WallBounce);

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

        UpdatePowerUpSpawning(dt);
        UpdatePowerUpCollection();

        for (int i = _powerUps.Count - 1; i >= 0; i--)
        {
            if (_powerUps[i].IsExpired)
                _powerUps.RemoveAt(i);
        }

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
                collector.ApplySpeedEffect(1.5f, PowerUpEffectDuration);
                break;
            case PowerUpType.SlowEnemy:
                opponent.ApplySpeedEffect(0.5f, PowerUpEffectDuration);
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

    private void UpdateFireworks(float dt)
    {
        _fireworkSpawnTimer -= dt;
        if (_fireworkSpawnTimer <= 0f)
        {
            float x = 100f + Random.Shared.NextSingle() * (GameSettings.VirtualWidth - 200f);
            float y = 60f + Random.Shared.NextSingle() * (GameSettings.VirtualHeight - 180f);
            bool leftWon = _leftScore >= _winScore;
            Color winColor = leftWon ? GameSettings.LeftPaddle : GameSettings.RightPaddle;

            Color burstColor;
            float r = Random.Shared.NextSingle();
            if (r < 0.4f) burstColor = winColor;
            else if (r < 0.6f) burstColor = new Color((byte)255, (byte)220, (byte)50, (byte)255);
            else if (r < 0.75f) burstColor = new Color((byte)255, (byte)100, (byte)50, (byte)255);
            else if (r < 0.9f) burstColor = new Color((byte)100, (byte)220, (byte)255, (byte)255);
            else burstColor = new Color((byte)255, (byte)255, (byte)255, (byte)255);

            _fireworks.Add(new Firework(new Vector2(x, y), burstColor));
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

    public void Draw()
    {
        Raylib.ClearBackground(GameSettings.Background);

        Raylib.BeginMode2D(_shake.GetCamera());

        Field.Draw();
        Field.DrawScores(_leftScore, _rightScore);

        foreach (var pu in _powerUps)
            pu.Draw();

        _leftPaddle.Draw();
        _rightPaddle.Draw();
        _ball.Draw();

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
        Raylib.DrawRectangle(0, 0, GameSettings.VirtualWidth, GameSettings.VirtualHeight,
            new Color((byte)0, (byte)0, (byte)0, (byte)180));

        foreach (var fw in _fireworks)
            fw.Draw();

        bool leftWon = _leftScore >= _winScore;
        string winner = leftWon ? "PLAYER 1 WINS!" : "PLAYER 2 WINS!";
        Color winColor = leftWon ? GameSettings.LeftPaddle : GameSettings.RightPaddle;

        float pulse = 1f + MathF.Sin(_gameOverTime * 3f) * 0.05f;
        int fontSize = (int)(50 * pulse);

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

        Raylib.DrawText(winner, textX, textY, fontSize, winColor);

        string scoreText = $"{_leftScore} - {_rightScore}";
        int scoreW = Raylib.MeasureText(scoreText, 36);
        Raylib.DrawText(scoreText, GameSettings.VirtualWidth / 2 - scoreW / 2,
            textY + fontSize + 10, 36, GameSettings.Text);

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
