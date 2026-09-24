using PurrplingCore.Toolkit.Hosting;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

internal class VirtualFileSystemManager : IVirtualFileSystemManager, IDisposable
{
    internal static PhysicalFileSystem Physical { get; } = new PhysicalFileSystem();

    private readonly IFileSystem _baseFs;
    private readonly AggregateFileSystem _rootContainers = new();
    private readonly MountFileSystem _root;
    private readonly object _syncRoot = new();
    private bool _disposed;

    public event EventHandler<VfsMountEventArgs>? Mounted;
    public event EventHandler<VfsRootContainerEventArgs>? RootContainerAdded;

    public VirtualFileSystemManager(IHostEnvironment env)
    {
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

        Mounted?.Invoke(this, new VfsMountEventArgs(absoluteTarget, fileSystem));
    }

    public void AddRootContainer(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        lock (_syncRoot)
        {
            _rootContainers.AddFileSystem(fileSystem);
        }

        RootContainerAdded?.Invoke(this, new VfsRootContainerEventArgs(fileSystem));
    }

    public IEnumerable<IFileSystem> GetRootContainers()
    {
        // AggregateFileSystem exposes its fallback separately from added layers.
        // The base game filesystem is that fallback and also contains a catalog.
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
