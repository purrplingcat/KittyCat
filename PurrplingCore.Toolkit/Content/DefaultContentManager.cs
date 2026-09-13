
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit.Vfs;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Content;

public class DefaultContentManager(
    IServiceProvider serviceProvider,
    string rootDirectory,
    ILogger logger,
    IFileSystem? fileSystem = null
) : ContentManager(serviceProvider, rootDirectory)
{
    private readonly IFileSystem? _fileSystem = fileSystem ?? serviceProvider.GetService<IFileSystem>();
    private readonly ILogger _logger = logger;

    public override T Load<T>(string assetName)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetName, nameof(assetName));

        if (LoadedAssets.TryGetValue(assetName, out var cachedAsset) && cachedAsset is T result)
        {
            return result;
        }

        _logger.LogDebug("Loading asset: {AssetName}", assetName);
        return base.Load<T>(assetName);
    }

    protected override Stream OpenStream(string assetName)
    {
        if (_fileSystem != null)
        {
            var path = Path.Combine(RootDirectory, assetName);
            if (!Path.HasExtension(assetName))
            {
                path = Path.ChangeExtension(path, ".xnb");
            }

            return _fileSystem.OpenRead(path);
        }

        return base.OpenStream(assetName);
    }
}
