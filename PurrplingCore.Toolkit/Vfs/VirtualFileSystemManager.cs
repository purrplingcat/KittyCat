using PurrplingCore.Toolkit.Hosting;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

/// <summary>
/// Maintains a stable mount root over an aggregate of the platform base filesystem and packages.
/// </summary>
internal class VirtualFileSystemManager : IVirtualFileSystemManager, IDisposable
{
    internal static PhysicalFileSystem Physical { get; } = new PhysicalFileSystem();

    private readonly IFileSystem _baseFs;
    private readonly AggregateFileSystem _rootContainers = new();
    private readonly MountFileSystem _root;
    private readonly object _syncRoot = new();
    private bool _disposed;

    public VirtualFileSystemManager(IHostEnvironment env)
    {
        // Consumers keep this stable root while aggregate layers are added underneath it.
        _baseFs = CreatePlatformFileSystem(env);
        _rootContainers = new AggregateFileSystem(_baseFs);
        _root = new MountFileSystem(_rootContainers);
    }

    public IFileSystem GetFileSystem()
    {
        return _root;
    }

    public void Mount(UPath target, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        var absoluteTarget = NormalizeTarget(target);

        lock (_syncRoot)
        {
            _root.Mount(absoluteTarget, fileSystem);
        }
    }

    public void AddRootContainer(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        lock (_syncRoot)
        {
            _rootContainers.AddFileSystem(fileSystem);
        }
    }

    public IEnumerable<IFileSystem> GetRootContainers()
    {
        yield return _baseFs;

        foreach (var container in _rootContainers.GetFileSystems())
        {
            yield return container;
        }
    }

    internal static IFileSystem CreatePlatformFileSystem(IHostEnvironment env)
    {
        return env.PlatformType switch
        {
            PlatformType.Desktop => Physical.CreateSubFileSystem(env.BaseDirectory),
            // Bundled mobile content is added later as TitleContainer-backed package streams.
            PlatformType.Mobile => new MemoryFileSystem(),
            _ => throw new NotSupportedException($"Platform {env.PlatformType} is not supported"),
        };
    }

    private static UPath NormalizeTarget(UPath target)
    {
        if (target.IsNull || target == UPath.Root)
        {
            throw new ArgumentException("A mount target must be a non-root path.", nameof(target));
        }

        return target.ToAbsolute();
    }

    #region Disposing
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _root.Dispose();
                _rootContainers.Dispose();
                _baseFs.Dispose();
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
