using DeterministicGuids;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Attributes;
using PurrplingCore.Toolkit.DI;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PurrplingCore.Toolkit.Content;

[DebuggerDisplay("{FullName}")]
public readonly struct AssetId : IEquatable<AssetId>
{
    public static readonly AssetId Empty = default;

    public readonly Guid Guid;
    public readonly string FullName;

    public AssetId(string fullName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fullName);
        FullName = fullName;
        Guid = CreateGuid(fullName);
    }

    public bool IsEmpty => this == Empty;

    public bool Equals(AssetId other) => Guid == other.Guid;

    public override string ToString() => FullName ?? "[Empty asset]";

    public override int GetHashCode()
    {
        return Guid.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid CreateGuid(string qualifiedId)
    {
        return DeterministicGuid.Create(Namespaces.Assets, qualifiedId);
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

[DebuggerDisplay("{FullName}#{AssetClass}")]
public sealed record AssetMetadata
{
    public AssetId Id { get; }
    public string Name => Id.FullName;
    public string PackageName { get; }
    public string ObjectPath { get; }
    public string AssetName { get; }
    public string AssetClass { get; }
    public string? ResourceType { get; }
    public IReadOnlyDictionary<string, string> Tags { get; }

    public AssetMetadata(
        AssetId id,
        string packageName,
        string objectPath,
        string assetClass = "",
        IReadOnlyDictionary<string, string>? tags = null,
        string? fileExtension = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);
        ObjectPath = NormalizeObjectPath(objectPath);
        ValidateFullName(id.FullName);
        Id = id;
        PackageName = packageName;
        AssetName = Path.GetFileName(ObjectPath);
        AssetClass = assetClass;
        ResourceType = NormalizeResourceType(fileExtension);
        Tags = tags ?? new Dictionary<string, string>();
    }

    public static AssetMetadata Create(
        string packageName,
        string objectPath,
        string assetClass = "",
        IReadOnlyDictionary<string, string>? tags = null,
        string? fileExtension = null,
        string? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);
        var normalizedPath = NormalizeObjectPath(objectPath);
        var assetId = new AssetId(id ?? $"{packageName.ToLowerInvariant()}:{normalizedPath}");
        return new AssetMetadata(assetId, packageName, normalizedPath, assetClass, tags, fileExtension);
    }

    private static string NormalizeObjectPath(string objectPath)
    {
        var normalizedPath = NormalizeRelativePath(objectPath, nameof(objectPath));
        if (normalizedPath.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath[..^4];
        }

        return normalizedPath;
    }

    private static string? NormalizeResourceType(string? fileExtension)
    {
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            return null;
        }

        var normalized = fileExtension.Trim();
        if (!normalized.StartsWith('.') || normalized.Length == 1 || normalized.Contains('/') || normalized.Contains('\\'))
        {
            throw new ArgumentException($"Invalid asset file extension '{fileExtension}'.", nameof(fileExtension));
        }

        return normalized;
    }

    private static string NormalizeRelativePath(string path, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, parameterName);
        var normalizedPath = path.Replace('\\', '/').Trim();
        if (normalizedPath.StartsWith('/') || normalizedPath.Split('/').Any(segment => segment is "" or "." or ".."))
        {
            throw new ArgumentException($"Invalid asset path '{path}'.", parameterName);
        }

        return normalizedPath;
    }

    public static void ValidateFullName(string fullName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fullName);

        if (fullName.Length > ushort.MaxValue)
            Throw("Asset name is too long.");

        int separator = fullName.IndexOf(':');
        if (separator <= 0 || separator == fullName.Length - 1 || separator != fullName.LastIndexOf(':'))
            Throw("Asset name must contain one ':' separating a non-empty namespace and path.");

        if (fullName.Split('/').Any(segment => segment is "" or "." or ".."))
            Throw("Asset name contains an invalid path segment.");

        void Throw(string message) => throw new ArgumentException($"Invalid asset name '{fullName}'. {message}", nameof(fullName));
    }
}

public interface IAssetCache
{
    bool TryRetain(Guid id, [NotNullWhen(true)] out object? value);
    bool Release(Guid id);
}

public class AssetCache<T> : IAssetCache
{
    private readonly Dictionary<Guid, CacheEntry> _storage = [];
    private readonly object _syncRoot = new();

    struct CacheEntry
    {
        public T Asset;
        public int RefCount;
    }

