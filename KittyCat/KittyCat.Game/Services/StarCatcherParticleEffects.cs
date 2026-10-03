using System;
using Friflo.Engine.ECS;
using Microsoft.Xna.Framework;
using PurrplingCore.Ecs.Particles;
using PurrplingCore.Graphics;
using PurrplingCore.Toolkit.Particles;
using PurrplingCore.Toolkit.Rendering;

namespace KittyCat.Services;

/// <summary>
/// Example particle recipes for collectible bursts and falling-object trails.
/// </summary>
public sealed class StarCatcherParticleEffects(GraphicsManager graphics)
{
    public void Burst(CommandBuffer commands, Vector2 position, Color color, int count = 24)
    {
        var texture = Primitives2D.GetPixel(graphics.GraphicsDevice);
        for (var index = 0; index < count; index++)
        {
            var angle = Random.Shared.NextSingle() * MathHelper.TwoPi;
            var speed = 40f + Random.Shared.NextSingle() * 85f;
            var emission = new ParticleEmission
            {
                Texture = texture,
                Position = position,
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                Acceleration = new Vector2(0f, 150f),
                Lifetime = 0.4f + Random.Shared.NextSingle() * 0.4f,
                StartColor = color,
                EndColor = new Color(color.R, color.G, color.B, (byte)0),
                StartScale = 3f + Random.Shared.NextSingle() * 2f,
                EndScale = 0.5f,
                Rotation = angle,
                AngularVelocity = (Random.Shared.NextSingle() - 0.5f) * 8f,
            };
            commands.EmitParticle(emission);
        }
    }

    public void Trail(CommandBuffer commands, Vector2 position, Color color)
    {
        commands.EmitParticle(new ParticleEmission
        {
            Texture = Primitives2D.GetPixel(graphics.GraphicsDevice),
            Position = position + new Vector2((Random.Shared.NextSingle() - 0.5f) * 8f, 0f),
            Velocity = new Vector2((Random.Shared.NextSingle() - 0.5f) * 16f, -20f),
            Lifetime = 0.33f + Random.Shared.NextSingle() * 0.25f,
            StartColor = new Color(color.R, color.G, color.B, (byte)230),
            EndColor = new Color(color.R, color.G, color.B, (byte)0),
            StartScale = 3f + Random.Shared.NextSingle() * 2f,
            EndScale = 0.75f,
        });
    }
}