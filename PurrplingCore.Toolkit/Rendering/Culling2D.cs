using Microsoft.Xna.Framework;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Conservative 2D visibility tests independent of submission types and graphics resources.
/// </summary>
public static class Culling2D
{
    /// <summary>
    /// Returns true when world-space bounds are entirely outside the visible region.
    /// Missing cull bounds disable culling; touching edges remain visible.
    /// </summary>
    public static bool IsCulled(Rectangle? cullBounds, Rectangle bounds)
        => IsCulled(cullBounds, bounds.Location.ToVector2(), Vector2.Zero, Vector2.One, bounds.Width, bounds.Height);

    /// <summary>
    /// Tests a transformed local rectangle against world-space cull bounds, including mirrored scales.
    /// Missing cull bounds or nonzero rotation conservatively keep the rectangle visible.
    /// </summary>
    public static bool IsCulled(
        Rectangle? cullBounds,
        Vector2 position,
        Vector2 origin,
        Vector2 scale,
        int width,
        int height,
        float rotation = 0f)
    {
        if (!cullBounds.HasValue || rotation != 0f) return false;

        var bounds = cullBounds.Value;
        var first = position - origin * scale;
        var second = first + new Vector2(width, height) * scale;

        return MathF.Max(first.X, second.X) < bounds.Left
            || MathF.Min(first.X, second.X) > bounds.Right
            || MathF.Max(first.Y, second.Y) < bounds.Top
            || MathF.Min(first.Y, second.Y) > bounds.Bottom;
    }

    /// <summary>
    /// Tests world-space bounds against the writer's sampled visible region.
    /// This does not check whether the writer is active.
    /// </summary>
    public static bool IsCulled<T>(this in LayerWriter<T> writer, Rectangle bounds) where T : struct
        => IsCulled(writer.CullBounds, bounds);

    /// <summary>
    /// Tests a transformed local rectangle against the writer's sampled visible region.
    /// This does not check whether the writer is active.
    /// </summary>
    public static bool IsCulled<T>(
        this in LayerWriter<T> writer,
        Vector2 position,
        Vector2 origin,
        Vector2 scale,
        int width,
        int height,
        float rotation = 0f) where T : struct
        => IsCulled(writer.CullBounds, position, origin, scale, width, height, rotation);
}