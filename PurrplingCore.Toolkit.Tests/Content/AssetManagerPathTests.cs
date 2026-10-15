using PurrplingCore.Toolkit.Tests.Doubles;
using PurrplingCore.Toolkit.Tests.Fixtures;
using Xunit;

namespace PurrplingCore.Toolkit.Tests.Content;

public sealed class AssetManagerPathTests
{
    // A mapped package root replaces the default package fallback; ObjectPath controls the file location.
    [Theory]
    [InlineData(null, "Textures/Fur", "Content", "Content/extra/Textures/Fur")]
    [InlineData(null, "Compiled/Fur", "Content", "Content/extra/Compiled/Fur")]
    [InlineData(null, "Textures/Fur", "Resources", "Resources/extra/Textures/Fur")]
    [InlineData("Mods/Extra", "Textures/Fur", "Content", "Mods/Extra/Textures/Fur")]
    [InlineData("Mods/Extra", "Compiled/Fur", "Content", "Mods/Extra/Compiled/Fur")]
    public void Get_ResolvesMetadataPath(
        string? mappedRoot, string objectPath, string defaultRoot, string expectedPath)
    {
        using var fixture = new AssetManagerFixture(defaultRoot);
        if (mappedRoot is not null)
        {
            fixture.Resolver.Map("extra", mappedRoot);
        }

        var id = fixture.Register("extra", objectPath);
        using var handle = fixture.Manager.Get<TestAsset>(id);

        Assert.Equal(expectedPath, Normalize(Assert.Single(fixture.Loader.LoadPaths)));
        Assert.Equal(string.Empty, fixture.ContentProvider.CreatedManager.RootDirectory);
    }

    [Fact]
    public void Dispose_UsesMappedPathForUnload()
    {
        using var fixture = new AssetManagerFixture();
        fixture.Resolver.Map("extra", "Mods/Extra");
        var id = fixture.Register("extra", "Compiled/Fur");
        var handle = fixture.Manager.Get<TestAsset>(id);

        handle.Dispose();

        Assert.Equal(
            "Mods/Extra/Compiled/Fur",
            Normalize(Assert.Single(fixture.ContentProvider.CreatedManager.UnloadPaths)));
    }

    [Fact]
    public void Resolver_MapsPrefixesWithoutCatalogAndUnmapsAllPackagePaths()
    {
        var resolver = new ContentPathResolver();
        resolver.Map("Extra", "Content/Extra");
        resolver.Map("Extra", "Mods/Extra", "Textures");
        resolver.Map("Extra", "Mods/Extra", "Textures");
        resolver.Map("Extra", "Other", "Textures");

        Assert.Equal("Mods/Extra", resolver.Resolve("extra", "Textures/Fur"));
        Assert.Equal("Content/Extra", resolver.Resolve("extra", "TexturesOther/Fur"));

        Assert.True(resolver.Unmap("extra"));
        Assert.Null(resolver.Resolve("Extra", "Textures/Fur"));
        Assert.False(resolver.Unmap("Extra"));
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
}