using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Vfs;
using PurrplingCore.Toolkit.Vfs.Comparers;
using PurrplingCore.Toolkit.Vfs.FileSystems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zio;
using Zio.FileSystems;

namespace KittyCat.Core;

public class VirtualFileSystemStartup : IStartupService
{
    private readonly IHostEnvironment _env;
    private readonly IVirtualFileSystemManager _vfs;
    private readonly ContentManagerOptions _contentOptions;
    private readonly ILogger _logger;

    public int Order => -100;

    public VirtualFileSystemStartup(
        IVirtualFileSystemManager vfs,
        IHostEnvironment env,
        IOptions<ContentManagerOptions> contentOptions,
        ILoggerFactory loggerFactory
    )
    {
        _vfs = vfs;
        _env = env;
        _contentOptions = contentOptions.Value;
        _logger = loggerFactory.CreateLogger("VFS");
    }

    public void OnStartup()
    {
        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _env.ApplicationName);
        string cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _env.ApplicationName, "Cache");
        string temp = Path.Combine(Path.GetTempPath(), _env.ApplicationName);
        var physical = new PhysicalFileSystem();

        // Ephemeral files held only for the lifetime of the process.
        _vfs.Mount("/memory", new MemoryFileSystem());
        // Persistent per-user game data, such as saves and user settings.
        _vfs.Mount("/user", physical.CreateSubFileSystem(appData));
        // Rebuildable per-user cached data.
        _vfs.Mount("/cache", physical.CreateSubFileSystem(cache));
        // Temporary files that may be discarded by the operating system.
        _vfs.Mount("/temp", physical.CreateSubFileSystem(temp));

        var paksDirectory = Path.Combine(_env.BaseDirectory, _contentOptions.ContentRoot, "Paks");
        if (Directory.Exists(paksDirectory))
        {
            AddContentPaks(Directory.EnumerateFiles(paksDirectory, "*", SearchOption.TopDirectoryOnly));
        }

        _logger.LogVfsStructure(_vfs.GetFileSystem());
    } 

    private void AddContentPaks(IEnumerable<string> enumerable)
    {
        foreach(var path in enumerable.Order(new PakPathComparer()))
        {
            IFileSystem? contentLayer = null;
            switch (Path.GetExtension(path).ToLower())
            {
                case ".pak":
                    contentLayer = new PakFileSystem(path);
                    break;
                case ".zip":
                    contentLayer = new ZipArchiveFileSystem(path);
                    break;
                default:
                    continue;
            }

            _vfs.AddRootContainer(contentLayer);
            _logger.LogDebug("Mount: {Path}", Path.GetRelativePath(_env.BaseDirectory, path));
        }
    }
}


