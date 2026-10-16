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
            fixture.Resolver.RegisterMountPoint("extra", mappedRoot);
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
        fixture.Resolver.RegisterMountPoint("extra", "Mods/Extra");
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
        fixture.Resolver.RegisterMountPoint("A", "Mods/A");
        fixture.Resolver.RegisterMountPoint("B", "Mods/B");
        fixture.Resolver.RegisterMountPoint("Unused", "Mods/Unused");
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
        resolver.RegisterMountPoint("Extra", "Content/Extra");
        resolver.RegisterMountPoint("Other", "Content/Other");
        resolver.RegisterMountPoint("Extra", "Mods/Extra");
        resolver.RegisterMountPoint("Extra", "Mods/Extra");

        Assert.Equal("Mods/Extra", resolver.Resolve("extra"));
        Assert.Equal("Content/Other", resolver.Resolve("Other"));

        Assert.True(resolver.UnregisterMountPoint("extra"));
        Assert.Null(resolver.Resolve("Extra"));
        Assert.Equal("Content/Other", resolver.Resolve("Other"));
        Assert.False(resolver.UnregisterMountPoint("Extra"));
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