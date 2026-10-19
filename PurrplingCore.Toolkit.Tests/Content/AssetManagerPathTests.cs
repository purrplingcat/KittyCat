using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.Tests.Doubles;
using PurrplingCore.Toolkit.Tests.Fixtures;
using Xunit;

namespace PurrplingCore.Toolkit.Tests.Content;

public sealed class AssetManagerPathTests
{
    // A mapped package root replaces the default root; ObjectPath controls the relative file location.
    [Theory]
    [InlineData(null, "Textures/Fur", "Content")]
    [InlineData(null, "Compiled/Fur", "Content")]
    [InlineData(null, "Textures/Fur", "Resources")]
    [InlineData("Mods/Extra", "Textures/Fur", "Content")]
    [InlineData("Mods/Extra", "Compiled/Fur", "Content")]
    public void Get_ResolvesMetadataPath(
        string? mappedRoot, string objectPath, string defaultRoot)
    {
        using var fixture = new AssetManagerFixture(defaultRoot);
        if (mappedRoot is not null)
        {
            fixture.Resolver.RegisterMountPoint(new Name("extra"), mappedRoot);
        }

        var id = fixture.Register("extra", objectPath);
        using var handle = fixture.Manager.Get<TestAsset>(id);

        Assert.Equal(objectPath, Normalize(Assert.Single(fixture.Loader.LoadPaths)));
        Assert.Equal(mappedRoot ?? defaultRoot, fixture.ContentProvider.CreatedManager.RootDirectory);
    }

    [Fact]
    public void Dispose_UsesMappedPathForUnload()
    {
        using var fixture = new AssetManagerFixture();
        fixture.Resolver.RegisterMountPoint(new Name("extra"), "Mods/Extra");
        var id = fixture.Register("extra", "Compiled/Fur");
        var handle = fixture.Manager.Get<TestAsset>(id);

        handle.Dispose();

        Assert.Equal(
            "Compiled/Fur",
            Normalize(Assert.Single(fixture.ContentProvider.CreatedManager.UnloadPaths)));
        Assert.Equal("Mods/Extra", fixture.ContentProvider.CreatedManager.RootDirectory);
    }

    [Fact]
    public void Get_CreatesManagersOnlyForLoadedPackagesAndReusesEachOne()
    {
        using var fixture = new AssetManagerFixture();
        fixture.Resolver.RegisterMountPoint(new Name("A"), "Mods/A");
        fixture.Resolver.RegisterMountPoint(new Name("B"), "Mods/B");
        fixture.Resolver.RegisterMountPoint(new Name("Unused"), "Mods/Unused");
        Assert.Empty(fixture.ContentProvider.CreatedManagers);

        using var first = fixture.Manager.Get<TestAsset>(fixture.Register("A", "Textures/First"));
        var firstManager = fixture.ContentProvider.CreatedManager;
        using var second = fixture.Manager.Get<TestAsset>(fixture.Register("A", "Textures/Second"));
        Assert.Same(firstManager, fixture.ContentProvider.CreatedManager);

        using var third = fixture.Manager.Get<TestAsset>(fixture.Register("B", "Textures/Third"));
        Assert.Equal(["Mods/A", "Mods/B"], fixture.ContentProvider.CreatedManagers.Select(manager => manager.RootDirectory));

        second.Dispose();
        Assert.Equal(["Textures/Second"], firstManager.UnloadPaths.Select(Normalize));
        Assert.Empty(fixture.ContentProvider.CreatedManager.UnloadPaths);
    }

    [Fact]
    public void Get_CreatesSeparateManagersForUnregisteredPackages()
    {
        using var fixture = new AssetManagerFixture();
        using var first = fixture.Manager.Get<TestAsset>(fixture.Register("A", "Textures/Fur"));
        using var second = fixture.Manager.Get<TestAsset>(fixture.Register("B", "Textures/Fur"));

        Assert.Equal(["Content", "Content"], fixture.ContentProvider.CreatedManagers.Select(manager => Normalize(manager.RootDirectory)));
        Assert.Equal(["Textures/Fur", "Textures/Fur"], fixture.Loader.LoadPaths.Select(Normalize));
    }

    [Fact]
    public void PackageManager_ReplacesOneRootAndKeepsOtherPackagesIndependent()
    {
        var resolver = new PackageManager();
        resolver.RegisterMountPoint(new Name("Extra"), "Content/Extra");
        resolver.RegisterMountPoint(new Name("Other"), "Content/Other");
        resolver.RegisterMountPoint(new Name("Extra"), "Mods/Extra");
        resolver.RegisterMountPoint(new Name("Extra"), "Mods/Extra");

        Assert.Equal("Mods/Extra", resolver.Resolve(new Name("extra")));
        Assert.Equal("Content/Other", resolver.Resolve(new Name("Other")));

        Assert.True(resolver.UnregisterMountPoint(new Name("extra")));
        Assert.Null(resolver.Resolve(new Name("Extra")));
        Assert.Equal("Content/Other", resolver.Resolve(new Name("Other")));
        Assert.False(resolver.UnregisterMountPoint(new Name("Extra")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    [InlineData("\u2003")]
    public void PackagesRejectNoneNames(string? value)
    {
        var packageName = new Name(value);
        var packages = new PackageManager();

        Assert.Throws<ArgumentException>(() => packages.RegisterMountPoint(packageName, "Content"));
        Assert.Throws<ArgumentException>(() =>
            new AssetMetadata(new AssetId("game:Texture"), packageName, "Texture"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AssetId_RejectsNullOrEmptyFullNamesAtConstruction(string? fullName)
    {
        Assert.ThrowsAny<ArgumentException>(() => new AssetId(fullName!));
    }

    [Theory]
    [InlineData("Texture")]
    [InlineData(":Texture")]
    [InlineData("game:")]
    [InlineData("game:Textures//Fur")]
    [InlineData("game:Textures/../Fur")]
    public void AssetId_AcceptsExplicitNamesWithoutPathRestrictions(string fullName)
    {
        var id = new AssetId(fullName);

        Assert.False(id.IsEmpty);
        Assert.Equal(fullName, id.FullName.ToString());
    }

    [Fact]
    public void Metadata_RejectsDefaultAssetId()
    {
        Assert.Throws<ArgumentException>(() =>
            new AssetMetadata(AssetId.Empty, new Name("game"), "Texture"));
    }

    [Theory]
    [InlineData("game:_0")]
    [InlineData("game:Textures/Fur_42")]
    public void Metadata_PreservesValidatedNumberedIdentifier(string fullName)
    {
        var id = new AssetId(fullName);
        var metadata = new AssetMetadata(id, new Name("game"), "Texture");

        Assert.Equal(id, metadata.Id);
        Assert.Equal(id.FullName, metadata.Name);
        Assert.Equal(fullName, metadata.Name.ToString());
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    [Theory]
    [InlineData(null, "Resources")]
    [InlineData("Game", "Game")]
    [InlineData("", "")]
    public void ContentProvider_UsesLiteralRootWithoutPackageServices(string? root, string expectedRoot)
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var provider = new DefaultContentManagerProvider(
            services, Options.Create(new ContentManagerOptions { ContentRoot = "Resources" }));
        using var defaultContent = provider.ContentManager;
        using var content = provider.CreateContentManager(root);

        Assert.Equal("Resources", defaultContent.RootDirectory);
        Assert.Equal(expectedRoot, content.RootDirectory);
    }
}