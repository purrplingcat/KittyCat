using DeterministicGuids;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit.Attributes;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Zio;

namespace PurrplingCore.Toolkit.Content;

[DebuggerDisplay("{FullName}")]
public readonly struct AssetId : IEquatable<AssetId>
{
    public static readonly AssetId Empty = default;

    public readonly Guid Guid;
    public readonly string FullName;

    public AssetId(string fullName)
    {
        ValidateName(fullName);

        FullName = fullName;
        Guid = CreateGuid(fullName);
    }

    [Cold]
    public AssetId(string ns, string? categoryPath, string name)
    {
        FullName = Qualify(ns, categoryPath, name);
        Guid = CreateGuid(FullName);

        ValidateName(FullName);
    }

    public ReadOnlySpan<char> Ns
    {
        get
        {
            if (FullName == null) return "unknown";
            int colon = FullName.IndexOf(':');
            return FullName.AsSpan(0, colon); // Dvojtečka tu 100% je díky validaci
        }
    }

    public ReadOnlySpan<char> Category
    {
        get
        {
            if (FullName == null) return default;

            int colon = FullName.IndexOf(':');
            int lastSlash = FullName.LastIndexOf('/');

            // Pokud tam není lomítko, nebo je před dvojtečkou, kategorie neexistuje
            if (lastSlash < colon)
                return default;

            // Vyřízneme přesně to, co je mezi dvojtečkou a posledním lomítkem
            return FullName.AsSpan(colon + 1, lastSlash - colon - 1);
        }
    }

    public ReadOnlySpan<char> Name
    {
        get
        {
            if (FullName == null) return Guid.ToString();

            int colon = FullName.IndexOf(':');
            int lastSlash = FullName.LastIndexOf('/');

            int nameStartIndex = Math.Max(colon, lastSlash) + 1;
            return FullName.AsSpan(nameStartIndex);
        }
    }

    public bool IsEmpty => this == Empty;

    [Cold]
    public static void ValidateName(string raw)
    {
        ArgumentException.ThrowIfNullOrEmpty(raw);

        if (raw.Length > ushort.MaxValue)
            Throw("Asset id is too long!");

        int firstColon = raw.IndexOf(':');

        if (firstColon == -1)
            Throw("Missing ':'");

        if (firstColon == 0)
            Throw("Cannot start with ':'");

        if (firstColon == raw.Length - 1)
            Throw("Cannot end with ':'");

        if (firstColon != raw.LastIndexOf(':'))
            Throw("Multiple ':' found");

        void Throw(string message)
        {
            throw new ArgumentException($"Invalid AssetId: '{raw}'. {message}", nameof(raw));
        }
    }

    public bool Equals(AssetId other) => Guid == other.Guid;

    public override string ToString()
    {
        return !string.IsNullOrEmpty(FullName)
            ? FullName 
            : $"[Unknown asset: {Guid}]";
    }

    public override int GetHashCode()
    {
        return Guid.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid CreateGuid(string qualifiedId)
    {
        return DeterministicGuid.Create(Namespaces.Assets, qualifiedId);
    }

    [Cold]
    public static string Qualify(string ns, string? categoryPath, string id)
    {
        if (string.IsNullOrEmpty(categoryPath)) return $"{ns}:{id}";

        var path = categoryPath.Trim('/');
        return $"{ns}:{path}/{id}";
    }

    public static implicit operator Guid(AssetId id) => id.Guid;

    [Cold]
    public override bool Equals(object? obj)
    {
        return obj is AssetId id && Equals(id);
    }
    public static bool operator ==(AssetId left, AssetId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AssetId left, AssetId right)
    {
        return !left.Equals(right);
    }
}

[DebuggerDisplay("{AssetFullName}#{AssetClass}")]
public sealed record AssetMetadata
{
    public required AssetId Id { get; init; }
    public string AssetClass { get; init; } = string.Empty;
    public string? PhysicalPath { get; init; }
    public string AssetName => Id.Name.ToString();
    public string AssetFullName => Id.FullName;
    public string PackageName => Id.Ns.ToString();
    public string ObjectPath => Id.Category.IsEmpty 
        ? Id.Name.ToString() 
        : $"{Id.Category}/{Id.Name}";

    public IReadOnlyDictionary<string, string> Tags { get; init; }
        = new Dictionary<string, string>();
}

public interface IAssetCache
{
    void Add(Guid uid, object value);
    object? Get(Guid uid);
    IEnumerable<Guid> GetKeys();
    bool Exists(Guid uid);
    bool Clear(Guid uid);
    void Clear();
}

public class AssetCache<T> : IAssetCache
{
    private readonly ConcurrentDictionary<Guid, T> _storage = [];

    public void Add(Guid uid, T asset)
    {
        _storage[uid] = asset;
    }

    public T? Get(Guid id)
    {
        if (_storage.TryGetValue(id, out var asset))
        {
            return asset;
        }
        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? Get(AssetId cid) => Get(cid.Guid);

    public bool TryGet(Guid id, [MaybeNullWhen(false)] out T asset)
    {
        return _storage.TryGetValue(id, out asset);
    }

    public bool Exists(Guid id) => _storage.ContainsKey(id);

    void IAssetCache.Add(Guid uid, object value)
    {
        throw new NotImplementedException();
    }

    object? IAssetCache.Get(Guid uid)
    {
        return Get(uid);
    }

    public IEnumerable<Guid> GetKeys()
    {
        return _storage.Keys;
    }

    public bool Clear(Guid uid)
    {
        return _storage.TryRemove(uid, out var _);
    }

    public void Clear()
    {
        _storage.Clear();
    }
}

public class AssetRegistry(ILogger<AssetRegistry> logger)
{
    private readonly Dictionary<Guid, AssetMetadata> _database = [];
    private readonly ILogger<AssetRegistry> _logger = logger;
    private readonly ReaderWriterLockSlim _lock = new();

    public void RegisterAsset(AssetMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        
        using (_lock.Write())
        {
            ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(_database, metadata.Id, out bool exists);

            if (exists)
            {
                _logger.LogWarning("Overwrite asset: {AssetId}", metadata.Id.FullName);
            }

            // insert via ref
            slot = metadata;
        }
    }

    public AssetId GetIdentifier(Guid id)
    {
        using var _ = _lock.Read();
        if (_database.TryGetValue(id, out var metadata) && metadata != null)
        {
            return metadata.Id;
        }

        return AssetId.Empty;
    }

    public AssetMetadata? GetMetadata(Guid id)
    {
        using var _ = _lock.Read();
        if (_database.TryGetValue(id, out var metadata))
        {
            return metadata;
        }

        return default;
    }

    [Cold]
    public IEnumerable<AssetMetadata> GetAssetsByName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

        foreach (var asset in GetAllMetadata())
        {
            if (asset.AssetName == name)
                yield return asset;
        }

    }

    public int GetAssetsByName(string name, Span<AssetMetadata> buffer, Func<AssetMetadata, bool> filter)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(filter);

        int count = 0;
        using var _ = _lock.Read();
        foreach (var asset in _database.Values)
        {
            if (count == buffer.Length) break;

            if (asset.AssetName == name)
            {
                if (filter != null && !filter(asset)) continue;

                buffer[count++] = asset;
            }
        }

        return count;
    }

    public int GetAssetsByName(string name, Span<AssetMetadata> buffer)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

        int count = 0;
        using var _ = _lock.Read();
        foreach (var asset in _database.Values)
        {
            if (count == buffer.Length) break;

            if (asset.AssetName == name)
            {
                buffer[count++] = asset;
            }
        }

        return count;
    }

    [Cold]
    public AssetId ResolveName(string nameOrPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameOrPath, nameof(nameOrPath));

        using var _ = _lock.Read();

        if (!nameOrPath.Contains(':'))
        {
            foreach (var asset in _database.Values)
            {
                if (asset.AssetName == nameOrPath)
                {
                    return asset.Id;
                }
            }

            return AssetId.Empty;
        }

        var guid = AssetId.CreateGuid(nameOrPath);
        if (_database.TryGetValue(guid, out var hotAsset))
        {
            return hotAsset.Id;
        }

        return AssetId.Empty;
    }

    [Cold]
    public IEnumerable<AssetMetadata> GetAllMetadata()
    {
        using var _ = _lock.Read();
        return [.. _database.Values];
    }
}

