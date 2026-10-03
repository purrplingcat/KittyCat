using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Toolkit.Rendering;

namespace PurrplingCore.Toolkit.Particles;

/// <summary>
/// Optional sprite rendering for low-level particle state through the layered renderer.
/// </summary>
public static class ParticleRenderer2D
{
    /// <summary>
    /// Queues one live, textured particle without retaining its state or changing simulation.
    /// </summary>
    public static void Draw(in ParticleState particle, in LayerWriter writer)
    {
        if (!writer.IsActive || !particle.IsAlive) return;
        var texture = particle.Texture;
        if (texture is null) return;

        var tint = particle.Tint;
        writer.Draw(
            texture,
            particle.Position,
            particle.Source,
            Color.FromNonPremultiplied(tint.R, tint.G, tint.B, tint.A),
            particle.Rotation,
            new Vector2(particle.Source.Width * 0.5f, particle.Source.Height * 0.5f),
            new Vector2(particle.Scale),
            SpriteEffects.None,
            particle.Depth);
    }
}