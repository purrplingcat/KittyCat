using PurrplingCore.Toolkit.Content;
using System.Text.Json;

namespace PurrplingCore.ContentBuilder;

/// <summary>
/// Builds semantic asset catalogs from source metadata after MonoGame has compiled the content.
/// </summary>
public static class AssetCatalogBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record SourceAssetMetadata
    {
        public required string AssetFile { get; init; }
        public string? Id { get; init; }
        public string AssetClass { get; init; } = string.Empty;
        public Dictionary<string, string> Tags { get; init; } = [];
    }

    /// <summary>
    /// Creates a binary asset catalog from source metadata whose compiled XNB files exist.
    /// </summary>
    /// <param name="sourceDirectory">Root containing source <c>*.meta.json</c> files.</param>
    /// <param name="compiledDirectory">Root containing compiled XNB files.</param>
    /// <param name="packageName">Bundle-level package name used to form asset IDs.</param>
    /// <param name="packageId">Catalog source identifier stored in the catalog.</param>
    /// <param name="outputPath">Optional catalog destination; defaults to the compiled root.</param>
    /// <param name="contentRoots">Output roots used by the bundle's content collection.</param>
    /// <returns>The number of asset entries written, or zero when no compiled/copied asset matches.</returns>
    public static int Generate(
        string sourceDirectory,
        string compiledDirectory,
        string packageName,
        string? packageId = null,
        string? outputPath = null,
        IReadOnlyCollection<string>? contentRoots = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceDirectory);
        ArgumentException.ThrowIfNullOrEmpty(compiledDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        var catalogPath = outputPath ?? Path.Combine(compiledDirectory, AssetCatalog.DefaultFileName);
        var assets = new List<AssetCatalogEntry>();

        foreach (var metadataPath in Directory.EnumerateFiles(sourceDirectory, "*.meta.json", SearchOption.AllDirectories))
        {
            using var metadataStream = File.OpenRead(metadataPath);
            var sourceMetadata = JsonSerializer.Deserialize<SourceAssetMetadata>(metadataStream, s_jsonOptions)
                ?? throw new InvalidDataException($"Asset metadata '{metadataPath}' is empty.");

            if (Path.IsPathRooted(sourceMetadata.AssetFile))
            {
                throw new InvalidDataException($"AssetFile '{sourceMetadata.AssetFile}' must be relative to '{metadataPath}'.");
            }

            var sourceFilePath = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(metadataPath)!,
                sourceMetadata.AssetFile));
            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException(
                    $"AssetFile '{sourceMetadata.AssetFile}' referenced by '{metadataPath}' was not found.",
                    sourceFilePath);
            }

            var sourceRelativePath = Path.GetRelativePath(sourceDirectory, sourceFilePath);
            if (Path.IsPathRooted(sourceRelativePath) || sourceRelativePath == ".." ||
                sourceRelativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                throw new InvalidDataException($"AssetFile '{sourceMetadata.AssetFile}' escapes the bundle source directory.");
            }

            var sourceExtension = Path.GetExtension(sourceRelativePath);
            if (sourceExtension.Length == 0)
            {
                throw new InvalidDataException($"AssetFile '{sourceMetadata.AssetFile}' must include a file extension.");
            }

            var objectPath = AssetCatalogSerializer.NormalizeObjectPath(
                Path.ChangeExtension(sourceRelativePath, null)!.Replace('\\', '/'));
            var matchingRoot = FindCompiledRoot(compiledDirectory, objectPath, sourceExtension, contentRoots, out var outputExtension);
            if (matchingRoot is null)
            {
                continue;
            }

            var assetId = sourceMetadata.Id ?? $"{packageName.ToLowerInvariant()}:{objectPath}";
            AssetMetadata.ValidateFullName(assetId);

            assets.Add(new AssetCatalogEntry
            {
                Id = assetId,
                PackageName = packageName,
                ObjectPath = objectPath,
                FileExtension = outputExtension,
                AssetClass = sourceMetadata.AssetClass,
                Tags = sourceMetadata.Tags
            });
        }

        var catalog = new AssetCatalog
        {
            PackageId = packageId ?? Path.GetFileName(compiledDirectory),
            Assets = [.. assets]
        };

        Directory.CreateDirectory(Path.GetDirectoryName(catalogPath)!);
        using var catalogStream = File.Create(catalogPath);
        AssetCatalogSerializer.Save(catalogStream, catalog);

        return assets.Count;
    }

    private static string? FindCompiledRoot(
        string compiledDirectory,
        string objectPath,
        string sourceExtension,
        IReadOnlyCollection<string>? contentRoots,
        out string? outputExtension)
    {
        var found = false;
        var matchingRoot = string.Empty;
        outputExtension = null;
        foreach (var root in contentRoots ?? [string.Empty])
        {
            var rootDirectory = string.IsNullOrEmpty(root)
                ? compiledDirectory
                : Path.Combine(compiledDirectory, root.Replace('/', Path.DirectorySeparatorChar));
            var xnbExists = File.Exists(Path.Combine(rootDirectory, objectPath + ".xnb"));
            var copiedFileExists = File.Exists(Path.Combine(rootDirectory, objectPath + sourceExtension));
            if (!xnbExists && !copiedFileExists)
            {
                continue;
            }

            if (found)
            {
                throw new InvalidDataException(
                    $"Asset object path '{objectPath}' exists under multiple content roots ('{matchingRoot}' and '{root}').");
            }

            found = true;
            matchingRoot = root;
            outputExtension = xnbExists ? null : sourceExtension;
        }

        return found ? matchingRoot : null;
    }

}