public class AssetBank<T> where T : class
{
    private T[] _values;
    private readonly Dictionary<Guid, ushort> _lookup = [];
    private readonly AssetManager _assets;
    private ushort _count = 0;

    public AssetBank(AssetManager assets, ushort capacity = 8)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        _values = new T[capacity];
    }

    public AssetBank(AssetManager assets) : this(assets, 8) { }

    public T this[ushort index] => Get(index);

    private ushort FindFreeIndex()
    {
        if (_count >= _values.Length)
        {
            if (_count == ushort.MaxValue)
                throw new InvalidOperationException("AssetBank exceeded their maximum capacity!");
            Array.Resize(ref _values, _values.Length * 2);
        }

        return _count++;
    }

    public ushort GetIndex(AssetId assetId)
    {
        bool found = _lookup.TryGetValue(assetId, out var index);
        
        if (found && _assets.IsLoaded<T>(assetId))
            return index;

        T loadedAsset = _assets.Get<T>(assetId);
        if (!found)
            index = FindFreeIndex();

        _values[index] = loadedAsset;
        _lookup[assetId.Guid] = index;

        return index;
    }

    public T Get(ushort index)
    {
        if (index >= _count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _values[index];
    }
}

public class AssetManager
{
    private readonly ContentManager _content;
    private readonly AssetRegistry _masterRegistry;
    private readonly IServiceProvider _services;
    private readonly ConcurrentDictionary<Type, IAssetCache> _cacheMap = [];

