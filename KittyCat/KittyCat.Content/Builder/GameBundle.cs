using MonoGame.Framework.Content.Pipeline.Builder;
using PurrplingCore.ContentBuilder;


[AssetBundle("Game_Base", PackageName = "Game", Type = AssetBundleType.Pak)]
internal class GameBundle : AssetBundle
{
    protected override IContentCollection CreateContentCollection()
    {
        var collection = new ContentCollection();
        collection.Include<WildcardRule>("Fonts/*.spritefont");

        return collection;
    }
}
