using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Particles;

/// <summary>
/// Describes one particle in logical units without choosing a simulation owner or renderer.
/// </summary>
public readonly record struct ParticleEmission
{
    public ParticleEmission() { }

    /// <summary>
    /// Optional borrowed texture for sprite rendering. Simulation does not require a texture.
    /// </summary>
    public Texture2D? Texture { get; init; }
    public Rectangle? Source { get; init; }
    public Vector2 Position { get; init; }
    public Vector2 Velocity { get; init; }
    public Vector2 Acceleration { get; init; }
    public float Lifetime { get; init; } = 1f;

    /// <summary>
    /// Straight-alpha tint at birth, interpolated toward EndColor over the lifetime.
    /// </summary>
    public Color StartColor { get; init; } = Color.White;

    public Color EndColor { get; init; } = Color.Transparent;
    public float StartScale { get; init; } = 1f;
    public float EndScale { get; init; }
    public float Rotation { get; init; }
    public float AngularVelocity { get; init; }
    public float Depth { get; init; }
}