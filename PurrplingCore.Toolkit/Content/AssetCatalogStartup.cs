using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;

namespace PurrplingCore.Toolkit.Content;

/// <summary>
/// Finds asset catalog artifacts in a content namespace and imports their metadata into the
/// <see cref="AssetRegistry"/>.
/// </summary>
/// <remarks>
/// The indexer depends only on <see cref="IFileProvider"/>, not on VFS. It supports one root
/// catalog plus uniquely named package catalogs under <see cref="AssetCatalog.DirectoryName"/>.
/// Already imported paths are skipped, and all scans are serialized so lazy refreshes are safe.
/// </remarks>
public sealed partial class AssetCatalogIndexer
{
    private readonly AssetRegistry _registry;
    private readonly ILogger<AssetCatalogIndexer> _logger;
    private readonly IFileProvider? _contentFiles;
    private readonly string _binaryCatalogPath;
    private readonly string _jsonCatalogPath;
    private readonly string _catalogDirectoryPath;
    private readonly HashSet<string> _indexedPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncRoot = new();

    public AssetCatalogIndexer(
        AssetRegistry registry,
        ILogger<AssetCatalogIndexer> logger,
        IOptions<ContentManagerOptions> contentOptions,
        IFileProvider? contentFiles = null)
    {
        ArgumentNullException.ThrowIfNull(contentOptions);

        _registry = registry;
        _logger = logger;
        _contentFiles = contentFiles;

        var contentRoot = contentOptions.Value.ContentRoot.Replace('\\', '/').Trim('/');
        _binaryCatalogPath = $"{contentRoot}/{AssetCatalog.BinaryFileName}";
        _jsonCatalogPath = $"{contentRoot}/{AssetCatalog.JsonFileName}";
        _catalogDirectoryPath = $"{contentRoot}/{AssetCatalog.DirectoryName}";
    }

    /// <summary>
    /// Scans one content provider for the root catalog and all package-specific catalogs.
    /// </summary>
    public void Index(IFileProvider source)
    {
        ArgumentNullException.ThrowIfNull(source);

        lock (_syncRoot)
        {
            IndexCore(source);
        }
    }

    private void IndexCore(IFileProvider source)
    {
        var sourceType = source.GetType().Name;
        LogIndexingSource(_logger, sourceType);

        var catalogFound = TryImportCatalog(source, sourceType);
        catalogFound |= ImportPackageCatalogs(source, sourceType);

        if (!catalogFound)
        {
            LogCatalogNotFound(
                _logger,
                sourceType,
                _binaryCatalogPath,
                _jsonCatalogPath);
        }
    }

    /// <summary>
    /// Re-scans the optional provider supplied through dependency injection.
    /// </summary>
    /// <remarks>
    /// This is also called after an asset lookup miss, allowing catalogs from a newly mounted
    /// package to be discovered without VFS events or a direct VFS dependency.
    /// </remarks>
    public void Refresh()
    {
        if (_contentFiles is not null)
        {
            Index(_contentFiles);
        }
    }

    /// <summary>
    /// Imports the root catalog through MonoGame's <see cref="TitleContainer"/>.
    /// </summary>
    /// <remarks>
    /// This is the no-VFS path used on platforms where content is packaged by the application
    /// and cannot be accessed as ordinary operating-system files.
    /// </remarks>
    public void IndexTitleCatalog()
    {
        lock (_syncRoot)
        {
            const string sourceType = "TitleContainer";
            LogIndexingTitleContainer(_logger);

            if (!TryImportTitleCatalog(sourceType))
            {
                LogTitleCatalogNotFound(
                    _logger,
                    _binaryCatalogPath,
                    _jsonCatalogPath);
            }
        }
    }

    private bool TryImportCatalog(IFileProvider source, string sourceType)
    {
        if (TryImportBinaryCatalog(TryOpenRead(source, _binaryCatalogPath), _binaryCatalogPath, sourceType))
        {
            return true;
        }

        return TryImportJsonCatalog(TryOpenRead(source, _jsonCatalogPath), _jsonCatalogPath, sourceType);
    }

