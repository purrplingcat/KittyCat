using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Maps graphics resources to the dense indices stored in <see cref="DrawCommand"/>.
/// </summary>
/// <remarks>
/// The table persists across frames because textures and fonts outlive a single frame, so a
/// steady-state frame performs lookups only and never grows the backing storage. Identity is
/// compared by reference to avoid virtual equality calls on the submission path.
/// </remarks>
public sealed class RenderResourceTable
{
    private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceComparer<T> Instance = new();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }

    private readonly Dictionary<Texture2D, ushort> _textureIndices = new(64, ReferenceComparer<Texture2D>.Instance);
    private readonly Dictionary<SpriteFont, ushort> _fontIndices = new(8, ReferenceComparer<SpriteFont>.Instance);
    private readonly Dictionary<VertexBuffer, ushort> _vertexBufferIndices = new(16, ReferenceComparer<VertexBuffer>.Instance);
    private Texture2D[] _textures = new Texture2D[64];
    private SpriteFont[] _fonts = new SpriteFont[8];
    private VertexBuffer[] _vertexBuffers = new VertexBuffer[16];
    private int _textureCount;
    private int _fontCount;
    private int _vertexBufferCount;

    /// <summary>
    /// Returns the index of <paramref name="texture"/>, registering it on first use.
    /// </summary>
    public ushort GetTextureIndex(Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);

        if (_textureIndices.TryGetValue(texture, out var index)) return index;

        return Register(texture);
    }

    /// <summary>
    /// Returns the index of <paramref name="font"/>, registering it on first use.
    /// </summary>
    public ushort GetFontIndex(SpriteFont font)
    {
        ArgumentNullException.ThrowIfNull(font);

        if (_fontIndices.TryGetValue(font, out var index)) return index;

        return Register(font);
    }

    /// <summary>
    /// Returns the index of <paramref name="vertexBuffer"/>, registering it on first use.
    /// </summary>
    public ushort GetVertexBufferIndex(VertexBuffer vertexBuffer)
    {
        ArgumentNullException.ThrowIfNull(vertexBuffer);

        if (_vertexBufferIndices.TryGetValue(vertexBuffer, out var index)) return index;

        return Register(vertexBuffer);
    }

    /// <summary>
    /// Resolves a texture index produced by <see cref="GetTextureIndex"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Texture2D GetTexture(ushort index) => _textures[index];

    /// <summary>
    /// Resolves a font index produced by <see cref="GetFontIndex"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpriteFont GetFont(ushort index) => _fonts[index];

    /// <summary>
    /// Resolves a vertex-buffer index produced by <see cref="GetVertexBufferIndex"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VertexBuffer GetVertexBuffer(ushort index) => _vertexBuffers[index];

    /// <summary>
    /// Drops all registrations. Call after unloading content so disposed resources are released.
    /// </summary>
    public void Reset()
    {
        Array.Clear(_textures, 0, _textureCount);
        Array.Clear(_fonts, 0, _fontCount);
        Array.Clear(_vertexBuffers, 0, _vertexBufferCount);
        _textureIndices.Clear();
        _fontIndices.Clear();
        _vertexBufferIndices.Clear();
        _textureCount = 0;
        _fontCount = 0;
        _vertexBufferCount = 0;
    }

    private ushort Register(Texture2D texture)
    {
        if (_textureCount == _textures.Length)
        {
            Array.Resize(ref _textures, _textures.Length * 2);
        }

        var index = Checked(_textureCount++);
        _textures[index] = texture;
        _textureIndices.Add(texture, index);
        return index;
    }

    private ushort Register(SpriteFont font)
    {
        if (_fontCount == _fonts.Length)
        {
            Array.Resize(ref _fonts, _fonts.Length * 2);
        }

        var index = Checked(_fontCount++);
        _fonts[index] = font;
        _fontIndices.Add(font, index);
        return index;
    }

    private ushort Register(VertexBuffer vertexBuffer)
    {
        if (_vertexBufferCount == _vertexBuffers.Length)
        {
            Array.Resize(ref _vertexBuffers, _vertexBuffers.Length * 2);
        }

        var index = Checked(_vertexBufferCount++);
        _vertexBuffers[index] = vertexBuffer;
        _vertexBufferIndices.Add(vertexBuffer, index);
        return index;
    }

    private static ushort Checked(int index)
    {
        if (index > ushort.MaxValue)
        {
            throw new InvalidOperationException(
                $"A render resource table cannot hold more than {ushort.MaxValue + 1} resources of one kind.");
        }

        return (ushort)index;
    }
}
