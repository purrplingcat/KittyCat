using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Stable identifier of a render layer, interned by name so the same name always
/// resolves to the same value across renderers and assemblies.
/// </summary>
/// <remarks>
/// Identifiers are allocated during composition, never per frame. The numeric
/// <see cref="Value"/> is what the renderer uses to index its per-layer storage.
/// </remarks>
public readonly struct RenderLayerId : IEquatable<RenderLayerId>
{
    private static readonly ConcurrentDictionary<string, RenderLayerId> Interned = new(StringComparer.Ordinal);
    private static int _nextValue = -1;

    private readonly string? _name;

    private RenderLayerId(ushort value, string name)
    {
        Value = value;
        _name = name;
    }

    /// <summary>Dense zero-based index used by renderers to address per-layer storage.</summary>
    public ushort Value { get; }

    /// <summary>Human readable name used for diagnostics and registration.</summary>
    public string Name => _name ?? string.Empty;

    /// <summary>Indicates whether this identifier was produced by <see cref="Of"/>.</summary>
    public bool IsValid => _name is not null;

    /// <summary>
    /// Returns the identifier for <paramref name="name"/>, allocating a new one on first use.
    /// </summary>
    public static RenderLayerId Of(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return Interned.GetOrAdd(name, static key =>
        {
            var value = Interlocked.Increment(ref _nextValue);
            if (value > ushort.MaxValue)
            {
                throw new InvalidOperationException($"Cannot allocate more than {ushort.MaxValue + 1} render layers.");
            }

            return new RenderLayerId((ushort)value, key);
        });
    }

    public bool Equals(RenderLayerId other) => Value == other.Value && IsValid == other.IsValid;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is RenderLayerId other && Equals(other);

    public override int GetHashCode() => Value;

    public override string ToString() => Name;

    public static bool operator ==(RenderLayerId left, RenderLayerId right) => left.Equals(right);

    public static bool operator !=(RenderLayerId left, RenderLayerId right) => !left.Equals(right);
}
