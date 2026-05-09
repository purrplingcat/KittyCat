using DeterministicGuids;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PurrplingCore.Toolkit.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Toolkit.Content;

public readonly struct AssetId : IEquatable<AssetId>
{
    public readonly string Type;
    public readonly string Mod;
    public readonly string Id; 
    public readonly string QualifiedId;  // type:mod/id
    public readonly Guid Guid;

    public static readonly AssetId Empty = default;

    [ColdPath]
    public AssetId(string type, string mod, string id)
    {
        Type = type;
        Mod = mod;
        Id = id;
        QualifiedId = Qualify(type, mod, id);
        Guid = GetGuid(QualifiedId);
    }

    private AssetId(string rawInput)
    {
        int colonIndex = rawInput.IndexOf(':');
        int slashIndex = rawInput.IndexOf('/');

        if (colonIndex == -1 || slashIndex == -1)
            throw new ArgumentException($"Invalid ContentId: {rawInput}");

        QualifiedId = rawInput;
        Type = rawInput[..colonIndex];
        Mod = rawInput[(colonIndex + 1)..slashIndex];
        Id = rawInput[(slashIndex + 1)..];
        Guid = GetGuid(QualifiedId);
    }

    public bool Equals(AssetId other)
    {
        return Guid.Equals(other.Guid);
    }

    public override string ToString() => QualifiedId;

    public override bool Equals(object? obj)
    {
        return obj is AssetId id && Guid.Equals(id.Guid);
    }

    public override int GetHashCode()
    {
        return Guid.GetHashCode();
    }

    public static string Qualify(string type, string mod, string id) 
    { 
        ArgumentException.ThrowIfNullOrEmpty(type, nameof(type));
        ArgumentException.ThrowIfNullOrEmpty(mod, nameof(mod));
        ArgumentException.ThrowIfNullOrEmpty(id, nameof(id));

        return $"{type}:{mod}/{id}"; 
    }

    public static Guid GetGuid(string qualifiedId)
    {
        return DeterministicGuid.Create(DeterministicGuid.Namespaces.Oid, qualifiedId);
    }

    public static AssetId Parse(string raw) => new(raw);

    public static bool operator ==(AssetId left, AssetId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AssetId left, AssetId right)
    {
        return !(left == right);
    }
}

public interface IAsset
{
    string Id { get; }
}

public interface IRegistry
{
    IEnumerable<AssetId> GetAllIds(); // Pro výpis tabulky
}

public class AssetRegistry<T>(string typePrefix, ILogger<AssetRegistry<T>>? logger = null) : IRegistry where T : IAsset
{
    private readonly ILogger<AssetRegistry<T>> _logger = logger ?? NullLogger<AssetRegistry<T>>.Instance;
    private readonly struct Entry(AssetId cid, T def)
    {
        public readonly AssetId Cid = cid;
        public readonly T Definition = def;
    }

    private readonly Dictionary<Guid, Entry> _storage = [];

    public Guid Add(string modId, T content)
    {
        var cid = AssetId.Parse($"{typePrefix}:{modId}/{content.Id}");
        var entry = new Entry(cid, content);

        if (_storage.ContainsKey(cid.Guid))
        {
            _logger.LogWarning(
                "Overwriting existing entry for {@QualifiedId}", 
                cid.QualifiedId
            );
        }

        _storage[cid.Guid] = entry;
        return cid.Guid;
    }

    public T? Get(Guid id)
    {
        if (_storage.TryGetValue(id, out var entry))
        {
            return entry.Definition;
        }
        return default;
    }

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

    public bool Has(Guid id) => _storage.ContainsKey(id);

    public IEnumerable<AssetId> GetAllIds() => _storage.Values.Select(e => e.Cid);
}

public class AssetDatabase(ILoggerFactory loggerFactory)
{
    private readonly Dictionary<string, IRegistry> _registries = [];

    public void AddType<T>(string typeName) where T : IAsset
    {
        var logger = loggerFactory.CreateLogger<AssetRegistry<T>>();
        _registries[typeName] = new AssetRegistry<T>(typeName, logger);
    }

    public AssetRegistry<T> GetRegistry<T>(string typeName) where T : IAsset
    {
        return (AssetRegistry<T>)_registries[typeName];
    }

    [HotPath]
    public T? Get<T>(AssetId cid) where T : IAsset
    {
        var registry = GetRegistry<T>(cid.Type);
        return registry.Get(cid.Guid);
    }
}

public static class ContentDatabaseExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? Get<T>(this AssetDatabase db, string type, string mod, string id) where T : IAsset
    {
        var cid = new AssetId(type, mod, id);
        return db.Get<T>(cid);
    }
}

public interface IVfsProvider : IDisposable
{
    string SourceName { get; }
    IEnumerable<string> GetFiles(string extension);
    Stream OpenRead(string path);
}

public class ZipVfsProvider(string zipPath) : IVfsProvider
{
    private readonly ZipArchive _archive = ZipFile.OpenRead(zipPath);
    public string SourceName => Path.GetFileNameWithoutExtension(zipPath);

    public IEnumerable<string> GetFiles(string extension)
    {
        return _archive.Entries
            .Where(e => e.FullName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.FullName);
    }

    public Stream OpenRead(string path)
    {
        var entry = _archive.GetEntry(path)
            ?? throw new FileNotFoundException($"File {path} not found in {SourceName}");
        return entry.Open();
    }

    public void Dispose() => _archive.Dispose();
}

public class FolderVfsProvider(string rootPath) : IVfsProvider
{
    public string SourceName => Path.GetFileName(rootPath);

    public IEnumerable<string> GetFiles(string extension)
    {
        if (!Directory.Exists(rootPath)) return [];
        return Directory.GetFiles(rootPath, $"*{extension}", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(rootPath, p).Replace('\\', '/'));
    }

    public Stream OpenRead(string path)
    {
        string fullPath = Path.Combine(rootPath, path);
        return File.OpenRead(fullPath);
    }

    public void Dispose() { }
}
