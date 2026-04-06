using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PongGameV2.Gameplay;

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
            p.Velocity *= 1f - 2.5f * dt;
            p.Velocity.Y += 40f * dt;
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

            byte glowAlpha = (byte)(t * 60);
            Raylib.DrawCircleV(p.Position, size * 3f, new Color(p.Color.R, p.Color.G, p.Color.B, glowAlpha));
            Raylib.DrawCircleV(p.Position, size, c);

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
