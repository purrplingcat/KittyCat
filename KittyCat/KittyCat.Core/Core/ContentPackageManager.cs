using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Pak;
using PurrplingCore.Toolkit.Vfs;
using PurrplingCore.Toolkit.Vfs.Comparers;
using PurrplingCore.Toolkit.Vfs.FileSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using Zio;
using Zio.FileSystems;

namespace KittyCat.Core;

/// <summary>
/// Discovers application-bundled content packages and adds them as read-only VFS root layers.
/// </summary>
/// <remarks>
/// The platform supplies a keyed <see cref="IFileProvider"/> capable of enumerating its packaged
/// content. Packages are sorted by <see cref="PakPathComparer"/> before mounting, so adding a PAK
/// file does not require updating a separate manifest.
/// Packages are mounted during <see cref="IGameApplicationLifetime.GameInitializing"/>. Dynamic
/// callers may use <see cref="AddPackage"/> later; asset catalogs are discovered independently.
/// </remarks>
public sealed class ContentPackageManager : IStartupService
{
    public const string FileProviderKey = "ContentPackages";
    private const string PackagesPath = "Content/Paks";

    private readonly IGameApplicationLifetime _lifetime;
    private readonly IFileProvider _packageFiles;
    private readonly IVirtualFileSystemManager _vfs;
    private readonly ILogger _logger;

    public ContentPackageManager(
        IGameApplicationLifetime lifetime,
        [FromKeyedServices(FileProviderKey)] IFileProvider packageFiles,
        IVirtualFileSystemManager vfs,
        ILoggerFactory loggerFactory)
    {
        _lifetime = lifetime;
        _packageFiles = packageFiles;
        _vfs = vfs;
        _logger = loggerFactory.CreateLogger<ContentPackageManager>();
    }

    public int Order => -90;

    public void Startup()
    {
        // Package mounting belongs to game initialization, not host service registration.
        _lifetime.GameInitializing.Register(Initialize);
    }

    private void Initialize()
    {
        var packages = _packageFiles.GetDirectoryContents(PackagesPath);
        if (!packages.Exists)
        {
            _logger.LogDebug("No bundled content package directory found at {Path}.", PackagesPath);
            return;
        }

        AddPackages(packages);
        _logger.LogVfsStructure(_vfs.GetFileSystem());
    }

    /// <summary>
    /// Adds an already opened package filesystem to the aggregate content namespace.
    /// </summary>
    public void AddPackage(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        _vfs.AddRootContainer(fileSystem);
    }

    private void AddPackages(IEnumerable<IFileInfo> files)
    {
        foreach (var file in files
            .Where(IsPackage)
            .OrderBy(file => file.Name, new PakPathComparer()))
        {
            var fileSystem = OpenPackage(file);
            AddPackage(fileSystem);
            _logger.LogDebug("Mounted bundled content package: {Path}/{Name}", PackagesPath, file.Name);
        }
    }

    private static bool IsPackage(IFileInfo file) =>
        file.Exists && !file.IsDirectory &&
        (file.Name.EndsWith(".pak", StringComparison.OrdinalIgnoreCase) ||
         file.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

    private static IFileSystem OpenPackage(IFileInfo file)
    {
        var stream = file.CreateReadStream();
        try
        {
            // The created filesystem owns the stream for its entire mounted lifetime.
            IFileSystem fileSystem = file.Name.EndsWith(".pak", StringComparison.OrdinalIgnoreCase)
                ? new PakFileSystem(new PakArchive(stream))
                : new ZipArchiveFileSystem(stream);

            stream = null;
            return fileSystem;
        }
        finally
        {
            stream?.Dispose();
        }
    }
}