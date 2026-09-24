using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Vfs;
using Zio;

namespace PurrplingCore.Toolkit.Content;

/// <summary>
/// Imports catalogs from VFS root containers when VFS is registered.
/// </summary>
internal sealed class AssetCatalogStartup(
    AssetRegistry registry,
    IVirtualFileSystemManager vfs,
    IOptions<ContentManagerOptions> contentOptions) : IStartupService
{
    private readonly IVirtualFileSystemManager _vfs = vfs;
    private readonly AssetRegistry _registry = registry;
    private readonly UPath _contentRoot = new UPath(contentOptions.Value.ContentRoot).ToAbsolute();

    public int Order => -90;

    public void OnStartup()
    {
        _vfs.RootContainerAdded += OnRootContainerAdded;

        foreach (var container in _vfs.GetRootContainers())
        {
            ImportCatalog(container);
        }
    }

    private void OnRootContainerAdded(object? sender, VfsRootContainerEventArgs eventArgs)
    {
        ImportCatalog(eventArgs.FileSystem);
    }

    private void ImportCatalog(IFileSystem container)
    {
        var binaryPath = UPath.Combine(_contentRoot, AssetCatalog.BinaryFileName);
        if (container.FileExists(binaryPath))
        {
            using var stream = container.OpenRead(binaryPath);
            var catalog = AssetCatalogSerializer.Load(stream);
            catalog.Import(_registry, catalog.PackageId);
            return;
        }

        var jsonPath = UPath.Combine(_contentRoot, AssetCatalog.JsonFileName);
        if (container.FileExists(jsonPath))
        {
            using var stream = container.OpenRead(jsonPath);
            var catalog = AssetCatalogSerializer.LoadJson(stream);
            catalog.Import(_registry, catalog.PackageId);
        }
    }
}
