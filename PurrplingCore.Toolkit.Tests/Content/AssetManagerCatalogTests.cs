using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.Tests.Doubles;
using PurrplingCore.Toolkit.Tests.Fixtures;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Tests.Content;

public sealed class AssetManagerCatalogTests
{
    [Fact]
    public void Get_UsesExplicitPackageRegistrationAndCatalogIndexing()
    {
        using var fixture = new AssetManagerFixture();
        var id = new AssetId("extra:Fur");
        fixture.Files.CreateDirectory("/Content");
        using (var stream = fixture.Files.CreateFile("/Content/asset-catalog.bin"))
        {
            AssetCatalogSerializer.Save(stream, new AssetCatalog
            {
                PackageId = "Extra",
                Assets = [new AssetCatalogEntry { Id = id.FullName.ToString(), PackageName = "extra", ObjectPath = "Compiled/Fur" }]
            });
        }

        fixture.Resolver.RegisterMountPoint(new Name("extra"), "Ready");
        fixture.Indexer.Refresh();
        using var handle = fixture.Manager.Get<TestAsset>(id);

        Assert.NotNull(fixture.Registry.GetMetadata(id));
        Assert.Equal("Ready", fixture.ContentProvider.CreatedManager.RootDirectory);
        Assert.Equal("Compiled/Fur", Normalize(Assert.Single(fixture.Loader.LoadPaths)));
    }

    [Fact]
    public void Get_LoadsAssetFromCatalogImportedByCustomParser()
    {
        using var fixture = new AssetManagerFixture();
        var id = new AssetId("extra:Fur");

        // Mods can parse their own format, then hand the shared metadata model to the registry.
        new AssetCatalog
        {
            PackageId = "ParsedModCatalog",
            Assets = [new AssetCatalogEntry { Id = id.FullName.ToString(), PackageName = "extra", ObjectPath = "Mod/Textures/Fur" }]
        }.Import(fixture.Registry, "ParsedModCatalog");

        using var handle = fixture.Manager.Get<TestAsset>(id);

        Assert.Equal("Content", fixture.ContentProvider.CreatedManager.RootDirectory);
        Assert.Equal("Mod/Textures/Fur", Normalize(Assert.Single(fixture.Loader.LoadPaths)));
    }

    [Fact]
    public void Get_LoadsAssetsAfterExplicitPackageRegistrationAndCatalogIndexing()
    {
        using var fixture = new AssetManagerFixture();
        fixture.Resolver.RegisterMountPoint(new Name("Textures"), "Dlc1");
        fixture.Resolver.RegisterMountPoint(new Name("Sounds"), "Dlc2");
        fixture.Files.CreateDirectory($"/Content/{AssetCatalog.DirectoryName}");
        using (var stream = fixture.Files.CreateFile($"/Content/{AssetCatalog.DirectoryName}/Dlc.bin"))
        {
            AssetCatalogSerializer.Save(stream, new AssetCatalog
            {
                PackageId = "Dlc",
                Assets =
                [
                    new AssetCatalogEntry { Id = "dlc:Textures/Fur", PackageName = "Textures", ObjectPath = "Textures/Fur", FileExtension = ".png" },
                    new AssetCatalogEntry { Id = "dlc:Sounds/Click", PackageName = "Sounds", ObjectPath = "Sounds/Click", FileExtension = ".wav" }
                ]
            });
        }

        fixture.Indexer.Refresh();
        using var fur = fixture.Manager.Get<TestAsset>(new AssetId("dlc:Textures/Fur"));
        using var click = fixture.Manager.Get<TestAsset>(new AssetId("dlc:Sounds/Click"));

        Assert.Equal(2, fixture.Registry.GetAllMetadata().Count());
        Assert.Equal(["Dlc1", "Dlc2"], fixture.ContentProvider.CreatedManagers.Select(manager => manager.RootDirectory));
        Assert.Equal(["Textures/Fur.png", "Sounds/Click.wav"], fixture.Loader.LoadPaths.Select(Normalize));
    }

