using PurrplingCore.Toolkit.Content;
using System.Text.Json;

namespace KittyCat.Content.Tools;

/// <summary>
/// Builds semantic asset catalogs from source metadata after MonoGame has compiled the content.
/// </summary>
public static class AssetCatalogBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    public static int Generate(string sourceDirectory, string compiledDirectory, string? packageId = null)
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

        var catalogPath = Path.Combine(compiledDirectory, AssetCatalog.DefaultFileName);
        using var catalogStream = File.Create(catalogPath);
        AssetCatalogSerializer.Save(catalogStream, catalog);
        return assets.Count;
    }
}