    public bool TryRetain(Guid id, [NotNullWhen(true)] out T? asset)
    {
        lock (_syncRoot)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(_storage, id);

            if (!Unsafe.IsNullRef(ref entry) && entry.Asset != null)
            {
                entry.RefCount++;
                asset = entry.Asset;
                return true;
            }

            asset = default;
            return false;
        }
    }

    public void Add(Guid id, T asset)
    {
        ArgumentNullException.ThrowIfNull(asset, nameof(asset));

        lock (_syncRoot)
        {
            _storage.Add(id, new CacheEntry
            {
                Asset = asset,
                RefCount = 1
            });
        }
    }

    public bool Release(Guid id)
    {
        lock (_syncRoot)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(_storage, id);

            if (!Unsafe.IsNullRef(ref entry))
            {
                if (--entry.RefCount <= 0)
                {
                    _storage.Remove(id);
                    return true;
                }
            }

            return false;
        }
    }

    bool IAssetCache.TryRetain(Guid id, [NotNullWhen(true)] out object? value)
    {
        bool found = TryRetain(id, out var asset);
        value = asset;
        return found;
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
        ValidateMetadata(metadata);
        
        using (_lock.Write())
        {
            ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(_database, metadata.Id, out bool exists);

            if (exists)
            {
                throw new InvalidOperationException($"Asset '{metadata.Name}' is already registered.");
            }

            slot = metadata;
        }
    }

    /// <summary>
    /// Atomically imports a package catalog. Asset IDs are globally unique; source layering selects the
    /// file bytes, while duplicate semantic asset registrations are always rejected.
    /// </summary>
    public void RegisterCatalog(string sourceId, IEnumerable<AssetMetadata> assets)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentNullException.ThrowIfNull(assets);

        var incomingAssets = assets.ToArray();
        foreach (var asset in incomingAssets)
        {
            ArgumentNullException.ThrowIfNull(asset);
            ValidateMetadata(asset);
        }

        using (_lock.Write())
        {
            foreach (var asset in incomingAssets)
            {
                if (_database.ContainsKey(asset.Id.Guid))
                {
                    throw new InvalidDataException(
                        $"Catalog '{sourceId}' duplicates already registered asset '{asset.Name}'.");
                }
            }

            foreach (var asset in incomingAssets)
            {
                _database.Add(asset.Id.Guid, asset);
            }
        }

        _logger.LogInformation("Imported {Count} assets from catalog {SourceId}.", incomingAssets.Length, sourceId);
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
            if (asset.AssetName == name && filter(asset))
                buffer[count++] = asset;
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
                buffer[count++] = asset;
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

        AssetMetadata.ValidateFullName(nameOrPath);
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

    private static void ValidateMetadata(AssetMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
    }
}

public class AssetBank<T> : IDisposable
{
    private AssetHandle<T>[] _handles;
    private readonly Dictionary<Guid, ushort> _lookup = [];
    private readonly AssetManager _assets;
    private ushort _count = 0;

    public AssetBank(AssetManager assets, ushort capacity = 8)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        _handles = new AssetHandle<T>[capacity];
    }

    public AssetBank(AssetManager assets) : this(assets, 8) { }

    public T this[ushort index] => Get(index);

    private ushort FindFreeIndex()
    {
        if (_count >= _handles.Length)
        {
            if (_count == ushort.MaxValue)
                throw new InvalidOperationException("AssetBank exceeded their maximum capacity!");
            Array.Resize(ref _handles, _handles.Length * 2);
        }

        return _count++;
    }

    public ushort GetIndex(AssetId assetId)
    {
        ObjectDisposedException.ThrowIf(_handles == null, this);

        if (_lookup.TryGetValue(assetId.Guid, out var index))
            return index;

        // Vydáme Handle. Cache zvedne ref-counter.
        var handle = _assets.Get<T>(assetId);

        index = FindFreeIndex();
        _handles[index] = handle;
        _lookup[assetId.Guid] = index;

        return index;
    }

    [Hot]
    public T Get(ushort index)
    {
        if (index >= _count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        ref var handle = ref _handles[index];
        return handle.Asset;
    }

    public void Dispose()
    {
        if (_handles == null) return;

        foreach (var handle in _handles)
        {
            handle.Dispose();
        }

        _count = 0;
        _handles = null!; // Object disposed
        _lookup.Clear();
        GC.SuppressFinalize(this);
    }
}