    public delegate void AssetsUnloadedHandler(ReadOnlySpan<AssetId> unloadedAssets);

    public event AssetsUnloadedHandler? AssetsUnloaded;
    public event Action? CacheCleared;

    public AssetManager(IContentManagerProvider contentProvider, AssetRegistry masterRegistry)
    {
        _content = contentProvider.CreateContentManager();
        _masterRegistry = masterRegistry;
        _services = _content.ServiceProvider;
    }

    protected virtual AssetCache<T> GetAssetCache<T>()
    {
        return (AssetCache<T>)_cacheMap.GetOrAdd(
            key: typeof(T),
            valueFactory: static (_, services) => 
                ActivatorUtilities.GetServiceOrCreateInstance<AssetCache<T>>(services),
            factoryArgument: _services
        );
    }

    public T Get<T>(AssetId assetID)
    {
        var cache = GetAssetCache<T>();

        if (!cache.TryGet(assetID, out var loadedAsset))
        {
            var descriptor = _masterRegistry.GetMetadata(assetID) 
                ?? throw new KeyNotFoundException($"Asset {assetID} is not registered!");
            loadedAsset = LoadAsset<T>(in descriptor);
            cache.Add(assetID, loadedAsset);
        }

        return loadedAsset;
    }

    public bool IsLoaded<T>(AssetId assetId)
    {
        return GetAssetCache<T>().Exists(assetId);
    }

    public bool IsLoaded(AssetId assetId)
    {
        foreach (var cache in _cacheMap.Values)
        {
            if (cache.Exists(assetId)) 
                return true;
        }

        return false;
    }

    protected virtual T LoadAsset<T>(in AssetMetadata descriptor)
    {
        var loader = _services.GetService<IContentLoader<T>>() 
            ?? throw new NotSupportedException($"Asset type {typeof(T)} is not supported.");

        return loader.Load(_content, descriptor.GetAssetPath());
    }

    public void UnloadAsset(AssetId assetId)
    {
        var metadata = _masterRegistry.GetMetadata(assetId);
        if (metadata != null)
        {
            foreach (var cache in _cacheMap.Values)
            {
                cache.Clear(assetId);
            }

            _content.UnloadAsset(metadata.GetAssetPath());
            AssetsUnloaded?.Invoke([assetId]);
        }
    }

    public void Unload()
    {
        _cacheMap.Clear();
        _content.Unload();
    }
}

public static class AssetHelper
{
    public static string GetAssetPath(this AssetMetadata descriptor)
    {
        if (string.IsNullOrEmpty(descriptor.PhysicalPath))
        {
            var path = UPath.Combine(descriptor.PackageName, descriptor.ObjectPath);
            return path.FullName;
        }

        return descriptor.PhysicalPath;
    }

    public static AssetBank<T> CreateBank<T>(this AssetManager assetmanager) where T : class
    {
        return new AssetBank<T>(assetmanager);
    }
}

public static class LockExtensions
{
    public readonly ref struct ReadLockToken
    {
        private readonly ReaderWriterLockSlim _lock;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadLockToken(ReaderWriterLockSlim rwLock)
        {
            _lock = rwLock;
            _lock.EnterReadLock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _lock.ExitReadLock();
    }

    public readonly ref struct WriteLockToken
    {
        private readonly ReaderWriterLockSlim _lock;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WriteLockToken(ReaderWriterLockSlim rwLock)
        {
            _lock = rwLock;
            _lock.EnterWriteLock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _lock.ExitWriteLock();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadLockToken Read(this ReaderWriterLockSlim rwLock) => new(rwLock);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WriteLockToken Write(this ReaderWriterLockSlim rwLock) => new(rwLock);
}

public static class PurrplingAssetsServiceExtensions
{
    // Tuto metodu zavolá vývojář hry/enginu při startu
    public static IServiceCollection AddPurrplingAssetSystem(this IServiceCollection services)
    {
        // Globální systémy (žijí celou hru)
        services.TryAddSingleton<AssetRegistry>();
        services.TryAddSingleton<AssetManager>();

        // Registrace otevřeného generika pro Scoped banky.
        // Kdykoliv si jakýkoliv systém v novém Scope řekne o AssetBank<T>, 
        // DI mu automaticky vytvoří novou instanci a předá jí Singleton AssetManager.
        services.TryAddScoped(typeof(AssetBank<>));

        return services;
    }
}
