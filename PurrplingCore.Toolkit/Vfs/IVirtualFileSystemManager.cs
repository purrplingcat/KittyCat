using Zio;

namespace PurrplingCore.Toolkit.Vfs;

/// <summary>
/// Manages the virtual file system and its mount points.
/// </summary>
public interface IVirtualFileSystemManager
{
    IFileSystem GetFileSystem();
    void Mount(UPath target, IFileSystem fileSystem);
    void AddRootContainer(IFileSystem fileSystem);
    IEnumerable<IFileSystem> GetRootContainers();

    event EventHandler<VfsMountEventArgs>? Mounted;
    event EventHandler<VfsRootContainerEventArgs>? RootContainerAdded;
}

public sealed class VfsMountEventArgs(UPath target, IFileSystem fileSystem) : EventArgs
{
    public UPath Target { get; } = target;
    public IFileSystem FileSystem { get; } = fileSystem;
}

public sealed class VfsRootContainerEventArgs(IFileSystem fileSystem) : EventArgs
{
    public IFileSystem FileSystem { get; } = fileSystem;
}
