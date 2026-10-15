using PurrplingCore.ContentBuilder.Tests.Fixtures;
using PurrplingCore.Toolkit.Content;
using Xunit;

namespace PurrplingCore.ContentBuilder.Tests.Catalogs;

public sealed class AssetCatalogBuilderTests
{
    [Fact]
    public void Generate_NoMatchingAssetsWritesEmptyCatalog()
    {
        using var files = new TemporaryContentDirectory();
        var sourceDirectory = Path.Combine(files.Root, "Assets");
        Directory.CreateDirectory(sourceDirectory);
        File.WriteAllText(Path.Combine(sourceDirectory, "Fur.meta.json"), """
            { "assetFile": "Fur.png" }
            """);
        File.WriteAllBytes(Path.Combine(sourceDirectory, "Fur.png"), [1, 2, 3]);

        var compiledDirectory = Path.Combine(files.Root, "obj", "Game", "Content");
        var catalogPath = Path.Combine(compiledDirectory, AssetCatalog.DirectoryName, "Game.bin");

        var count = AssetCatalogBuilder.Generate(
            sourceDirectory,
            compiledDirectory,
            "Game",
            "Game",
            catalogPath);

        Assert.Equal(0, count);
        Assert.True(File.Exists(catalogPath));

        using var stream = File.OpenRead(catalogPath);
        var catalog = AssetCatalogSerializer.Load(stream);
        Assert.Equal("Game", catalog.PackageId);
        Assert.Empty(catalog.Assets);
    }
}