using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Particles;

/// <summary>
/// Mutable particle state usable without the engine, ECS, DI, or a graphics device.
/// </summary>
public struct ParticleState
{
    public Texture2D? Texture;
    public Rectangle Source;
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Acceleration;
    public float Age;
    public float Lifetime;
    public Color StartColor;
    public Color EndColor;
    public float StartScale;
    public float EndScale;
    public float Rotation;
    public float AngularVelocity;
    public float Depth;

    public ParticleState(in ParticleEmission emission)
    {
        Texture = emission.Texture;
        Source = emission.Source ?? emission.Texture?.Bounds ?? Rectangle.Empty;
        Position = emission.Position;
        Velocity = emission.Velocity;
        Acceleration = emission.Acceleration;
        Age = 0f;
        Lifetime = emission.Lifetime;
        StartColor = emission.StartColor;
        EndColor = emission.EndColor;
        StartScale = emission.StartScale;
        EndScale = emission.EndScale;
        Rotation = emission.Rotation;
        AngularVelocity = emission.AngularVelocity;
        Depth = emission.Depth;
    }

    public readonly bool IsAlive => Age < Lifetime;

    /// <summary>
    /// Current straight-alpha tint. Expired particles use the end color.
    /// </summary>
    public readonly Color Tint => Color.Lerp(StartColor, EndColor, IsAlive ? Age / Lifetime : 1f);

    /// <summary>
    /// Current sprite scale. Expired particles use the end scale.
    /// </summary>
    public readonly float Scale => MathHelper.Lerp(StartScale, EndScale, IsAlive ? Age / Lifetime : 1f);

    /// <summary>
    /// Advances motion and age, returning false when the owner should remove this particle.
    /// </summary>
    /// <param name="deltaTime">A finite, nonnegative elapsed time in seconds.</param>
    public bool Update(float deltaTime)
    {
        Age += deltaTime;
        if (!IsAlive) return false;

        Position += Velocity * deltaTime + Acceleration * (0.5f * deltaTime * deltaTime);
        Velocity += Acceleration * deltaTime;
        Rotation += AngularVelocity * deltaTime;
        return true;
    }
}