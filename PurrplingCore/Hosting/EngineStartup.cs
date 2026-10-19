using PurrplingCore.Content;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Vfs;
using Zio.FileSystems;

namespace PurrplingCore.Hosting;

public static class StartupOrder
{
    public const int Initial = -1_000_000;
    public const int PreStartup = -10_000;
    public const int EngineStartup = -1000;
    public const int Default = 0;
    public const int LateStartup = 1000;
    public const int PostStartup = 10_000;
    public const int Final = 1_000_000;
}

/// <summary>
/// Prepares runtime storage, bundled content, and asset metadata before game components are constructed.
/// </summary>
internal sealed class EngineStartup(
    IVirtualFileSystemManager vfs,
    IGame game,
    IHostEnvironment env,
    ContentPackageManager contentPackages,
    PackageManager packages,
    AssetCatalogIndexer catalogIndexer
) : IStartupService
{
    public int Order => StartupOrder.EngineStartup;

    public void Startup()
    {
        packages.RegisterMountPoint(PackageNames.Game, "Content");
        packages.RegisterMountPoint(PackageNames.Engine, "Content/Engine");

        game.Initializing += OnGameInitializing;
    }

    private void OnGameInitializing(object? sender, EventArgs args)
    {
        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), env.ApplicationName);
        string cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), env.ApplicationName, "Cache");
        string temp = Path.Combine(Path.GetTempPath(), env.ApplicationName);
        var physical = new PhysicalFileSystem();

        vfs.Mount("/memory", new MemoryFileSystem());
        vfs.Mount("/user", physical.CreateSubFileSystem(appData));
        vfs.Mount("/cache", physical.CreateSubFileSystem(cache));
        vfs.Mount("/temp", physical.CreateSubFileSystem(temp));

        contentPackages.MountPackages();
        catalogIndexer.Refresh();
    }
}