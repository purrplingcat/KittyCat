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

    /// <summary>
    /// Returns the shared 1x1 white texture for <paramref name="device"/>.
    /// </summary>
    public static Texture2D GetPixel(GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (Pixels.TryGetValue(device, out var pixel) && !pixel.IsDisposed) return pixel;

        pixel = new Texture2D(device, 1, 1);
        pixel.SetData([Color.White]);
        Pixels[device] = pixel;
        return pixel;
    }

    /// <summary>
    /// Fills <paramref name="bounds"/> with a solid colour.
    /// </summary>
    public static void FillRect(this in LayerWriter writer, GraphicsDevice device, Rectangle bounds, Color color, float depth = 0f)
        => writer.Draw(GetPixel(device), bounds, color, depth);

    /// <summary>
    /// Draws the outline of <paramref name="bounds"/>.
    /// </summary>
    public static void DrawRect(
        this in LayerWriter writer,
        GraphicsDevice device,
        Rectangle bounds,
        Color color,
        int thickness = 1,
        float depth = 0f)
    {
        Span<Rectangle> edges = stackalloc Rectangle[4];
        var count = GetOutlineEdges(bounds, thickness, edges);
        if (count == 0) return;

        var pixel = GetPixel(device);
        for (var index = 0; index < count; index++)
            writer.Draw(pixel, edges[index], color, depth);
    }

    /// <summary>
    /// Draws an outline through an already open batch using its current transform and clipping.
    /// The caller owns Begin and End; thickness is measured in batch coordinates.
    /// </summary>
    public static void DrawRect(
        this ISpriteBatcher batcher,
        GraphicsDevice device,
        RenderResourceTable resources,
        FrameTextBuffer text,
        Rectangle bounds,
        Color color,
        int thickness = 1)
    {
        ArgumentNullException.ThrowIfNull(batcher);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(text);

        Span<Rectangle> edges = stackalloc Rectangle[4];
        var count = GetOutlineEdges(bounds, thickness, edges);
        if (count == 0) return;

        var textureIndex = resources.GetTextureIndex(GetPixel(device));
        for (var index = 0; index < count; index++)
        {
            var edge = edges[index];
            var command = new DrawCommand
            {
                Source = new Rectangle(0, 0, 1, 1),
                Position = new Vector2(edge.X, edge.Y),
                Scale = new Vector2(edge.Width, edge.Height),
                Color = color,
                ResourceIndex = textureIndex,
                Kind = DrawCommandKind.Sprite,
            };
            batcher.Submit(command, resources, text);
        }
    }

    private static int GetOutlineEdges(Rectangle bounds, int thickness, Span<Rectangle> edges)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || thickness <= 0) return 0;

        var count = 0;
        var topHeight = Math.Min(thickness, bounds.Height);
        edges[count++] = new Rectangle(bounds.X, bounds.Y, bounds.Width, topHeight);
        var bottomHeight = Math.Min(thickness, bounds.Height - topHeight);
        if (bottomHeight > 0)
            edges[count++] = new Rectangle(bounds.X, bounds.Bottom - bottomHeight, bounds.Width, bottomHeight);

        var sideHeight = bounds.Height - topHeight - bottomHeight;
        if (sideHeight <= 0) return count;

        var leftWidth = Math.Min(thickness, bounds.Width);
        edges[count++] = new Rectangle(bounds.X, bounds.Y + topHeight, leftWidth, sideHeight);
        var rightWidth = Math.Min(thickness, bounds.Width - leftWidth);
        if (rightWidth > 0)
            edges[count++] = new Rectangle(bounds.Right - rightWidth, bounds.Y + topHeight, rightWidth, sideHeight);
        return count;
    }

    /// <summary>
    /// Draws a straight line between two points.
    /// </summary>
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

    /// <summary>
    /// Draws a filled circle using horizontal spans.
    /// </summary>
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
