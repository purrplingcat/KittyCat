namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Frame-scoped character storage for queued text commands.
/// </summary>
/// <remarks>
/// Text submission takes a <see cref="ReadOnlySpan{T}"/> and copies it here instead of keeping a
/// string reference, so callers can format numbers into a stack buffer without allocating. The
/// backing array is reused between frames; <see cref="Clear"/> only resets the write position.
/// </remarks>
public sealed class FrameTextBuffer
{
    private char[] _buffer = new char[1024];
    private int _length;

    /// <summary>
    /// Copies <paramref name="text"/> into the buffer and returns its start offset.
    /// </summary>
    public int Append(ReadOnlySpan<char> text)
    {
        if (text.Length > ushort.MaxValue)
        {
            throw new ArgumentException(
                $"A single text command cannot exceed {ushort.MaxValue} characters.", nameof(text));
        }

        EnsureCapacity(_length + text.Length);

        var offset = _length;
        text.CopyTo(_buffer.AsSpan(offset));
        _length += text.Length;
        return offset;
    }

    /// <summary>
    /// Returns the characters previously written by <see cref="Append"/>.
    /// </summary>
    public ReadOnlySpan<char> Slice(int offset, int length) => _buffer.AsSpan(offset, length);

    /// <summary>
    /// Discards the current frame's text without releasing the backing array.
    /// </summary>
    public void Clear() => _length = 0;

    private void EnsureCapacity(int required)
    {
        if (required <= _buffer.Length) return;

        var capacity = _buffer.Length;
        do
        {
            capacity *= 2;
        }
        while (capacity < required);

        Array.Resize(ref _buffer, capacity);
    }
}
