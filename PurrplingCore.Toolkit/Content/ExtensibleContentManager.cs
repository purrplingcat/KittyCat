
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit.Vfs;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Content;

public class ExtensibleContentManager(
    IServiceProvider serviceProvider,
    string rootDirectory,
    IVirtualFileSystem? fileSystem = null
) : ContentManager(serviceProvider, rootDirectory)
{
    private readonly ContentLoaderOptions _options = serviceProvider.GetService<IOptions<ContentLoaderOptions>>()?.Value
                   ?? new ContentLoaderOptions();
    private readonly Dictionary<Type, IContentLoader> _loaders = [];
    private readonly IVirtualFileSystem? _fileSystem = fileSystem ?? serviceProvider.GetService<IVirtualFileSystem>();

    protected bool TryGetLoader(string extension, [MaybeNullWhen(false)] out IContentLoader loader)
    {
        if (!string.IsNullOrEmpty(extension) && _options.ExtensionMappings.TryGetValue(extension, out var loaderType))
        {
            if (!_loaders.TryGetValue(loaderType, out loader))
            {
                loader = (IContentLoader)ActivatorUtilities.GetServiceOrCreateInstance(ServiceProvider, loaderType);
                _loaders[loaderType] = loader;
            }

            return true;
        }

        loader = null;
        return false;
    }

    public override T Load<T>(string assetName)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetName, nameof(assetName));

        if (LoadedAssets.TryGetValue(assetName, out var cachedAsset) && cachedAsset is T result)
        {
            return result;
        }

        var extension = Path.GetExtension(assetName);
        
        if (TryGetLoader(extension, out var loader))
        {
            var loadedAsset = loader.Load<T>(this, assetName);
            LoadedAssets[assetName] = loadedAsset;

            return loadedAsset;
        }

        return base.Load<T>(assetName);
    }

    protected override Stream OpenStream(string assetName)
    {
        if (_fileSystem != null)
        {
            var path = Path.Combine(RootDirectory, assetName);
            if (!Path.HasExtension(assetName))
            {
                Path.ChangeExtension(path, ".xnb");
            }

            _fileSystem.OpenRead(path);
        }

        return base.OpenStream(assetName);
    }
}


