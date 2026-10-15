using Microsoft.Extensions.Logging.Abstractions;
using PurrplingCore.ContentBuilder.Tests.Doubles;
using PurrplingCore.ContentBuilder.Tests.Fixtures;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.Vfs;
using PurrplingCore.Toolkit.Vfs.FileSystems;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.ContentBuilder.Tests.Catalogs;

public sealed class AssetCatalogPublishTests
{
    [Fact]
    public void GenerateCatalog_UsesBundlePackageNameAndLocalAssetName()
    {
        using var files = new TemporaryContentDirectory();
        var bundle = new TestAssetBundle("HudBundle", true, packageName: "GameContent");
        CreateXnbBundleFiles(files.Root, bundle.Name);
        var builder = new ContentBuilder(NullLogger<ContentBuilder>.Instance, [bundle]);
        var options = CreateOptions(files.Root);

        builder.GenerateCatalog(options, bundle);
        builder.Publish(options, bundle);

        using var pak = OpenBundle(files.Root, bundle.Name);
        using var catalogStream = pak.OpenRead(new UPath($"/Content/{AssetCatalog.DirectoryName}/HudBundle.bin"));
        var catalog = AssetCatalogSerializer.Load(catalogStream);
        var entry = Assert.Single(catalog.Assets);

        Assert.Equal("GameContent", entry.PackageName);
        var redirect = Assert.Single(catalog.Redirects);
        Assert.Equal("Content", redirect.ContentRoot);
        Assert.Equal("GameContent", redirect.PackageName);
        Assert.Equal(string.Empty, redirect.ObjectPathPrefix);
        Assert.Equal("Fonts/Hud", entry.ObjectPath);
        Assert.Null(entry.FileExtension);
        Assert.Equal("gamecontent:Fonts/Hud", entry.Id);
        Assert.True(pak.FileExists(new UPath("/Content/Fonts/Hud.xnb")));
    }

    [Fact]
    public void GenerateCatalog_StoresRawFileExtension()
    {
        using var files = new TemporaryContentDirectory();
        var bundle = new MultiRootAssetBundle("RawAssets");
        CreateRawBundleFiles(files.Root, bundle.Name);
        var builder = new ContentBuilder(NullLogger<ContentBuilder>.Instance, [bundle]);
        var options = CreateOptions(files.Root);

        builder.GenerateCatalog(options, bundle);
        builder.Publish(options, bundle);

        using var pak = OpenBundle(files.Root, bundle.Name);
        using var catalogStream = pak.OpenRead(new UPath($"/Content/{AssetCatalog.DirectoryName}/RawAssets.bin"));
        var catalog = AssetCatalogSerializer.Load(catalogStream);
        Assert.Equal(2, catalog.Assets.Length);
        Assert.Equal("rawassets:Textures/Fur", Assert.Single(catalog.Assets, entry => entry.ObjectPath == "Textures/Fur").Id);
        Assert.Equal("sfx:click", Assert.Single(catalog.Assets, entry => entry.ObjectPath == "Sounds/Click").Id);
        Assert.Equal(2, catalog.Redirects.Length);
        Assert.Equal("Dlc1", Assert.Single(catalog.Redirects, redirect => redirect.ObjectPathPrefix == "Textures").ContentRoot);
        Assert.Equal("Dlc2", Assert.Single(catalog.Redirects, redirect => redirect.ObjectPathPrefix == "Sounds").ContentRoot);
        Assert.Collection(
            catalog.Assets.OrderBy(entry => entry.ObjectPath),
            entry => Assert.Equal(("Sounds/Click", ".wav"), (entry.ObjectPath, entry.FileExtension)),
            entry => Assert.Equal(("Textures/Fur", ".png"), (entry.ObjectPath, entry.FileExtension)));
        Assert.True(pak.FileExists(new UPath("/Dlc1/Textures/Fur.png")));
        Assert.True(pak.FileExists(new UPath("/Dlc2/Sounds/Click.wav")));
    }

    [Fact]
    public void GenerateCatalog_OverlayRemovesStaleCatalog()
    {
        using var files = new TemporaryContentDirectory();
        var bundle = new TestAssetBundle("Overlay", generateAssetCatalog: false);
        CreateXnbBundleFiles(files.Root, bundle.Name);
        var staleCatalogPath = Path.Combine(files.Root, "obj", bundle.Name, "Content", AssetCatalog.DirectoryName, "Overlay.bin");
        Directory.CreateDirectory(Path.GetDirectoryName(staleCatalogPath)!);
        File.WriteAllText(staleCatalogPath, "stale catalog");
        var builder = new ContentBuilder(NullLogger<ContentBuilder>.Instance, [bundle]);
        var options = CreateOptions(files.Root);

        builder.GenerateCatalog(options, bundle);
        builder.Publish(options, bundle);

        using var pak = OpenBundle(files.Root, bundle.Name);
        Assert.True(pak.FileExists(new UPath("/Content/Fonts/Hud.xnb")));
        Assert.False(pak.FileExists(new UPath($"/Content/{AssetCatalog.DirectoryName}/Overlay.bin")));
    }

