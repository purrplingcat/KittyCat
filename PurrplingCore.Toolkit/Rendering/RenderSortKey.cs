using System.Runtime.CompilerServices;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>Determines how a layer orders its draw commands before they reach the batcher.</summary>
public enum RenderSortMode : byte
{
    /// <summary>Keep submission order. No sorting work is performed at all.</summary>
    Submission = 0,

    /// <summary>Order by ascending depth, so lower depth values are drawn first.</summary>
    FrontToBack,

    /// <summary>Order by descending depth, so higher depth values are drawn first.</summary>
    BackToFront,

    /// <summary>Group by texture to minimise texture switches inside a single batch.</summary>
    Texture,
}

/// <summary>
/// Packs the per-command sort criteria into a single <see cref="ulong"/> so a layer can be
/// ordered with one numeric sort instead of a comparer callback.
/// </summary>
/// <remarks>
/// Layers own separate command lists, so the layer identity is not part of the key and the
/// full 64 bits are available: 32 bits of primary criterion and 32 bits of submission sequence.
/// The sequence guarantees a deterministic order for equal primary criteria even though
/// <see cref="Array.Sort(Array, Array)"/> is not stable.
/// </remarks>
public static class RenderSortKey
{
    /// <summary>Builds the sort key for a command in a layer using <paramref name="mode"/>.</summary>
    /// <param name="depth">Depth criterion, used by the depth-ordered modes.</param>
    /// <param name="textureSortKey">Texture identity, used by <see cref="RenderSortMode.Texture"/>.</param>
    /// <param name="sequence">Submission index within the layer, used as the tie-breaker.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Create(RenderSortMode mode, float depth, int textureSortKey, int sequence)
    {
        var low = (uint)sequence;
        var high = mode switch
        {
            RenderSortMode.FrontToBack => Orderable(depth),
            RenderSortMode.BackToFront => ~Orderable(depth),
            RenderSortMode.Texture => (uint)textureSortKey,
            _ => 0u,
        };

        return ((ulong)high << 32) | low;
    }

    /// <summary>
    /// Maps a float onto an unsigned integer that preserves the original ordering, including
    /// negative values, so depth can be compared as part of an integer key.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Orderable(float value)
    {
        var bits = BitConverter.SingleToUInt32Bits(value);
        // Negative floats have descending bit patterns; flipping all bits restores ascending order.
        return (bits & 0x8000_0000u) != 0 ? ~bits : bits | 0x8000_0000u;
    }
}
