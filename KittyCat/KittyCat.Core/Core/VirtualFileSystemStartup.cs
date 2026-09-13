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
    private readonly IVfsMountManager _mounts;
    private readonly ContentManagerOptions _contentOptions;
    private readonly ILogger _logger;

    public int Order => -100;

    public VirtualFileSystemStartup(
        IVirtualFileSystemManager vfs,
        IVfsMountManager mounts,
        IHostEnvironment env,
        IOptions<ContentManagerOptions> contentOptions,
        ILoggerFactory loggerFactory
    )
    {
        _vfs = vfs;
        _mounts = mounts;
        _env = env;
        _contentOptions = contentOptions.Value;
        _logger = loggerFactory.CreateLogger("VFS");
    }

    public void OnStartup()
    {
        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _env.ApplicationName);
        string temp = Path.Combine(Path.GetTempPath(), _env.ApplicationName);

        var watcher = _vfs.GetFileSystem().Watch(UPath.Root);

        watcher.EnableRaisingEvents = true;
        watcher.Changed += (s, e) =>
        {
            _logger.LogInformation("File system changed: {ChangeType} {Path}", e.ChangeType, e.FullPath);
        };

        _mounts.Mount("/memory", new MemoryFileSystem());
        _mounts.Mount("/data", _vfs.Physical.CreateSubFileSystem(appData));
        _mounts.Mount("/tmp", _vfs.Physical.CreateSubFileSystem(temp));

        var contentRoot = new UPath(_contentOptions.ContentRoot).ToAbsolute();
        var physicalContent = new SubFileSystem(_vfs.Base, contentRoot);
        _mounts.AddLayer(contentRoot, physicalContent, int.MinValue);

        var paksDirectory = Path.Combine(_env.BaseDirectory, _contentOptions.ContentRoot, "Paks");
        if (Directory.Exists(paksDirectory))
        {
            AddContentPaks(
                contentRoot,
                Directory.EnumerateFiles(paksDirectory, "*", SearchOption.TopDirectoryOnly)
            );
        }

        _logger.LogVfsStructure(_vfs.GetFileSystem());
    } 

    private void AddContentPaks(UPath contentRoot, IEnumerable<string> enumerable)
    {
        int order = 0;
        foreach(var path in enumerable.Order(new PakPathComparer()))
        {
            IFileSystem? contentLayer = null;
            switch (Path.GetExtension(path).ToLower())
            {
                case ".pak":
                    var pakFs = new PakFileSystem(path);
                    contentLayer = CreateContentLayer(pakFs, contentRoot);
                    break;
                case ".zip":
                    var zipFs = new ZipArchiveFileSystem(path);
                    contentLayer = CreateContentLayer(zipFs, contentRoot);
                    break;
                default:
                    continue;
            }

            _mounts.AddLayer(contentRoot, contentLayer, order++);
            _logger.LogDebug("Mount: {Path}", Path.GetRelativePath(_env.BaseDirectory, path));
        }
    }

    private static IFileSystem CreateContentLayer(IFileSystem archive, UPath contentRoot)
    {
        return archive.DirectoryExists(contentRoot)
            ? new SubFileSystem(archive, contentRoot)
            : archive;
    }
}


