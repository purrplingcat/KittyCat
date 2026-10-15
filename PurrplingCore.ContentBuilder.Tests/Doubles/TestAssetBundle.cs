using MonoGame.Framework.Content.Pipeline.Builder;
using PurrplingCore.ContentBuilder;

namespace PurrplingCore.ContentBuilder.Tests.Doubles;

internal sealed class TestAssetBundle : AssetBundle
{
    private readonly string _contentRoot;

    public TestAssetBundle(
        string name,
        bool generateAssetCatalog,
        string contentRoot = "Content",
        string? packageName = null)
        : base(name, AssetBundleType.Pak)
    {
        GenerateAssetCatalog = generateAssetCatalog;
        if (packageName is not null)
        {
            PackageName = packageName;
        }
        _contentRoot = contentRoot;
    }

    protected override IContentCollection CreateContentCollection()
    {
        var collection = new ContentCollection();
        collection.SetContentRoot(_contentRoot);
        collection.Include<WildcardRule>("Textures/*.png");
        return collection;
    }
}

internal sealed class MultiRootAssetBundle(string name) : AssetBundle(name, AssetBundleType.Pak)
{
    protected override IContentCollection CreateContentCollection()
    {
        var collection = new ContentCollection();
        collection.SetContentRoot("Dlc1");
        collection.Include<WildcardRule>("Textures/*.png");
        collection.SetContentRoot("Dlc2");
        collection.Include<WildcardRule>("Sounds/*.wav");
        return collection;
    }
}