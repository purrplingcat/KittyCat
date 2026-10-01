using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Shape drawing built on a shared single-pixel texture, for debug visuals, bars, and flat UI.
/// </summary>
/// <remarks>
/// The pixel is created once per graphics device and reused, so games do not have to own one.
/// Every shape becomes ordinary sprite commands and batches with the rest of its layer.
/// </remarks>
public static class Primitives2D
{
    private static readonly Dictionary<GraphicsDevice, Texture2D> Pixels = [];

    /// <summary>Returns the shared 1x1 white texture for <paramref name="device"/>.</summary>
    public static Texture2D GetPixel(GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (Pixels.TryGetValue(device, out var pixel) && !pixel.IsDisposed) return pixel;

        pixel = new Texture2D(device, 1, 1);
        pixel.SetData([Color.White]);
        Pixels[device] = pixel;
        return pixel;
    }

    /// <summary>Fills <paramref name="bounds"/> with a solid colour.</summary>
    public static void FillRect(this in LayerWriter writer, GraphicsDevice device, Rectangle bounds, Color color, float depth = 0f)
        => writer.Draw(GetPixel(device), bounds, color, depth);

    /// <summary>Draws the outline of <paramref name="bounds"/>.</summary>
    public static void DrawRect(
        this in LayerWriter writer,
        GraphicsDevice device,
        Rectangle bounds,
        Color color,
        int thickness = 1,
        float depth = 0f)
    {
        var pixel = GetPixel(device);
        writer.Draw(pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color, depth);
        writer.Draw(pixel, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color, depth);
        writer.Draw(pixel, new Rectangle(bounds.X, bounds.Y + thickness, thickness, bounds.Height - thickness * 2), color, depth);
        writer.Draw(pixel, new Rectangle(bounds.Right - thickness, bounds.Y + thickness, thickness, bounds.Height - thickness * 2), color, depth);
    }

    /// <summary>Draws a straight line between two points.</summary>
    public static void DrawLine(
        this in LayerWriter writer,
        GraphicsDevice device,
        Vector2 from,
        Vector2 to,
        Color color,
        float thickness = 1f,
        float depth = 0f)
    {
        var delta = to - from;
        var length = delta.Length();
        if (length <= float.Epsilon) return;

        writer.Draw(
            GetPixel(device),
            from,
            null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            depth);
    }

    /// <summary>Draws a filled circle using horizontal spans.</summary>
    public static void FillCircle(
        this in LayerWriter writer,
        GraphicsDevice device,
        Vector2 center,
        int radius,
        Color color,
        float depth = 0f)
    {
        if (radius <= 0) return;

        var pixel = GetPixel(device);
        var centerX = (int)center.X;
        var centerY = (int)center.Y;

        for (var offsetY = -radius; offsetY <= radius; offsetY++)
        {
            var halfWidth = (int)MathF.Sqrt(radius * radius - offsetY * offsetY);
            writer.Draw(
                pixel,
                new Rectangle(centerX - halfWidth, centerY + offsetY, halfWidth * 2 + 1, 1),
                color,
                depth);
        }
    }
}
