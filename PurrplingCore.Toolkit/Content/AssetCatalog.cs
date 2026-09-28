using System.Text;
using System.Text.Json;

namespace PurrplingCore.Toolkit.Content;

/// <summary>
/// Semantic asset index stored by a content package independently of its PAK, ZIP, or directory source.
/// </summary>
public sealed record AssetCatalog
{
    public const string DirectoryName = "AssetCatalogs";
    public const string BinaryFileName = "asset-catalog.bin";
    public const string JsonFileName = "asset-catalog.json";
    public const string DefaultFileName = BinaryFileName;
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public required string PackageId { get; init; }
    public AssetCatalogEntry[] Assets { get; init; } = [];
}

/// <summary>
/// Describes one logical asset and the content-relative path used to load its compiled data.
/// </summary>
public sealed record AssetCatalogEntry
{
    public required string Id { get; init; }
    public required string ContentPath { get; init; }
    public string AssetClass { get; init; } = string.Empty;
    public Dictionary<string, string> Tags { get; init; } = [];
}

/// <summary>
/// Reads, writes, and validates the persistent binary and JSON asset catalog formats.
/// </summary>
public static class AssetCatalogSerializer
{
    private const uint BinaryMagic = 0x54414341; // ACAT
    private const int MaxAssetCount = 1_000_000;
    private const int MaxTagCount = 65_535;

    private static readonly JsonSerializerOptions s_options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static AssetCatalog Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            if (reader.ReadUInt32() != BinaryMagic)
            {
                throw new InvalidDataException($"Asset catalog is not a supported binary catalog.");
            }

            var catalog = new AssetCatalog
            {
                SchemaVersion = reader.ReadInt32(),
                PackageId = reader.ReadString(),
                Assets = ReadAssets(reader)
            };

            Validate(catalog);
            return catalog;
        }
        catch (EndOfStreamException exception)
        {
            throw new InvalidDataException($"Asset catalog is truncated.", exception);
        }
    }

    public static AssetCatalog LoadJson(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var catalog = JsonSerializer.Deserialize<AssetCatalog>(stream, s_options)
            ?? throw new InvalidDataException($"Asset catalog is empty.");

        Validate(catalog);
        return catalog;
    }

    public static void Save(Stream stream, AssetCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(catalog);

        Validate(catalog);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(BinaryMagic);
        writer.Write(catalog.SchemaVersion);
        writer.Write(catalog.PackageId);
        writer.Write(catalog.Assets.Length);

        foreach (var entry in catalog.Assets)
        {
            writer.Write(entry.Id);
            writer.Write(entry.ContentPath);
            writer.Write(entry.AssetClass);
            writer.Write(entry.Tags.Count);

            foreach (var tag in entry.Tags.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                writer.Write(tag.Key);
                writer.Write(tag.Value);
            }
        }
    }

    public static void SaveJson(Stream stream, AssetCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(catalog);

        Validate(catalog);
        JsonSerializer.Serialize(stream, catalog, s_options);
    }

    public static void Validate(AssetCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        if (catalog.SchemaVersion != AssetCatalog.CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"Unsupported asset catalog schema {catalog.SchemaVersion}. Expected {AssetCatalog.CurrentSchemaVersion}.");
        }

        if (string.IsNullOrWhiteSpace(catalog.PackageId))
        {
            throw new InvalidDataException("Asset catalog package ID is required.");
        }

        var identifiers = new HashSet<Guid>();
        foreach (var entry in catalog.Assets)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(entry.AssetClass);
            ArgumentNullException.ThrowIfNull(entry.Tags);

            AssetMetadata.ValidateFullName(entry.Id);
            var id = new AssetId(entry.Id);
            if (!identifiers.Add(id.Guid))
            {
                throw new InvalidDataException($"Asset catalog contains duplicate asset '{entry.Id}'.");
            }

            var contentPath = NormalizeContentPath(entry.ContentPath);
            foreach (var tag in entry.Tags)
            {
                ArgumentNullException.ThrowIfNull(tag.Key);
                ArgumentNullException.ThrowIfNull(tag.Value);
            }
        }
    }

    public static string NormalizeContentPath(string contentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentPath);

        var normalized = contentPath.Replace('\\', '/').Trim();
        if (normalized.StartsWith('/') || normalized.Split('/').Any(part => part is "." or ".." or ""))
        {
            throw new InvalidDataException($"Asset content path '{contentPath}' must be a relative non-traversing path.");
        }

        return normalized.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase)
            ? normalized[..^4]
            : normalized;
    }

    private static AssetCatalogEntry[] ReadAssets(BinaryReader reader)
    {
        var count = ReadCount(reader, MaxAssetCount, "asset");
        var assets = new AssetCatalogEntry[count];

        for (var index = 0; index < assets.Length; index++)
        {
            var id = reader.ReadString();
            var contentPath = reader.ReadString();
            var assetClass = reader.ReadString();
            var tagCount = ReadCount(reader, MaxTagCount, "tag");
            var tags = new Dictionary<string, string>(tagCount, StringComparer.Ordinal);
            for (var tagIndex = 0; tagIndex < tagCount; tagIndex++)
            {
                var key = reader.ReadString();
                if (!tags.TryAdd(key, reader.ReadString()))
                {
                    throw new InvalidDataException($"Asset catalog contains duplicate tag '{key}'.");
                }
            }

            assets[index] = new AssetCatalogEntry
            {
                Id = id,
                ContentPath = contentPath,
                AssetClass = assetClass,
                Tags = tags
            };
        }

        return assets;
    }

    private static int ReadCount(BinaryReader reader, int maximum, string itemName)
    {
        var count = reader.ReadInt32();
        if (count < 0 || count > maximum)
        {
            throw new InvalidDataException($"Asset catalog contains an invalid {itemName} count of {count}.");
        }

        return count;
    }
}

public static class AssetCatalogExtensions
{
    /// <summary>
    /// Validates a catalog and atomically registers all of its entries in an asset registry.
    /// </summary>
    /// <remarks>
    /// Asset IDs are globally unique. The registry rejects a catalog when any entry duplicates an
    /// asset that was already registered, rather than silently changing source precedence.
    /// </remarks>
    public static void Import(
        this AssetCatalog catalog,
        AssetRegistry registry,
        string sourceId)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);

        AssetCatalogSerializer.Validate(catalog);
        registry.RegisterCatalog(sourceId, catalog.Assets.Select(entry => AssetMetadata.Create(
            entry.Id,
            AssetCatalogSerializer.NormalizeContentPath(entry.ContentPath),
            entry.AssetClass,
            entry.Tags)));
    }
}