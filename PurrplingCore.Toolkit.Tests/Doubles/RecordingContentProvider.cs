using Microsoft.Xna.Framework.Content;

namespace PurrplingCore.Toolkit.Tests.Doubles;

internal sealed class RecordingContentProvider(IServiceProvider services, string root) : IContentManagerProvider
{
    public ContentManager ContentManager { get; } = new(services, root);
    public RecordingContentManager CreatedManager { get; private set; } = null!;

    public ContentManager CreateContentManager(string? root = null)
    {
        CreatedManager = new RecordingContentManager(services, root ?? ContentManager.RootDirectory);
        return CreatedManager;
    }
}

internal sealed class RecordingContentManager(IServiceProvider services, string root) : ContentManager(services, root)
{
    public List<string> UnloadPaths { get; } = [];

    public override T Load<T>(string assetName)
    {
        var asset = new TestAsset();
        LoadedAssets[assetName.Replace('\\', '/')] = asset;
        return (T)(object)asset;
    }

    public override void UnloadAsset(string assetName)
    {
        UnloadPaths.Add(assetName);
        base.UnloadAsset(assetName);
    }
}