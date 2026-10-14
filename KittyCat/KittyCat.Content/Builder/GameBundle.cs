using MonoGame.Framework.Content.Pipeline.Builder;
using PurrplingCore.ContentBuilder;

internal class GameBundle : AssetBundle
{
    public GameBundle() : base("Game", AssetBundleType.Pak)
    {
    }

    protected override IContentCollection CreateContentCollection()
    {
        var collection = new ContentCollection();
        collection.Include<WildcardRule>("Fonts/*.spritefont");

        return collection;
    }
}
