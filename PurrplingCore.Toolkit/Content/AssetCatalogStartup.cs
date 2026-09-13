using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Vfs;
using Zio;

namespace PurrplingCore.Toolkit.Content;

public static class AssetCatalogServiceExtensions
{
    /// <summary>
    /// Imports asset catalogs from VFS content layers.
    /// Adds the VFS and asset-system services required by catalog import.
    /// </summary>
    public static IServiceCollection AddVfsAssetCatalogs(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddVfs();
        services.AddAssetSystem();
        services.AddStartup<AssetCatalogStartup>();
        return services;
    }
}

/// <summary>
/// Imports catalogs from content overlay layers after the VFS startup has mounted them.
/// Registered explicitly by <see cref="AssetCatalogServiceExtensions.AddVfsAssetCatalogs"/>.
/// </summary>
internal sealed class AssetCatalogStartup(
    IVfsMountManager mounts,
    AssetRegistry registry,
    IOptions<ContentManagerOptions> contentOptions) : IStartupService
{
    private readonly IVfsMountManager _mounts = mounts;
    private readonly AssetRegistry _registry = registry;
    private readonly UPath _contentRoot = new UPath(contentOptions.Value.ContentRoot).ToAbsolute();
    private readonly HashSet<IFileSystem> _importedLayers = new(ReferenceEqualityComparer.Instance);
    private readonly object _syncRoot = new();

    public int Order => -90;

    public void OnStartup()
    {
        _mounts.Changed += OnVfsChanged;
        ImportNewLayers();
    }

    private void OnVfsChanged(object? sender, VfsChangedEventArgs eventArgs)
    {
        if (eventArgs.Kind == VfsChangeKind.LayerAdded && eventArgs.Target == _contentRoot)
        {
            ImportNewLayers();
        }
    }

    private void ImportNewLayers()
    {
        foreach (var layer in _mounts.GetLayers(_contentRoot))
        {
            lock (_syncRoot)
            {
                if (!_importedLayers.Add(layer))
                {
                    continue;
                }
            }

            try
            {
                ImportCatalog(layer);
            }
            catch
            {
                lock (_syncRoot)
                {
                    _importedLayers.Remove(layer);
                }

                throw;
            }
        }
    }

    private void ImportCatalog(IFileSystem layer)
    {
        var binaryPath = new UPath(AssetCatalog.BinaryFileName).ToAbsolute();
        if (layer.FileExists(binaryPath))
        {
            var catalog = AssetCatalogSerializer.Load(layer, binaryPath);
            catalog.Import(_registry, catalog.PackageId);
            return;
        }

        var jsonPath = new UPath(AssetCatalog.JsonFileName).ToAbsolute();
        if (layer.FileExists(jsonPath))
        {
            var catalog = AssetCatalogSerializer.LoadJson(layer, jsonPath);
            catalog.Import(_registry, catalog.PackageId);
        }
    }
}