using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.Tests.Doubles;
using PurrplingCore.Toolkit.Tests.Fixtures;
using Xunit;

namespace PurrplingCore.Toolkit.Tests.Content;

public sealed class AssetManagerCacheTests
{
    [Fact]
    public void Get_ReusesAssetWhileRetained()
    {
        using var fixture = new AssetManagerFixture();
        var id = fixture.Register("extra", "Textures/Fur");
        using var first = fixture.Manager.Get<TestAsset>(id);
        using var second = fixture.Manager.Get<TestAsset>(id);

        Assert.Same(first.Asset, second.Asset);
        Assert.Single(fixture.Loader.LoadPaths);
    }

    [Fact]
    public void Get_KeepsDifferentAssetsSeparate()
    {
        using var fixture = new AssetManagerFixture();
        var firstId = fixture.Register("extra", "Textures/Fur");
        var secondId = fixture.Register("extra", "Textures/Eyes");
        using var first = fixture.Manager.Get<TestAsset>(firstId);
        using var second = fixture.Manager.Get<TestAsset>(secondId);

        Assert.NotSame(first.Asset, second.Asset);
        Assert.Equal(2, fixture.Loader.LoadPaths.Count);
    }

    [Fact]
    public void Get_UsesCacheRegisteredInDependencyInjection()
    {
        using var fixture = new AssetManagerFixture(registerCache: true);
        var id = fixture.Register("extra", "Textures/Fur");
        using var handle = fixture.Manager.Get<TestAsset>(id);

        Assert.True(fixture.Cache.TryRetain(id, out var cached));
        Assert.Same(handle.Asset, cached);
        Assert.False(fixture.Cache.Release(id));
    }

    // The first release keeps the shared asset alive; only the final release unloads and notifies.
    [Fact]
    public void Dispose_UnloadsAndNotifiesOnlyAfterFinalRelease()
    {
        using var fixture = new AssetManagerFixture();
        var id = fixture.Register("extra", "Textures/Fur");
        var unloaded = new List<AssetId>();
        fixture.Manager.AssetsUnloaded += ids => unloaded.AddRange(ids.ToArray());
        var first = fixture.Manager.Get<TestAsset>(id);
        var second = fixture.Manager.Get<TestAsset>(id);

        first.Dispose();

        Assert.Empty(fixture.ContentProvider.CreatedManager.UnloadPaths);
        Assert.Empty(unloaded);
        Assert.False(second.Asset.IsDisposed);

        second.Dispose();

        Assert.Equal(id, Assert.Single(unloaded));
        Assert.True(second.Asset.IsDisposed);
        Assert.Equal(
            Assert.Single(fixture.Loader.LoadPaths),
            Assert.Single(fixture.ContentProvider.CreatedManager.UnloadPaths));
    }

    [Fact]
    public void Get_ReloadsAfterFinalRelease()
    {
        using var fixture = new AssetManagerFixture();
        var id = fixture.Register("extra", "Textures/Fur");
        var first = fixture.Manager.Get<TestAsset>(id);
        first.Dispose();

        using var second = fixture.Manager.Get<TestAsset>(id);

        Assert.NotSame(first.Asset, second.Asset);
        Assert.Equal(2, fixture.Loader.LoadPaths.Count);
        Assert.False(second.Asset.IsDisposed);
    }
}