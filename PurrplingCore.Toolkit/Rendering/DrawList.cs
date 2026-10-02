using System.Runtime.CompilerServices;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Growable, reusable command queue for a single render layer.
/// </summary>
/// <remarks>
/// Backing arrays are allocated once and grown by doubling; <see cref="Clear"/> only resets the
/// element count, so a steady-state frame allocates nothing. Sorting is indirect: only the key
/// and index arrays are permuted, which keeps command payloads in place.
/// </remarks>
public sealed class DrawList
{
    private DrawCommand[] _commands;
    private ulong[] _keys;
    private int[] _order;
    private int _count;
    private bool _useOrder;

    public DrawList(RenderSortMode sortMode, int capacity = 256)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        SortMode = sortMode;
        _commands = new DrawCommand[capacity];
        _keys = new ulong[capacity];
        _order = new int[capacity];
    }

    /// <summary>
    /// Ordering applied by <see cref="Sort"/>.
    /// </summary>
    public RenderSortMode SortMode { get; }

    /// <summary>
    /// Number of commands queued in the current frame.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Appends a command and records its sort key.
    /// </summary>
    /// <param name="resourceSortKey">
    /// Resource identity used by <see cref="RenderSortMode.Texture"/>; ignored by other modes.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(in DrawCommand command, int resourceSortKey = 0)
    {
        if (_count == _commands.Length)
        {
            Grow();
        }

        _commands[_count] = command;
        var resourceKey = ((int)command.Kind << 16) | (resourceSortKey & ushort.MaxValue);
        _keys[_count] = RenderSortKey.Create(SortMode, command.Depth, resourceKey, _count);
        _count++;
    }

    /// <summary>
    /// Orders the queued commands. Does no work for <see cref="RenderSortMode.Submission"/> or
    /// when the keys were already produced in ascending order.
    /// </summary>
    public void Sort()
    {
        _useOrder = false;

        if (SortMode == RenderSortMode.Submission || _count < 2 || IsAlreadyOrdered()) return;

        for (var i = 0; i < _count; i++)
        {
            _order[i] = i;
        }

        Array.Sort(_keys, _order, 0, _count);
        _useOrder = true;
    }

    /// <summary>
    /// Returns the command at <paramref name="index"/> in the order established by <see cref="Sort"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly DrawCommand At(int index) => ref _commands[_useOrder ? _order[index] : index];

    /// <summary>
    /// Discards the current frame's commands without releasing the backing arrays.
    /// </summary>
    public void Clear()
    {
        _count = 0;
        _useOrder = false;
    }

    private bool IsAlreadyOrdered()
    {
        var keys = _keys;
        for (var i = 1; i < _count; i++)
        {
            if (keys[i] < keys[i - 1]) return false;
        }

        return true;
    }

    private void Grow()
    {
        var capacity = _commands.Length * 2;
        Array.Resize(ref _commands, capacity);
        Array.Resize(ref _keys, capacity);
        Array.Resize(ref _order, capacity);
    }
}