    [Fact]
    public void GenerateCatalog_StoresSharedRootOnlyOnce()
    {
        using var files = new TemporaryContentDirectory();
        var bundle = new TestAssetBundle("SharedRoot", true);
        CreateXnbBundleFiles(files.Root, bundle.Name);
        var sourceDirectory = Path.Combine(files.Root, "Assets", "Fonts");
        File.Copy(Path.Combine(sourceDirectory, "Hud.spritefont"), Path.Combine(sourceDirectory, "Title.spritefont"));
        File.WriteAllText(Path.Combine(sourceDirectory, "Title.meta.json"), """
            { "assetFile": "Title.spritefont" }
            """);
        var outputDirectory = Path.Combine(files.Root, "obj", bundle.Name, "Content", "Fonts");
        File.Copy(Path.Combine(outputDirectory, "Hud.xnb"), Path.Combine(outputDirectory, "Title.xnb"));
        var builder = new ContentBuilder(NullLogger<ContentBuilder>.Instance, [bundle]);
        var options = CreateOptions(files.Root);

        builder.GenerateCatalog(options, bundle);
        builder.Publish(options, bundle);

        using var pak = OpenBundle(files.Root, bundle.Name);
        using var stream = pak.OpenRead(new UPath($"/Content/{AssetCatalog.DirectoryName}/SharedRoot.bin"));
        var catalog = AssetCatalogSerializer.Load(stream);
        var redirect = Assert.Single(catalog.Redirects);
        Assert.Equal("Content", redirect.ContentRoot);
        Assert.Equal(string.Empty, redirect.ObjectPathPrefix);
        Assert.Equal(2, catalog.Assets.Length);
        Assert.Null(typeof(AssetCatalogEntry).GetProperty("ContentRoot"));
        Assert.Null(typeof(AssetCatalogEntry).GetProperty("ContentRootIndex"));
    }

    private static void CreateXnbBundleFiles(string root, string bundleName)
    {
        var sourceDirectory = Path.Combine(root, "Assets", "Fonts");
        Directory.CreateDirectory(sourceDirectory);
        File.WriteAllText(Path.Combine(sourceDirectory, "Hud.meta.json"), """
            {
                            "assetFile": "Hud.spritefont",
              "assetClass": "SpriteFont",
              "tags": {}
            }
            """);
                File.WriteAllText(Path.Combine(sourceDirectory, "Hud.spritefont"), "test font source");

        var compiledPath = Path.Combine(root, "obj", bundleName, "Content", "Fonts", "Hud.xnb");
        Directory.CreateDirectory(Path.GetDirectoryName(compiledPath)!);
        File.WriteAllBytes(compiledPath, [1, 2, 3]);
    }

    private static void CreateRawBundleFiles(string root, string bundleName)
    {
        var sourceDirectory = Path.Combine(root, "Assets");
        var furMetadataPath = Path.Combine(sourceDirectory, "Textures", "Fur.meta.json");
        var clickMetadataPath = Path.Combine(sourceDirectory, "Sounds", "Click.meta.json");
        Directory.CreateDirectory(Path.GetDirectoryName(furMetadataPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(clickMetadataPath)!);
        File.WriteAllText(furMetadataPath, """
            { "assetFile": "Fur.png" }
            """);
        File.WriteAllText(clickMetadataPath, """
            { "id": "sfx:click", "assetFile": "Click.wav" }
            """);
        File.WriteAllBytes(Path.Combine(sourceDirectory, "Textures", "Fur.png"), [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(sourceDirectory, "Sounds", "Click.wav"), [1, 2, 3]);

        var compiledDirectory = Path.Combine(root, "obj", bundleName);
        var furPath = Path.Combine(compiledDirectory, "Dlc1", "Textures", "Fur.png");
        var clickPath = Path.Combine(compiledDirectory, "Dlc2", "Sounds", "Click.wav");
        Directory.CreateDirectory(Path.GetDirectoryName(furPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(clickPath)!);
        File.WriteAllBytes(furPath, [1, 2, 3]);
        File.WriteAllBytes(clickPath, [1, 2, 3]);
    }

    private static ContentBuilderOptions CreateOptions(string root) => new()
    {
        WorkingDirectory = root,
        SourceDirectory = "Assets",
        IntermediateDirectory = "obj",
        OutputDirectory = "output"
    };

    private static PakFileSystem OpenBundle(string root, string bundleName) => new(
        Path.Combine(root, "output", "Paks", $"{bundleName}.pak"));
}