using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit.Content;

namespace PurrplingCore.Toolkit.Tests.Doubles;

internal sealed class RecordingAssetLoader : IContentLoader<TestAsset>
{
    public List<string> LoadPaths { get; } = [];
    public bool FailLoad { get; set; }

    public TestAsset Load(ContentManager contentManager, string path)
    {
        LoadPaths.Add(path);
        if (FailLoad)
        {
            throw new ContentLoadException("Test load failure.");
        }

        return contentManager.Load<TestAsset>(path);
    }
}

internal sealed class TestAsset : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}