public readonly struct AssetHandle<T> : IDisposable
{
    public readonly T Asset;
    private readonly AssetManager _manager; // Konkrétní třída, žádný interface
    private readonly Guid _assetId;

    internal AssetHandle(AssetManager manager, T asset, Guid assetId)
    {
        Asset = asset;
        _manager = manager;
        _assetId = assetId;
    }

    public void Dispose()
    {
        _manager?.Release<T>(_assetId);
    }
}

public class AssetManager : IDisposable
{
    private readonly PackageContentManagers _contentManagers;
    private readonly PackageManager _packages;
    private readonly AssetRegistry _masterRegistry;
    private readonly IServiceProvider _services;
    private readonly ConcurrentDictionary<Type, IAssetCache> _cacheMap = [];

    public delegate void AssetsUnloadedHandler(ReadOnlySpan<AssetId> unloadedAssets);

    public event AssetsUnloadedHandler? AssetsUnloaded;
    public event Action? CacheCleared;

    public AssetManager(
        IContentManagerProvider contentProvider,
        AssetRegistry masterRegistry,
        PackageManager pathResolver)
    {
        _contentManagers = new PackageContentManagers(contentProvider, pathResolver);
        _packages = pathResolver;
        _masterRegistry = masterRegistry;
        _services = contentProvider.ContentManager.ServiceProvider;
    }

    protected virtual AssetCache<T> GetAssetCache<T>()
    {
        if (_cacheMap.TryGetValue(typeof(T), out var cache))
        {
            return (AssetCache<T>)cache;
        }

        var newCache = ActivatorUtilities.GetServiceOrCreateInstance<AssetCache<T>>(_services);
        return (AssetCache<T>)_cacheMap.GetOrAdd(typeof(T), newCache);
    }

    /// <summary>
    /// Retains or loads a registered asset without discovering packages or indexing catalogs.
    /// </summary>
    public AssetHandle<T> Get<T>(AssetId assetId)
    {
        var cache = GetAssetCache<T>();

        if (!cache.TryRetain(assetId, out var loadedAsset))
        {
            var descriptor = _masterRegistry.GetMetadata(assetId)
                ?? throw new KeyNotFoundException($"Asset {assetId.Guid} is not registered!");

            loadedAsset = LoadAsset<T>(in descriptor);
            cache.Add(assetId, loadedAsset);
        }

        return new AssetHandle<T>(this, loadedAsset, assetId);
    }

    protected virtual T LoadAsset<T>(in AssetMetadata descriptor)
    {
        var loader = _services.GetService<IContentLoader<T>>()
            ?? throw new NotSupportedException($"Asset type {typeof(T)} is not supported.");

        var manager = _contentManagers.Get(descriptor.PackageName);
        return loader.Load(manager, descriptor.GetAssetPath());
    }

    internal void Release<T>(Guid assetId)
    {
        var cache = GetAssetCache<T>();

        if (cache.Release(assetId))
        {
            var descriptor = _masterRegistry.GetMetadata(assetId);
            if (descriptor != null)
            {
                var manager = _contentManagers.Get(descriptor.PackageName);
                manager.UnloadAsset(descriptor.GetAssetPath());
                AssetsUnloaded?.Invoke([descriptor.Id]);
            }
        }
    }

    public void Dispose() => _contentManagers.Dispose();
}

public static class AssetHelper
{
    public static string GetAssetPath(this AssetMetadata descriptor, string? root = null)
    {
        var path = Path.ChangeExtension(descriptor.ObjectPath, descriptor.ResourceType);
        return root is null ? path : Path.Combine(root, path);
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
    /// <summary>
    /// Registers asset storage, loading, catalog indexing, and provider-backed catalog startup.
    /// </summary>
    /// <remarks>
    /// The optional <see cref="Microsoft.Extensions.FileProviders.IFileProvider"/> is resolved by
    /// <see cref="AssetCatalogIndexer"/>. Register <see cref="AddTitleAssetCatalog"/> instead when
    /// the application intentionally has no provider-backed content namespace.
    /// </remarks>
    public static IServiceCollection AddAssetRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<AssetRegistry>();
        services.TryAddSingleton<PackageManager>();
        services.TryAddSingleton<AssetManager>();
        services.TryAddScoped(typeof(AssetBank<>));
        services.TryAddSingleton<AssetCatalogIndexer>();
        services.AddStartup<FileProviderAssetCatalogStartup>();

        return services;
    }

    /// <summary>
    /// Registers title-container catalog indexing for applications that do not provide another content source.
    /// </summary>
    public static IServiceCollection AddTitleAssetCatalog(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAssetRegistry();
        services.AddStartup<TitleAssetCatalogStartup>();

        return services;
    }
}
