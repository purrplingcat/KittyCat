using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Toolkit.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Queues draw commands into a single layer.
/// </summary>
/// <remarks>
/// A ref struct so it cannot be stored or captured: a writer is valid only for the frame it was
/// obtained in. Nothing here touches the graphics device; commands are replayed when the owning
/// renderer flushes. Writers for unknown or disabled layers silently discard, which keeps
/// optional debug layers free of call-site checks.
/// </remarks>
public readonly ref struct LayerWriter
{
    private readonly RenderLayer? _layer;
    private readonly RenderResourceTable? _resources;
    private readonly FrameTextBuffer? _text;
    private readonly Rectangle _cullBounds;
    private readonly bool _culling;

    internal LayerWriter(RenderLayer layer, RenderResourceTable resources, FrameTextBuffer text)
    {
        _layer = layer;
        _resources = resources;
        _text = text;

        var bounds = layer.GetCullBounds();
        _culling = bounds.HasValue;
        _cullBounds = bounds ?? default;
    }

    /// <summary>False when the layer does not exist or is disabled, making every call a no-op.</summary>
    public bool IsActive => _layer is not null;

    /// <summary>Draws the whole texture at <paramref name="position"/>.</summary>
    public void Draw(Texture2D texture, Vector2 position, Color color, float depth = 0f)
    {
        if (_layer is null) return;

        Draw(texture, position, null, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, depth);
    }

    /// <summary>Stretches the whole texture over <paramref name="destination"/>.</summary>
    public void Draw(Texture2D texture, Rectangle destination, Color color, float depth = 0f)
    {
        if (_layer is null) return;

        var scale = new Vector2(
            (float)destination.Width / texture.Width,
            (float)destination.Height / texture.Height);

        Draw(
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

    /// <summary>Draws an atlas region at <paramref name="position"/>.</summary>
    public void Draw(in TextureRegion2D region, Vector2 position, Color color, float depth = 0f)
    {
        if (_layer is null) return;

        Draw(
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

    /// <summary>Queues a sprite with full control over its transform.</summary>
    public void Draw(
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
        if (_layer is null) return;

        ArgumentNullException.ThrowIfNull(texture);

        var bounds = source ?? new Rectangle(0, 0, texture.Width, texture.Height);
        if (IsCulled(position, origin, scale, bounds.Width, bounds.Height, rotation)) return;

        var index = _resources!.GetTextureIndex(texture);
        var command = new DrawCommand
        {
            Source = bounds,
            Position = position,
            Scale = scale,
            Origin = origin,
            Rotation = rotation,
            Depth = depth,
            Color = color,
            ResourceIndex = index,
            Kind = DrawCommandKind.Sprite,
            Effects = effects,
        };

        _layer.Commands.Add(command, index);
    }

    /// <summary>Queues a text run at <paramref name="position"/>.</summary>
    public void DrawString(SpriteFont font, ReadOnlySpan<char> text, Vector2 position, Color color, float depth = 0f)
    {
        if (_layer is null) return;

        DrawString(font, text, position, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, depth);
    }

    /// <summary>Queues a text run with full control over its transform.</summary>
    public void DrawString(
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
        if (_layer is null) return;

        ArgumentNullException.ThrowIfNull(font);

        if (text.IsEmpty) return;

        var index = _resources!.GetFontIndex(font);
        var command = new DrawCommand
        {
            Position = position,
            Scale = scale,
            Origin = origin,
            Rotation = rotation,
            Depth = depth,
            Color = color,
            TextOffset = _text!.Append(text),
            TextLength = (ushort)text.Length,
            ResourceIndex = index,
            Kind = DrawCommandKind.Text,
            Effects = effects,
        };

        _layer.Commands.Add(command, index);
    }

    private bool IsCulled(Vector2 position, Vector2 origin, Vector2 scale, int width, int height, float rotation)
    {
        // Rotated sprites would need an oriented bounding box; skipping them stays conservative.
        if (!_culling || rotation != 0f) return false;

        var left = position.X - origin.X * scale.X;
        var top = position.Y - origin.Y * scale.Y;

        return left + width * scale.X < _cullBounds.Left
            || left > _cullBounds.Right
            || top + height * scale.Y < _cullBounds.Top
            || top > _cullBounds.Bottom;
    }
}

/// <summary>
/// Per-renderer entry point for queueing draw commands.
/// </summary>
/// <remarks>
/// Resolving a layer also opens the frame when needed, so callers never have to begin or end
/// a frame explicitly regardless of the order in which they run.
/// </remarks>
public sealed class RenderContext
{
    private readonly Renderer2D _renderer;

    internal RenderContext(Renderer2D renderer) => _renderer = renderer;

    /// <summary>
    /// Returns a writer for <paramref name="id"/>. The writer is inactive when the layer is not
    /// declared on this renderer or is currently disabled.
    /// </summary>
    public LayerWriter Layer(RenderLayerId id) => _renderer.OpenLayer(id);
}
