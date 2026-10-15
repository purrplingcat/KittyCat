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
    public void Get_RefreshesBinaryCatalogWhenAssetIsNotRegistered()
    {
        using var fixture = new AssetManagerFixture();
        var id = new AssetId("extra:Fur");
        fixture.Files.CreateDirectory("/Content");
        using (var stream = fixture.Files.CreateFile("/Content/asset-catalog.bin"))
        {
            AssetCatalogSerializer.Save(stream, new AssetCatalog
            {
                PackageId = "Extra",
                Assets = [new AssetCatalogEntry { Id = id.FullName, PackageName = "extra", ObjectPath = "Compiled/Fur" }]
            });
        }

        using var handle = fixture.Manager.Get<TestAsset>(id);

        Assert.NotNull(fixture.Registry.GetMetadata(id));
        Assert.Equal("Content/extra/Compiled/Fur", Normalize(Assert.Single(fixture.Loader.LoadPaths)));
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
            Assets = [new AssetCatalogEntry { Id = id.FullName, PackageName = "extra", ObjectPath = "Mod/Textures/Fur" }]
        }.Import(fixture.Registry, "ParsedModCatalog");

        using var handle = fixture.Manager.Get<TestAsset>(id);

        Assert.Equal("Content/extra/Mod/Textures/Fur", Normalize(Assert.Single(fixture.Loader.LoadPaths)));
    }

    [Fact]
    public void Get_IndexesCatalogFromFixedRootAndLoadsAssetsWithoutCodeRedirects()
    {
        using var fixture = new AssetManagerFixture();
        fixture.Files.CreateDirectory($"/Content/{AssetCatalog.DirectoryName}");
        using (var stream = fixture.Files.CreateFile($"/Content/{AssetCatalog.DirectoryName}/Dlc.bin"))
        {
            AssetCatalogSerializer.Save(stream, new AssetCatalog
            {
                PackageId = "Dlc",
                Redirects =
                [
                    new AssetCatalogRedirect { PackageName = "Dlc", ObjectPathPrefix = "Textures", ContentRoot = "Dlc1" },
                    new AssetCatalogRedirect { PackageName = "Dlc", ObjectPathPrefix = "Sounds", ContentRoot = "Dlc2" }
                ],
                Assets =
                [
                    new AssetCatalogEntry { Id = "dlc:Textures/Fur", PackageName = "Dlc", ObjectPath = "Textures/Fur", FileExtension = ".png" },
                    new AssetCatalogEntry { Id = "dlc:Sounds/Click", PackageName = "Dlc", ObjectPath = "Sounds/Click", FileExtension = ".wav" }
                ]
            });
        }

        using var fur = fixture.Manager.Get<TestAsset>(new AssetId("dlc:Textures/Fur"));
        using var click = fixture.Manager.Get<TestAsset>(new AssetId("dlc:Sounds/Click"));

        Assert.Equal(2, fixture.Registry.GetAllMetadata().Count());
        Assert.Equal(["Dlc1/Textures/Fur.png", "Dlc2/Sounds/Click.wav"], fixture.Loader.LoadPaths.Select(Normalize));
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
    [InlineData("", "Fonts/Hud")]
    [InlineData("DLC/Winter/Content", "DLC/Winter/Content/Fonts/Hud")]
    public void Get_CatalogRootOverridesEngineDefaultForLoadAndUnload(string root, string expectedPath)
    {
        using var fixture = new AssetManagerFixture();
        fixture.Resolver.Map("game", "Content");
        var id = new AssetId("game:Fonts/Hud");
        fixture.Files.CreateDirectory("/Content");
        using (var stream = fixture.Files.CreateFile("/Content/asset-catalog.bin"))
        {
            AssetCatalogSerializer.Save(stream, new AssetCatalog
            {
                PackageId = "Winter",
                Redirects = [new AssetCatalogRedirect { PackageName = "Game", ContentRoot = root }],
                Assets = [new AssetCatalogEntry
                {
                    Id = id.FullName,
                    PackageName = "Game",
                    ObjectPath = "Fonts/Hud"
                }]
            });
        }

        var handle = fixture.Manager.Get<TestAsset>(id);
        handle.Dispose();

        Assert.Equal(expectedPath, Normalize(Assert.Single(fixture.Loader.LoadPaths)));
        Assert.Equal(expectedPath, Normalize(Assert.Single(fixture.ContentProvider.CreatedManager.UnloadPaths)));
        var metadata = Assert.IsType<AssetMetadata>(fixture.Registry.GetMetadata(id));
        Assert.Equal(id, metadata.Id);
        Assert.Equal("Game", metadata.PackageName);
        Assert.Equal(root, fixture.Resolver.Resolve("Game", "Fonts/Hud"));
    }

    [Theory]
    [InlineData("../Outside")]
    [InlineData("/Outside")]
    [InlineData("C:/Outside")]
    public void Save_RejectsNonRelativeContentRoots(string root)
    {
        using var stream = new MemoryStream();
        var catalog = new AssetCatalog
        {
            PackageId = "Invalid",
            Redirects = [new AssetCatalogRedirect { PackageName = "Game", ContentRoot = root }],
            Assets = [new AssetCatalogEntry
            {
                Id = "game:Fonts/Hud",
                PackageName = "Game",
                ObjectPath = "Fonts/Hud"
            }]
        };

        Assert.Throws<ArgumentException>(() => AssetCatalogSerializer.Save(stream, catalog));
    }

    [Fact]
    public void Import_RegistersAssetsWithoutApplyingRedirects()
    {
        using var fixture = new AssetManagerFixture();
        using var stream = new MemoryStream();
        AssetCatalogSerializer.Save(stream, new AssetCatalog
        {
            PackageId = "Winter",
            Redirects = [new AssetCatalogRedirect { PackageName = "Winter", ContentRoot = "DLC\\Winter\\Content" }],
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
        Assert.Single(catalog.Redirects);
        Assert.Null(fixture.Resolver.Resolve(hud.PackageName, hud.ObjectPath));
        Assert.Null(fixture.Resolver.Resolve(title.PackageName, title.ObjectPath));
        Assert.Null(typeof(AssetMetadata).GetProperty("ContentRoot"));
        Assert.Null(typeof(AssetMetadata).GetProperty("ContentRootIndex"));
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    [Fact]
    public void Resolve_UsesLongestPrefixAndRespectsDirectoryBoundaries()
    {
        using var fixture = new AssetManagerFixture();
        fixture.Resolver.Map("Winter", "Fallback");
        var catalog = new AssetCatalog
        {
            PackageId = "Winter",
            Redirects =
            [
                new AssetCatalogRedirect { PackageName = "Winter", ContentRoot = "DLC/Winter" },
                new AssetCatalogRedirect { PackageName = "Winter", ObjectPathPrefix = "Fonts", ContentRoot = "DLC/Fonts" },
                new AssetCatalogRedirect { PackageName = "Winter", ObjectPathPrefix = "Fonts/Titles", ContentRoot = "DLC/Titles" }
            ]
        };
        IndexCatalog(fixture, catalog);

        Assert.Equal("DLC/Fonts", fixture.Resolver.Resolve("winter", "Fonts/Hud"));
        Assert.Equal("DLC/Titles", fixture.Resolver.Resolve("winter", "Fonts/Titles/Main"));
        Assert.Equal("DLC/Winter", fixture.Resolver.Resolve("winter", "FontsExtra/Hud"));
    }

    [Fact]
    public void Index_KeepsFirstRedirectWhenAnotherCatalogConflicts()
    {
        using var fixture = new AssetManagerFixture();
        var catalog = new AssetCatalog
        {
            PackageId = "Winter",
            Redirects = [new AssetCatalogRedirect { PackageName = "Winter", ContentRoot = "DLC/Winter" }]
        };
        IndexCatalog(fixture, catalog);
        var conflicting = new AssetCatalog
        {
            PackageId = "Other",
            Redirects = [new AssetCatalogRedirect { PackageName = "winter", ContentRoot = "DLC/Other" }],
            Assets = [new AssetCatalogEntry { Id = "winter:Fonts/Hud", PackageName = "Winter", ObjectPath = "Fonts/Hud" }]
        };

        IndexCatalog(fixture, conflicting);

        Assert.NotNull(fixture.Registry.GetMetadata(new AssetId("winter:Fonts/Hud")));
        Assert.Equal("DLC/Winter", fixture.Resolver.Resolve("Winter", "Fonts/Hud"));
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
}