    private bool ImportPackageCatalogs(IFileProvider source, string sourceType)
    {
        var directory = source.GetDirectoryContents(_catalogDirectoryPath);
        if (!directory.Exists)
        {
            return false;
        }

        var catalogFound = false;
        foreach (var file in directory)
        {
            if (!file.Exists || file.IsDirectory)
            {
                continue;
            }

            var path = $"{_catalogDirectoryPath}/{file.Name}";
            var extension = Path.GetExtension(file.Name);
            if (extension.Equals(".bin", StringComparison.OrdinalIgnoreCase))
            {
                catalogFound |= TryImportBinaryCatalog(file.CreateReadStream(), path, sourceType);
            }
            else if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                var binaryPath = Path.ChangeExtension(path, ".bin");
                if (source.GetFileInfo(binaryPath).Exists)
                {
                    continue;
                }

                catalogFound |= TryImportJsonCatalog(file.CreateReadStream(), path, sourceType);
            }
        }

        return catalogFound;
    }

    private bool TryImportTitleCatalog(string sourceType)
    {
        if (TryImportBinaryCatalog(TryOpenTitleCatalog(_binaryCatalogPath), _binaryCatalogPath, sourceType))
        {
            return true;
        }

        return TryImportJsonCatalog(TryOpenTitleCatalog(_jsonCatalogPath), _jsonCatalogPath, sourceType);
    }

    private bool TryImportBinaryCatalog(Stream? stream, string path, string sourceType)
    {
        if (stream is null)
        {
            return false;
        }

        using (stream)
        {
            if (_indexedPaths.Contains(path))
            {
                return true;
            }

            var catalog = AssetCatalogSerializer.Load(stream);
            Import(catalog);
            _indexedPaths.Add(path);
            LogCatalogIndexed(catalog, path, "binary", sourceType);
        }

        return true;
    }

    private bool TryImportJsonCatalog(Stream? stream, string path, string sourceType)
    {
        if (stream is null)
        {
            return false;
        }

        using (stream)
        {
            if (_indexedPaths.Contains(path))
            {
                return true;
            }

            var catalog = AssetCatalogSerializer.LoadJson(stream);
            Import(catalog);
            _indexedPaths.Add(path);
            LogCatalogIndexed(catalog, path, "JSON", sourceType);
        }

        return true;
    }

    private void Import(AssetCatalog catalog) => catalog.Import(_registry, catalog.PackageId);

    private void LogCatalogIndexed(AssetCatalog catalog, string path, string format, string sourceType)
    {
        LogCatalogIndexed(
            _logger,
            format,
            path,
            sourceType,
            catalog.PackageId);
    }

    [LoggerMessage(1, LogLevel.Trace, "Indexing asset catalog source {SourceType}.")]
    private static partial void LogIndexingSource(ILogger logger, string sourceType);

    [LoggerMessage(2, LogLevel.Debug,
        "No asset catalog found in source {SourceType}; checked {BinaryCatalogPath} and {JsonCatalogPath}.")]
    private static partial void LogCatalogNotFound(
        ILogger logger,
        string sourceType,
        string binaryCatalogPath,
        string jsonCatalogPath);

    [LoggerMessage(3, LogLevel.Trace, "Indexing the title asset catalog.")]
    private static partial void LogIndexingTitleContainer(ILogger logger);

    [LoggerMessage(5, LogLevel.Debug,
        "No title asset catalog found; checked {BinaryCatalogPath} and {JsonCatalogPath}.")]
    private static partial void LogTitleCatalogNotFound(
        ILogger logger,
        string binaryCatalogPath,
        string jsonCatalogPath);

    [LoggerMessage(6, LogLevel.Debug,
        "Indexed {Format} asset catalog {CatalogPath} from {SourceType} for package {PackageId}.")]
    private static partial void LogCatalogIndexed(
        ILogger logger,
        string format,
        string catalogPath,
        string sourceType,
        string packageId);

    private static Stream? TryOpenRead(IFileProvider source, string path)
    {
        var file = source.GetFileInfo(path);
        return file.Exists ? file.CreateReadStream() : null;
    }

    private static Stream? TryOpenTitleCatalog(string path)
    {
        try
        {
            return TitleContainer.OpenStream(path);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}

/// <summary>
/// Refreshes provider-backed catalogs after initialization-time filesystem mounts are complete.
/// </summary>
internal sealed class FileProviderAssetCatalogStartup(
    AssetCatalogIndexer indexer,
    IGameApplicationLifetime lifetime) : IStartupService
{
    public int Order => 0;

    public void Startup()
    {
        // GameInitialized is deterministic here: every GameInitializing mount callback has finished.
        lifetime.GameInitialized.Register(indexer.Refresh);
    }
}
