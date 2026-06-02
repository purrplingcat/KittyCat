using DeterministicGuids;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PurrplingCore.Toolkit.Attributes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PurrplingCore.Toolkit.Content;

[DebuggerDisplay("{QualifiedId}")]
public readonly struct AssetDescriptor : IEquatable<AssetDescriptor>
{
    public readonly Guid Guid;
    public readonly string QualifiedId;
    private readonly ushort _colon;
    private readonly ushort _lastSlash;

    public static readonly AssetDescriptor Empty = default;

    public ReadOnlySpan<char> Ns => QualifiedId.AsSpan(0, _colon);

    public ReadOnlySpan<char> Category => _lastSlash == 0
        ? default
        : QualifiedId.AsSpan(_colon + 1, _lastSlash - _colon - 1);

    public ReadOnlySpan<char> Key => _lastSlash == 0
        ? QualifiedId.AsSpan(_colon + 1)
        : QualifiedId.AsSpan(_lastSlash + 1);

    [Cold]
    public AssetDescriptor(string ns, string? categoryPath, string key)
    {
        QualifiedId = Qualify(ns, categoryPath, key);
        Guid = GetGuid(QualifiedId);

        _colon = (ushort)ns.Length;
        _lastSlash = string.IsNullOrEmpty(categoryPath)
            ? (ushort)0
            : (ushort)(ns.Length + 1 + categoryPath.Length);
    }

    [Cold]
    public static AssetDescriptor Parse(string raw)
    {
        int colon = raw.IndexOf(':');
        int lastSlash = raw.LastIndexOf('/');

        if (raw.Length > ushort.MaxValue)
            throw new ArgumentException("Asset id is too long!");

        if (colon == -1)
            throw new ArgumentException($"Invalid AssetId: '{raw}'. Missing ':'.");

        if (lastSlash < colon) 
            lastSlash = -1;

        return new AssetDescriptor(raw, (ushort)colon, lastSlash == -1 ? (ushort)0 : (ushort)lastSlash);
    }

    private AssetDescriptor(string raw, ushort colon, ushort lastSlash)
    {
        QualifiedId = raw;
        _colon = colon;
        _lastSlash = lastSlash;
        Guid = GetGuid(raw);
    }

    [Cold]
    public static string Qualify(string ns, string? categoryPath, string id)
    {
        if (string.IsNullOrEmpty(categoryPath)) return $"{ns}:{id}";

        var path = categoryPath.Trim('/');
        return $"{ns}:{path}/{id}";
    }

    public bool Equals(AssetDescriptor other) => Guid == other.Guid;

    public override string ToString() => QualifiedId;

    public override int GetHashCode()
    {
        return Guid.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid GetGuid(string qualifiedId)
    {
        return DeterministicGuid.Create(Namespaces.Assets, qualifiedId);
    }

    [Cold]
    public override bool Equals(object? obj)
    {
        return obj is AssetDescriptor id && Equals(id);
    }
    public static bool operator ==(AssetDescriptor left, AssetDescriptor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AssetDescriptor left, AssetDescriptor right)
    {
        return !left.Equals(right);
    }
}

public interface IAsset
{
    string Key { get; }
    Type Type { get; }
}

public interface IRegistry
{
    IAsset? Get(Guid guid);
    IEnumerable<AssetDescriptor> GetAllIds(); // Pro výpis tabulky
    bool Has(Guid id);
    bool TryGet(Guid id, [MaybeNullWhen(false)] out IAsset result);
}

public class AssetRegistry<T>(ILogger<AssetRegistry<T>>? logger = null) : IRegistry where T : IAsset
{
    private readonly ILogger<AssetRegistry<T>> _logger = logger ?? NullLogger<AssetRegistry<T>>.Instance;
    private readonly struct Entry(AssetDescriptor cid, T def)
    {
        public readonly AssetDescriptor Cid = cid;
        public readonly T Definition = def;
    }

    private readonly Dictionary<Guid, Entry> _storage = [];

    public void Add(AssetDescriptor cid, T content, bool overwrite = false)
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_storage, cid.Guid, out bool exists);
        
        if (exists)
        {
            if (!overwrite)
                throw new InvalidOperationException($"Asset {cid.QualifiedId} already exists in this registry");

            _logger.LogWarning(
                "Overwriting existing entry for {@QualifiedId}",
                cid.QualifiedId
            );
        }

        // write to storage via ref
        entry = new Entry(cid, content);
    }

    public T? Get(Guid id)
    {
        if (_storage.TryGetValue(id, out var entry))
        {
            return entry.Definition;
        }
        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? Get(AssetDescriptor cid) => Get(cid.Guid);

    public bool TryGet(Guid id, [MaybeNullWhen(false)] out T content)
    {
        if (_storage.TryGetValue(id, out var entry))
        {
            content = entry.Definition;
            return true;
        }

        content = default;
        return false;
    }

    public AssetDescriptor GetDescriptor(Guid id)
    {
        if (_storage.TryGetValue(id, out var entry))
        {
            return entry.Cid;
        }

        return AssetDescriptor.Empty;
    }

    public bool Has(Guid id) => _storage.ContainsKey(id);

    public IEnumerable<AssetDescriptor> GetAllIds() => _storage.Values.Select(e => e.Cid);

    IAsset? IRegistry.Get(Guid guid)
    {
        return Get(guid);
    }

    bool IRegistry.TryGet(Guid id, [MaybeNullWhen(false)] out IAsset result)
    {
        if (_storage.TryGetValue(id, out var entry))
        {
            result = entry.Definition;
            return true;
        }

        result = default;
        return false;
    }
}

public class AssetRegistry(IEnumerable<IRegistry> registries)
{
    private readonly Dictionary<Type, IRegistry> _registriesByType = registries.ToDictionary(r => r.GetType());

    [Cold]
    public IEnumerable<IAsset> FindAll(Guid id)
    {
        foreach (var registry in registries)
        {
            if (registry.TryGet(id, out var entry))
            {
                yield return entry;
            }
        }
    }

    public T? Get<T>(AssetDescriptor id) where T : class, IAsset
    {
        if (_registriesByType.TryGetValue(typeof(AssetRegistry<T>), out var registry))
        {
            if (registry.TryGet(id.Guid, out var asset))
            {
                return asset as T;
            }
        }
        return null;
    }
}
