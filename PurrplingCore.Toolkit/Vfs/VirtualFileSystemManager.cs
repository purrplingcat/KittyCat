using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

public partial class VirtualFileSystemManager
    : IVirtualFileSystemManager, IDisposable
{
    private readonly IFileSystem _physicalFs;
    private IFileSystem _topmostFileSystem;
    private bool _disposed;

    public IFileSystem Physical => _physicalFs;

    public VirtualFileSystemManager(IHostEnvironment env)
    {
        _physicalFs = CreatePlatformFileSystem(env);
        _topmostFileSystem = _physicalFs.CreateSubFileSystem(env.BaseDirectory);
    }

    public IFileSystem GetFileSystem()
    {
        return _topmostFileSystem;
    }

    private static IFileSystem CreatePlatformFileSystem(IHostEnvironment env)
    {
        return env.PlatformType switch
        {
            PlatformType.Desktop => new PhysicalFileSystem(),
            _ => throw new NotSupportedException($"Platform {env.PlatformType} is not supported"),
        };
    }

    internal static IFileSystem CreateBaseFileSystem(IHostEnvironment env)
    {
        var platform = CreatePlatformFileSystem(env);
        return platform.CreateSubFileSystem(env.BaseDirectory);
    }

    public void SetFileSystem(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem, nameof(fileSystem));
        _topmostFileSystem = fileSystem;
    }

    public T? FindFileSystem<T>() where T : IFileSystem
    {
        var current = _topmostFileSystem;
        while (current != null)
        {
            if (current is T matched)
            {
                return matched;
            }
            current = current.GetLowerLevel();
        }

        return default;
    }

    public IFileSystem? FindFileSystem(string name)
    {
        var current = _topmostFileSystem;
        while (current != null)
        {
            if (current is FileSystem fs && fs.Name == name)
            {
                return fs;
            }
            if (current.GetType().Name == name)
            {
                return current;
            }
            current = current.GetLowerLevel();
        }

        return default;
    }

    #region Disposing
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _topmostFileSystem.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion

}

public static class VirtualFileSystemManagerExtensions
{
    public static IFileSystem Chain(this IVirtualFileSystemManager vfs, Func<IFileSystem, IFileSystem> factory)
    {
        var chainedFs = factory.Invoke(vfs.GetFileSystem());
        
        vfs.SetFileSystem(chainedFs);
        return chainedFs;
    }

    public static T GetRequiredFileSystem<T>(this IVirtualFileSystemManager vfs) where T : IFileSystem
    {
        return vfs.FindFileSystem<T>() 
            ?? throw new InvalidCastException($"FileSystem '{typeof(T)}' not found in chain.");
    }

    public static bool TryGetFileSystem<T>(this IVirtualFileSystemManager vfs, [MaybeNullWhen(false)] out T result) 
        where T : IFileSystem
    {
        result = vfs.FindFileSystem<T>();
        return result != null;
    }

    public static IFileSystem GetBaseFileSystem(this IVirtualFileSystemManager vfs)
    {
        var fs = vfs.GetFileSystem();

        while (fs.GetLowerLevel() is IFileSystem lowerFs)
        {
            fs = lowerFs;
        }

        Debug.Assert(fs != null, "Base file system cannot be null");
        return fs;
    }
}

public interface IFileSystemBootstrap
{
    void Initialize(IVirtualFileSystemManager vfs);
}

public class DefaultFileSystemBootstrap(IHostEnvironment env) : IFileSystemBootstrap
{
    public void Initialize(IVirtualFileSystemManager vfs)
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), env.ApplicationName);
        var temp = Path.Combine(Path.GetTempPath(), env.ApplicationName);

        var mountFs = vfs.FindFileSystem<MountFileSystem>();
        if (mountFs != null)
        {
            mountFs.Mount("/Memory", new MemoryFileSystem());
            mountFs.Mount("/User", vfs.Physical.CreateSubFileSystem(appData));
            mountFs.Mount("/Cache", vfs.Physical.CreateSubFileSystem(temp));
        }

        var aggregateFs = vfs.FindFileSystem<AggregateFileSystem>();
        if (aggregateFs != null)
        {
            // Příklad: aggregateFs.AddFileSystem(new ZipFileSystem("Content/BaseGame.pak"));
        }
    }
}

internal class VirtualFileSystemStartup : IStartupService
{
    private readonly IEnumerable<IFileSystemBootstrap> _bootstraps;
    private readonly IVirtualFileSystemManager _vfs;
    private readonly ILogger _logger;

    public int Order => -600;

    public VirtualFileSystemStartup(
        IVirtualFileSystemManager vfs,
        IEnumerable<IFileSystemBootstrap> bootstraps,
        ILoggerFactory loggerFactory
    )
    {
        _vfs = vfs;
        _bootstraps = bootstraps;
        _logger = loggerFactory.CreateLogger(nameof(VirtualFileSystemStartup));
    }

    public void OnStartup()
    {
        foreach (var bootstrap in _bootstraps)
        {
            bootstrap.Initialize(_vfs);
        }

        _logger.LogVfsStructure(_vfs.GetFileSystem());
    }
}
