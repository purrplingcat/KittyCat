using Microsoft.Extensions.Logging;
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
    private readonly ILogger _logger;

    public int Order => -100;

    protected IFileSystem Top => _vfs.GetFileSystem();

    public VirtualFileSystemStartup(
        IVirtualFileSystemManager vfs,
        IHostEnvironment env,
        ILoggerFactory loggerFactory
    )
    {
        _vfs = vfs;
        _env = env;
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

        var mountFs = _vfs.FindFileSystem<MountFileSystem>();
        if (mountFs != null)
        {
            mountFs.Mount("/memory", new MemoryFileSystem());
            mountFs.Mount("/data", _vfs.Physical.CreateSubFileSystem(appData));
            mountFs.Mount("/tmp", _vfs.Physical.CreateSubFileSystem(temp));
        }

        var aggregateFs = _vfs.FindFileSystem<AggregateFileSystem>();
        if (aggregateFs != null && Top.DirectoryExists("/Content/Paks"))
        {
            AddContentPaks(
                aggregateFs, 
                Top.GetPhysicalPaths("/Content/Paks", SearchOption.TopDirectoryOnly, SearchTarget.File)
            );
        }

        _logger.LogVfsStructure(_vfs.GetFileSystem());
    } 

    private void AddContentPaks(AggregateFileSystem aggregateFs, IEnumerable<string> enumerable)
    {
        foreach(var path in enumerable.Order(new PakPathComparer()))
        {
            switch (Path.GetExtension(path).ToLower())
            {
                case ".pak":
                    var pakFs = new PakFileSystem(path);
                    aggregateFs.AddFileSystem(pakFs);
                    break;
                case ".zip":
                    aggregateFs.AddFileSystem(new ZipArchiveFileSystem(path));
                    break;
                default:
                    continue;
            }
            _logger.LogDebug("Mount: {Path}", Path.GetRelativePath(_env.BaseDirectory, path));
        }
    }
}


