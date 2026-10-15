using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.Tests.Doubles;
using PurrplingCore.Toolkit.Tests.Fixtures;
using Xunit;

namespace PurrplingCore.Toolkit.Tests.Content;

public sealed class AssetManagerFailureTests
{
    [Fact]
    public void Get_ThrowsWhenAssetIsStillMissingAfterRefresh()
    {
        using var fixture = new AssetManagerFixture();
        var id = new AssetId("extra:Missing");

        var exception = Assert.Throws<KeyNotFoundException>(() =>
        {
            fixture.Manager.Get<TestAsset>(id);
        });

        Assert.Contains(id.Guid.ToString(), exception.Message);
        Assert.Empty(fixture.Loader.LoadPaths);
    }

    [Fact]
    public void Get_ThrowsWhenLoaderIsNotRegistered()
    {
        using var fixture = new AssetManagerFixture(registerLoader: false);
        var id = fixture.Register("extra", "Textures/Fur");

        var exception = Assert.Throws<NotSupportedException>(() =>
        {
            fixture.Manager.Get<TestAsset>(id);
        });

        Assert.Contains(typeof(TestAsset).ToString(), exception.Message);
        Assert.Empty(fixture.Loader.LoadPaths);
    }

    [Fact]
    public void Get_DoesNotCacheFailedLoads()
    {
        using var fixture = new AssetManagerFixture();
        var id = fixture.Register("extra", "Textures/Fur");
        fixture.Loader.FailLoad = true;

        // A failed load must leave the cache empty so a later request can retry.
        Assert.Throws<ContentLoadException>(() =>
        {
            fixture.Manager.Get<TestAsset>(id);
        });

        fixture.Loader.FailLoad = false;
        using var handle = fixture.Manager.Get<TestAsset>(id);

        Assert.Equal(2, fixture.Loader.LoadPaths.Count);
        Assert.False(handle.Asset.IsDisposed);
    }
}