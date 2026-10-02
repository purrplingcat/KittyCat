using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Discriminates the payload carried by a <see cref="DrawCommand"/>.
/// </summary>
public enum DrawCommandKind : byte
{
    Sprite = 0,
    Text,
    Primitive,
}

/// <summary>
/// A single queued draw operation. Commands are appended during submission and replayed
/// by a batcher when the owning renderer flushes.
/// </summary>
/// <remarks>
/// Resources live in a <see cref="RenderResourceTable"/> and text lives in a
/// <see cref="FrameTextBuffer"/>, both addressed by index, so commands contain no object references.
/// Fields are shared by command kinds where possible to keep the command array compact.
/// </remarks>
public struct DrawCommand
{
    /// <summary>
    /// Source rectangle for sprites; unused for text and primitive commands.
    /// </summary>
    public Rectangle Source;

    /// <summary>
    /// Destination position in the layer's coordinate space.
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// Scale applied around <see cref="Origin"/>.
    /// </summary>
    public Vector2 Scale;

    /// <summary>
    /// Rotation and scale pivot, in source pixels.
    /// </summary>
    public Vector2 Origin;

    /// <summary>
    /// Rotation in radians.
    /// </summary>
    public float Rotation;

    /// <summary>
    /// Sort criterion for the depth-ordered modes.
    /// </summary>
    public float Depth;

    /// <summary>
    /// Tint multiplied with the sampled colour.
    /// </summary>
    public Color Color;

    /// <summary>
    /// Start of the text run inside the frame text buffer.
    /// </summary>
    public int TextOffset;

    /// <summary>
    /// Length of the text run inside the frame text buffer.
    /// </summary>
    public ushort TextLength;

    /// <summary>
    /// Index of the texture, font, or vertex buffer according to <see cref="Kind"/>.
    /// </summary>
    public ushort ResourceIndex;

    /// <summary>
    /// Selects how the remaining fields are interpreted.
    /// </summary>
    public DrawCommandKind Kind;

    /// <summary>
    /// Horizontal and vertical flip flags.
    /// </summary>
    public SpriteEffects Effects;

    /// <summary>
    /// Primitive topology for <see cref="DrawCommandKind.Primitive"/>.
    /// </summary>
    public PrimitiveType PrimitiveType;

    /// <summary>
    /// Number of primitives for <see cref="DrawCommandKind.Primitive"/>.
    /// </summary>
    public int PrimitiveCount;

    /// <summary>
    /// First vertex for <see cref="DrawCommandKind.Primitive"/>.
    /// </summary>
    public int VertexStart;
}
