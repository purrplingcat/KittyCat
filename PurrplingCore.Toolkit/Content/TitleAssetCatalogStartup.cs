using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;

namespace PurrplingCore.Toolkit.Content;

/// <summary>
/// Loads the application-bundled root catalog through MonoGame's title container.
/// </summary>
/// <remarks>
/// Register this startup service for applications that intentionally run without VFS or direct
/// filesystem access, such as mobile application packages.
/// </remarks>
internal sealed class TitleAssetCatalogStartup(
    AssetCatalogIndexer indexer,
    IGameApplicationLifetime lifetime) : IStartupService
{
    public int Order => 0;

    public void Startup()
    {
        // TitleContainer becomes available as the MonoGame application enters initialization.
        lifetime.GameInitializing.Register(indexer.IndexTitleCatalog);
    }
}