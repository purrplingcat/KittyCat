using System.Text;

namespace PurrplingCore.Toolkit.Content;

/// <summary>
/// Semantic asset index stored by a content package independently of its PAK, ZIP, or directory source.
/// </summary>
public sealed record AssetCatalog
{
    public const string DirectoryName = "AssetCatalogs";
    public const string BinaryFileName = "asset-catalog.bin";
    public const string DefaultFileName = BinaryFileName;
    public const int CurrentSchemaVersion = 8;

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
    public required string PackageName { get; init; }
    public required string ObjectPath { get; init; }
    public string? FileExtension { get; init; }
    public string AssetClass { get; init; } = string.Empty;
    public Dictionary<string, string> Tags { get; init; } = [];
}

/// <summary>
/// Reads, writes, and validates the persistent binary asset catalog format.
/// </summary>
public static class AssetCatalogSerializer
{
    private const uint BinaryMagic = 0x54414341; // ACAT
    private const int MaxAssetCount = 1_000_000;
    private const int MaxTagCount = 65_535;

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

            var schemaVersion = reader.ReadInt32();
            if (schemaVersion != AssetCatalog.CurrentSchemaVersion)
            {
                throw new InvalidDataException(
                    $"Unsupported asset catalog schema {schemaVersion}. Expected {AssetCatalog.CurrentSchemaVersion}.");
            }

            var catalog = new AssetCatalog
            {
                SchemaVersion = schemaVersion,
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
            writer.Write(entry.PackageName);
            writer.Write(entry.ObjectPath);
            writer.Write(entry.FileExtension ?? string.Empty);
            writer.Write(entry.AssetClass);
            writer.Write(entry.Tags.Count);

            foreach (var tag in entry.Tags.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                writer.Write(tag.Key);
                writer.Write(tag.Value);
            }
        }
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
            ArgumentException.ThrowIfNullOrWhiteSpace(entry.Id);
            ArgumentException.ThrowIfNullOrWhiteSpace(entry.PackageName);
            ArgumentNullException.ThrowIfNull(entry.AssetClass);
            ArgumentNullException.ThrowIfNull(entry.Tags);

            var metadata = AssetMetadata.Create(
                entry.PackageName,
                NormalizeObjectPath(entry.ObjectPath),
                entry.AssetClass,
                entry.Tags,
                entry.FileExtension,
                entry.Id);
            if (!identifiers.Add(metadata.Id.Guid))
            {
                throw new InvalidDataException($"Asset catalog contains duplicate asset '{metadata.Name}'.");
            }

            foreach (var tag in entry.Tags)
            {
                ArgumentNullException.ThrowIfNull(tag.Key);
                ArgumentNullException.ThrowIfNull(tag.Value);
            }
        }
    }

    public static string NormalizeObjectPath(string objectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectPath);

        var normalized = objectPath.Replace('\\', '/').Trim();
        if (normalized.StartsWith('/') || normalized.Split('/').Any(part => part is "." or ".." or ""))
        {
            throw new InvalidDataException($"Asset object path '{objectPath}' must be a relative non-traversing path.");
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
            var packageName = reader.ReadString();
            var objectPath = reader.ReadString();
            var fileExtension = reader.ReadString();
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
                PackageName = packageName,
                ObjectPath = objectPath,
                FileExtension = fileExtension.Length == 0 ? null : fileExtension,
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
    /// asset that was already registered.
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
            entry.PackageName,
            AssetCatalogSerializer.NormalizeObjectPath(entry.ObjectPath),
            entry.AssetClass,
            entry.Tags,
            entry.FileExtension,
            entry.Id)));
    }
}