    [Fact]
    public void Get_IgnoresJsonCatalog()
    {
        using var fixture = new AssetManagerFixture();
        var id = new AssetId("extra:Fur");
        fixture.Files.CreateDirectory("/Content");
        using (var stream = fixture.Files.CreateFile("/Content/asset-catalog.json"))
        {
            stream.Write("{}"u8);
        }

        Assert.Throws<KeyNotFoundException>(() =>
        {
            fixture.Manager.Get<TestAsset>(id);
        });
        Assert.Null(fixture.Registry.GetMetadata(id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("DLC/Winter/Content")]
    public void Get_PackageMountOverridesEngineDefaultForLoadAndUnload(string root)
    {
        using var fixture = new AssetManagerFixture();
        fixture.Resolver.RegisterMountPoint(new Name("game"), "Content");
        fixture.Resolver.RegisterMountPoint(new Name("Game"), root);
        var id = new AssetId("game:Fonts/Hud");
        fixture.Files.CreateDirectory("/Content");
        using (var stream = fixture.Files.CreateFile("/Content/asset-catalog.bin"))
        {
            AssetCatalogSerializer.Save(stream, new AssetCatalog
            {
                PackageId = "Winter",
                Assets = [new AssetCatalogEntry
                {
                    Id = id.FullName.ToString(),
                    PackageName = "Game",
                    ObjectPath = "Fonts/Hud"
                }]
            });
        }

        fixture.Indexer.Refresh();
        var handle = fixture.Manager.Get<TestAsset>(id);
        handle.Dispose();

        Assert.Equal(root, fixture.ContentProvider.CreatedManager.RootDirectory);
        Assert.Equal("Fonts/Hud", Normalize(Assert.Single(fixture.Loader.LoadPaths)));
        Assert.Equal("Fonts/Hud", Normalize(Assert.Single(fixture.ContentProvider.CreatedManager.UnloadPaths)));
        var metadata = Assert.IsType<AssetMetadata>(fixture.Registry.GetMetadata(id));
        Assert.Equal(id, metadata.Id);
        Assert.Equal("Game", metadata.PackageName.PlainName);
        Assert.Equal(root, fixture.Resolver.Resolve(new Name("Game")));
    }

    [Theory]
    [InlineData("../Outside")]
    [InlineData("C:/Outside")]
    public void Mount_RejectsNonRelativeContentRoots(string root)
    {
        var packages = new PackageManager();
        Assert.Throws<ArgumentException>(() => packages.RegisterMountPoint(new Name("Game"), root));
    }

    [Fact]
    public void Import_RegistersMetadataWithoutRegisteringPackageMounts()
    {
        using var fixture = new AssetManagerFixture();
        using var stream = new MemoryStream();
        AssetCatalogSerializer.Save(stream, new AssetCatalog
        {
            PackageId = "Winter",
            Assets =
            [
                new AssetCatalogEntry { Id = "winter:Fonts/Hud", PackageName = "Winter", ObjectPath = "Fonts/Hud" },
                new AssetCatalogEntry { Id = "winter:Fonts/Title", PackageName = "Winter", ObjectPath = "Fonts/Title" }
            ]
        });
        stream.Position = 0;

        var catalog = AssetCatalogSerializer.Load(stream);
        catalog.Import(fixture.Registry, catalog.PackageId);

        var hud = Assert.IsType<AssetMetadata>(fixture.Registry.GetMetadata(new AssetId("winter:Fonts/Hud")));
        var title = Assert.IsType<AssetMetadata>(fixture.Registry.GetMetadata(new AssetId("winter:Fonts/Title")));
        Assert.Null(fixture.Resolver.Resolve(hud.PackageName));
        Assert.Null(fixture.Resolver.Resolve(title.PackageName));
        Assert.Null(typeof(AssetMetadata).GetProperty("ContentRoot"));
        Assert.Null(typeof(AssetMetadata).GetProperty("ContentRootIndex"));
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    [Fact]
    public void Mount_RejectsInvalidRootWithoutChangingTheExistingPackage()
    {
        using var fixture = new AssetManagerFixture();
        fixture.Resolver.RegisterMountPoint(new Name("Winter"), "Fallback");
        Assert.Throws<ArgumentException>(() => fixture.Resolver.RegisterMountPoint(new Name("Winter"), "../Outside"));

        Assert.Equal("Fallback", fixture.Resolver.Resolve(new Name("winter")));
    }

    [Fact]
    public void Index_DoesNotChangeExistingPackageMounts()
    {
        using var fixture = new AssetManagerFixture();
        fixture.Resolver.RegisterMountPoint(new Name("Winter"), "DLC/Winter");
        var conflicting = new AssetCatalog
        {
            PackageId = "Other",
            Assets = [new AssetCatalogEntry { Id = "winter:Fonts/Hud", PackageName = "Winter", ObjectPath = "Fonts/Hud" }]
        };

        IndexCatalog(fixture, conflicting);

        Assert.NotNull(fixture.Registry.GetMetadata(new AssetId("winter:Fonts/Hud")));
        Assert.Equal("DLC/Winter", fixture.Resolver.Resolve(new Name("Winter")));
    }

    private static void IndexCatalog(AssetManagerFixture fixture, AssetCatalog catalog)
    {
        fixture.Files.CreateDirectory($"/Content/{AssetCatalog.DirectoryName}");
        using (var stream = fixture.Files.CreateFile($"/Content/{AssetCatalog.DirectoryName}/{catalog.PackageId}.bin"))
        {
            AssetCatalogSerializer.Save(stream, catalog);
        }

        fixture.Indexer.Refresh();
    }

    [Theory]
    [InlineData("Mods/Winter")]
    [InlineData("DLC/Relocated")]
    public void Get_UsesExplicitMountRootWithoutChangingCatalog(string mountRoot)
    {
        using var fixture = new AssetManagerFixture();
        fixture.Resolver.RegisterMountPoint(new Name("Winter"), mountRoot + "/Content");
        fixture.Files.CreateDirectory("/Content/AssetCatalogs");
        var id = new AssetId("winter:Fonts/Hud");
        using (var stream = fixture.Files.CreateFile("/Content/AssetCatalogs/Winter.bin"))
        {
            AssetCatalogSerializer.Save(stream, new AssetCatalog
            {
                PackageId = "Winter",
                Assets = [new AssetCatalogEntry { Id = id.FullName.ToString(), PackageName = "Winter", ObjectPath = "Fonts/Hud" }]
            });
        }

        using var mounted = new MountFileSystem();
        mounted.Mount(new UPath("/" + mountRoot), fixture.Files);
        var provider = new PurrplingCore.Toolkit.Hosting.VirtualFileProvider(mounted);
        fixture.Indexer.Index(provider, mountRoot);

        var handle = fixture.Manager.Get<TestAsset>(id);
        handle.Dispose();

        Assert.Equal(mountRoot + "/Content", fixture.ContentProvider.CreatedManager.RootDirectory);
        Assert.Equal("Fonts/Hud", Normalize(Assert.Single(fixture.Loader.LoadPaths)));
        Assert.Equal("Fonts/Hud", Normalize(Assert.Single(fixture.ContentProvider.CreatedManager.UnloadPaths)));
        Assert.Equal("Fonts/Hud", fixture.Registry.GetMetadata(id)!.ObjectPath);
    }

    [Fact]
    public void Mount_KeepsManagerAlreadyCreatedForPackage()
    {
        using var fixture = new AssetManagerFixture();
        var id = fixture.Register("Winter", "Fonts/Hud");
        var metadata = fixture.Registry.GetMetadata(id);
        fixture.Resolver.RegisterMountPoint(new Name("Winter"), "OldLocation/Content");
        fixture.Manager.Get<TestAsset>(id).Dispose();

        fixture.Resolver.RegisterMountPoint(new Name("Winter"), "NewLocation/Content");
        fixture.Manager.Get<TestAsset>(id).Dispose();

        Assert.Equal(["OldLocation/Content"], fixture.ContentProvider.CreatedManagers.Select(manager => manager.RootDirectory));
        Assert.Equal(["Fonts/Hud", "Fonts/Hud"], fixture.Loader.LoadPaths.Select(Normalize));
        Assert.Same(metadata, fixture.Registry.GetMetadata(id));
        Assert.True(fixture.Resolver.UnregisterMountPoint(new Name("winter")));
        Assert.Null(fixture.Resolver.Resolve(new Name("Winter")));
    }

}