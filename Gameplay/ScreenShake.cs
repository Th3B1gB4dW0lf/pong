using System;
using System.Numerics;
using Raylib_cs;

namespace PongGameV2.Gameplay;

public class ScreenShake
{
    private float _timer;
    private float _intensity;

    public void Trigger(float intensity)
    {
        _timer = 0.15f;
        _intensity = intensity;
    }

    public void Stop()
    {
        _timer = 0f;
        _intensity = 0f;
    }

    public void Update(float dt)
    {
        if (_timer > 0f)
            _timer -= dt;
    }

    public Camera2D GetCamera()
    {
        float offsetX = 0, offsetY = 0;

        if (_timer > 0f)
        {
            offsetX = (Random.Shared.NextSingle() - 0.5f) * 2f * _intensity;
            offsetY = (Random.Shared.NextSingle() - 0.5f) * 2f * _intensity;
        }

        return new Camera2D
        {
            Offset = new Vector2(offsetX, offsetY),
            Target = Vector2.Zero,
            Rotation = 0f,
            Zoom = 1f,
        };
    }
}
