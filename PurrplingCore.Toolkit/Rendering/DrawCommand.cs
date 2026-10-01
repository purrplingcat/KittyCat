using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>Discriminates the payload carried by a <see cref="DrawCommand"/>.</summary>
public enum DrawCommandKind : byte
{
    Sprite = 0,
    Text,
}

/// <summary>
/// A single queued draw operation. Commands are appended during submission and replayed
/// by a batcher when the owning renderer flushes.
/// </summary>
/// <remarks>
/// The struct deliberately holds no object references: resources live in a
/// <see cref="RenderResourceTable"/> and text lives in a <see cref="FrameTextBuffer"/>, both
/// addressed by index. That keeps command arrays contiguous, invisible to the GC during
/// collection scans, and cheap to copy. Field order packs the struct into exactly 64 bytes.
/// </remarks>
public struct DrawCommand
{
    /// <summary>Source rectangle within the texture. Unused for <see cref="DrawCommandKind.Text"/>.</summary>
    public Rectangle Source;

    /// <summary>Destination position in the layer's coordinate space.</summary>
    public Vector2 Position;

    /// <summary>Scale applied around <see cref="Origin"/>.</summary>
    public Vector2 Scale;

    /// <summary>Rotation and scale pivot, in source pixels.</summary>
    public Vector2 Origin;

    /// <summary>Rotation in radians.</summary>
    public float Rotation;

    /// <summary>Sort criterion for the depth-ordered modes.</summary>
    public float Depth;

    /// <summary>Tint multiplied with the sampled colour.</summary>
    public Color Color;

    /// <summary>Start of the text run inside the frame text buffer.</summary>
    public int TextOffset;

    /// <summary>Length of the text run inside the frame text buffer.</summary>
    public ushort TextLength;

    /// <summary>Index of the texture or font inside the renderer's resource table.</summary>
    public ushort ResourceIndex;

    /// <summary>Selects how the remaining fields are interpreted.</summary>
    public DrawCommandKind Kind;

    /// <summary>Horizontal and vertical flip flags.</summary>
    public SpriteEffects Effects;
}
