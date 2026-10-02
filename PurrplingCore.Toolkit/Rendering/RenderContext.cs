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

    /// <summary>
    /// False when the layer does not exist or is disabled, making every call a no-op.
    /// </summary>
    public bool IsActive => _layer is not null;

    /// <summary>
    /// Queues a fully populated command without resolving or modifying its resource index.
    /// </summary>
    /// <remarks>
    /// The caller must provide valid resource indices and, for text, offsets into the current frame text buffer.
    /// </remarks>
    public void Submit(in DrawCommand command)
    {
        if (_layer is null) return;

        _layer.Commands.Add(command, command.ResourceIndex);
    }

    /// <summary>
    /// Queues non-indexed vertex-buffer primitives in the same ordered command stream as sprites and text.
    /// </summary>
    public void DrawPrimitives(
        VertexBuffer vertexBuffer,
        PrimitiveType primitiveType,
        int primitiveCount,
        Vector2 position,
        float depth = 0f)
        => DrawPrimitives(vertexBuffer, primitiveType, primitiveCount, position, Vector2.One, depth: depth);

    /// <summary>
    /// Queues vertex-buffer primitives with a per-command 2D transform.
    /// </summary>
    public void DrawPrimitives(
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
        if (_layer is null) return;

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
        };

        var resourceIndex = _resources!.GetVertexBufferIndex(vertexBuffer);
        command.ResourceIndex = resourceIndex;
        _layer.Commands.Add(command, resourceIndex);
    }

    /// <summary>
    /// Draws the whole texture at <paramref name="position"/>.
    /// </summary>
    public void Draw(Texture2D texture, Vector2 position, Color color, float depth = 0f)
    {
        if (_layer is null) return;

        Draw(texture, position, null, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, depth);
    }

    /// <summary>
    /// Stretches the whole texture over <paramref name="destination"/>.
    /// </summary>
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

    /// <summary>
    /// Draws an atlas region at <paramref name="position"/>.
    /// </summary>
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

    /// <summary>
    /// Queues a sprite with full control over its transform.
    /// </summary>
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
        if (IsCulled(position, origin, scale, bounds.Width, bounds.Height, rotation)) 
            return;

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
        };

        var resourceIndex = _resources!.GetTextureIndex(texture);
        command.ResourceIndex = resourceIndex;
        _layer.Commands.Add(command, resourceIndex);
    }

    /// <summary>
    /// Queues a text run at <paramref name="position"/>.
    /// </summary>
    public void DrawString(SpriteFont font, ReadOnlySpan<char> text, Vector2 position, Color color, float depth = 0f)
    {
        if (_layer is null) return;

        DrawString(font, text, position, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, depth);
    }

    /// <summary>
    /// Queues a text run with full control over its transform.
    /// </summary>
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
        };

        var resourceIndex = _resources!.GetFontIndex(font);
        command.ResourceIndex = resourceIndex;
        command.TextOffset = _text!.Append(text);
        command.TextLength = (ushort)text.Length;
        _layer.Commands.Add(command, resourceIndex);
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

    private static int GetRequiredVertexCount(PrimitiveType primitiveType, int primitiveCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(primitiveCount, 1);

        return primitiveType switch
        {
            PrimitiveType.TriangleList => checked(primitiveCount * 3),
            PrimitiveType.TriangleStrip => checked(primitiveCount + 2),
            PrimitiveType.LineList => checked(primitiveCount * 2),
            PrimitiveType.LineStrip => checked(primitiveCount + 1),
            _ => throw new ArgumentOutOfRangeException(nameof(primitiveType), primitiveType, "Unsupported primitive type."),
        };
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
    /// Gets the renderer-owned resource table for raw command construction.
    /// </summary>
    public RenderResourceTable Resources => _renderer.Resources;

    /// <summary>
    /// Gets the current frame text buffer for raw text command construction.
    /// </summary>
    public FrameTextBuffer TextBuffer => _renderer.TextBuffer;

    /// <summary>
    /// Returns a writer for <paramref name="id"/>. The writer is inactive when the layer is not
    /// declared on this renderer or is currently disabled.
    /// </summary>
    public LayerWriter Layer(RenderLayerId id) => _renderer.OpenLayer(id);

    /// <summary>
    /// Returns a writer for submissions accepted by the layer's <see cref="ILayerSubmissionPass{T}"/>.
    /// </summary>
    public LayerSubmissionWriter<T> Submissions<T>(RenderLayerId id) where T : struct
        => _renderer.OpenSubmissions<T>(id);
}

/// <summary>
/// Queues frame-scoped values for a layer pass that accepts <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// The writer cannot be stored or captured and never accesses the graphics device.
/// </remarks>
public readonly ref struct LayerSubmissionWriter<T> where T : struct
{
    private readonly ILayerSubmissionPass<T>? _pass;

    internal LayerSubmissionWriter(ILayerSubmissionPass<T>? pass) => _pass = pass;

    /// <summary>
    /// Indicates whether the layer has a compatible active submission pass.
    /// </summary>
    public bool IsActive => _pass is not null;

    /// <summary>
    /// Queues one value for the layer pass to consume during renderer flush.
    /// </summary>
    public void Submit(in T submission) => _pass?.Submit(in submission);
}
