using PurrplingCore.Toolkit.Content;
using System.Text.Json;

namespace PurrplingCore.ContentBuilder;

/// <summary>
/// Builds semantic asset catalogs from source metadata after MonoGame has compiled the content.
/// </summary>
public static class AssetCatalogBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Creates a binary asset catalog from source metadata whose compiled XNB files exist.
    /// </summary>
    /// <param name="sourceDirectory">Root containing source <c>*.meta.json</c> files.</param>
    /// <param name="compiledDirectory">Root containing compiled XNB files.</param>
    /// <param name="packageId">Logical package name stored in the catalog.</param>
    /// <param name="outputPath">Optional catalog destination; defaults to the compiled root.</param>
    /// <param name="contentPathPrefixes">
    /// Optional content-relative paths ("." = all) used to select entries for one package.
    /// </param>
    /// <returns>The number of asset entries written, or zero when no matching XNB exists.</returns>
    public static int Generate(
        string sourceDirectory,
        string compiledDirectory,
        string? packageId = null,
        string? outputPath = null,
        IReadOnlyCollection<string>? contentPathPrefixes = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceDirectory);
        ArgumentException.ThrowIfNullOrEmpty(compiledDirectory);

        var assets = new List<AssetCatalogEntry>();
        foreach (var metadataPath in Directory.EnumerateFiles(sourceDirectory, "*.meta.json", SearchOption.AllDirectories))
        {
            using var metadataStream = File.OpenRead(metadataPath);
            var entry = JsonSerializer.Deserialize<AssetCatalogEntry>(metadataStream, s_jsonOptions)
                ?? throw new InvalidDataException($"Asset metadata '{metadataPath}' is empty.");

            var contentPath = AssetCatalogSerializer.NormalizeContentPath(entry.ContentPath);
            // Per-package catalogs contain only assets physically packed into that package.
            if (contentPathPrefixes is not null && !IsUnderAny(contentPath, contentPathPrefixes))
            {
                continue;
            }

            var compiledPath = Path.Combine(compiledDirectory, contentPath + ".xnb");
            if (!File.Exists(compiledPath))
            {
                continue;
            }

            assets.Add(entry with { ContentPath = contentPath });
        }

        if (assets.Count == 0)
        {
            return 0;
        }

        var catalog = new AssetCatalog
        {
            PackageId = packageId ?? Path.GetFileName(compiledDirectory),
            Assets = [.. assets]
        };

        var catalogPath = outputPath ?? Path.Combine(compiledDirectory, AssetCatalog.DefaultFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(catalogPath)!);
        using var catalogStream = File.Create(catalogPath);
        AssetCatalogSerializer.Save(catalogStream, catalog);
        return assets.Count;
    }

    private static bool IsUnderAny(string contentPath, IReadOnlyCollection<string> prefixes)
    {
        foreach (var rawPrefix in prefixes)
        {
            var prefix = rawPrefix.Replace('\\', '/').Trim('/');
            if (prefix.Length == 0 || prefix == "." ||
                contentPath.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                contentPath.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}