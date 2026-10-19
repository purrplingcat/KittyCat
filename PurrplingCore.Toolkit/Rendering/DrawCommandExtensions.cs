using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Toolkit.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Sprite, text, and primitive submission helpers for draw-command writers.
/// </summary>
public static class DrawCommandExtensions
{
    /// <summary>
    /// Queues non-indexed vertex-buffer primitives in the same ordered stream as sprites and text.
    /// </summary>
    public static void DrawPrimitives(
        this in LayerWriter<DrawCommand> writer,
        VertexBuffer vertexBuffer,
        PrimitiveType primitiveType,
        int primitiveCount,
        Vector2 position,
        float depth = 0f)
        => writer.DrawPrimitives(vertexBuffer, primitiveType, primitiveCount, position, Vector2.One, depth: depth);

    /// <summary>
    /// Queues vertex-buffer primitives with a per-command 2D transform.
    /// </summary>
    public static void DrawPrimitives(
        this in LayerWriter<DrawCommand> writer,
        VertexBuffer vertexBuffer,
        PrimitiveType primitiveType,
        int primitiveCount,
        Vector2 position,
        Vector2 scale,
        float rotation = 0f,
        Vector2 origin = default,
        float depth = 0f,
        int vertexStart = 0)
    {
        if (!writer.IsActive) return;

        ArgumentNullException.ThrowIfNull(vertexBuffer);

        var command = new DrawCommand
        {
            Position = position,
            Scale = scale,
            Origin = origin,
            Rotation = rotation,
            Depth = depth,
            Kind = DrawCommandKind.Primitive,
            PrimitiveType = primitiveType,
            PrimitiveCount = primitiveCount,
            VertexStart = vertexStart,
            ResourceIndex = writer.Resources!.GetVertexBufferIndex(vertexBuffer),
        };

        writer.Submit(in command);
    }

    /// <summary>
    /// Draws the whole texture at <paramref name="position"/>.
    /// </summary>
    public static void Draw(this in LayerWriter<DrawCommand> writer, Texture2D texture, Vector2 position, Color color, float depth = 0f)
    {
        if (!writer.IsActive) return;

        writer.Draw(texture, position, null, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, depth);
    }

    /// <summary>
    /// Stretches the whole texture over <paramref name="destination"/>.
    /// </summary>
    public static void Draw(this in LayerWriter<DrawCommand> writer, Texture2D texture, Rectangle destination, Color color, float depth = 0f)
    {
        if (!writer.IsActive) return;

        var scale = new Vector2(
            (float)destination.Width / texture.Width,
            (float)destination.Height / texture.Height);

        writer.Draw(
            texture,
            new Vector2(destination.X, destination.Y),
            null,
            color,
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            depth);
    }

    /// <summary>
    /// Draws an atlas region at <paramref name="position"/>.
    /// </summary>
    public static void Draw(this in LayerWriter<DrawCommand> writer, in TextureRegion2D region, Vector2 position, Color color, float depth = 0f)
    {
        if (!writer.IsActive) return;

        writer.Draw(
            region.Texture,
            position,
            region.Bounds,
            color,
            0f,
            Vector2.Zero,
            Vector2.One,
            SpriteEffects.None,
            depth);
    }

    /// <summary>
    /// Queues a sprite with full control over its transform.
    /// </summary>
    public static void Draw(
        this in LayerWriter<DrawCommand> writer,
        Texture2D texture,
        Vector2 position,
        Rectangle? source,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float depth)
    {
        if (!writer.IsActive) return;

        ArgumentNullException.ThrowIfNull(texture);

        var bounds = source ?? new Rectangle(0, 0, texture.Width, texture.Height);
        if (writer.IsCulled(position, origin, scale, bounds.Width, bounds.Height, rotation)) return;

        var command = new DrawCommand
        {
            Source = bounds,
            Position = position,
            Scale = scale,
            Origin = origin,
            Rotation = rotation,
            Depth = depth,
            Color = color,
            Kind = DrawCommandKind.Sprite,
            Effects = effects,
            ResourceIndex = writer.Resources!.GetTextureIndex(texture),
        };

        writer.Submit(in command);
    }

    /// <summary>
    /// Queues a text run at <paramref name="position"/>.
    /// </summary>
    public static void DrawString(
        this in LayerWriter<DrawCommand> writer,
        SpriteFont font,
        ReadOnlySpan<char> text,
        Vector2 position,
        Color color,
        float depth = 0f)
    {
        if (!writer.IsActive) return;

        writer.DrawString(font, text, position, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, depth);
    }

    /// <summary>
    /// Queues a text run with full control over its transform.
    /// </summary>
    public static void DrawString(
        this in LayerWriter<DrawCommand> writer,
        SpriteFont font,
        ReadOnlySpan<char> text,
        Vector2 position,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float depth)
    {
        if (!writer.IsActive) return;

        ArgumentNullException.ThrowIfNull(font);

        if (text.IsEmpty) return;

        var command = new DrawCommand
        {
            Position = position,
            Scale = scale,
            Origin = origin,
            Rotation = rotation,
            Depth = depth,
            Color = color,
            Kind = DrawCommandKind.Text,
            Effects = effects,
            ResourceIndex = writer.Resources!.GetFontIndex(font),
            TextOffset = writer.Text!.Append(text),
            TextLength = (ushort)text.Length,
        };

        writer.Submit(in command);
    }
}