using Zio;

namespace PurrplingCore.Toolkit.Vfs;

/// <summary>
/// Manages the virtual file system and its mount points.
/// </summary>
public interface IVirtualFileSystemManager
{
    /// <summary>
    /// Gets the stable root filesystem seen by application consumers.
    /// </summary>
    IFileSystem GetFileSystem();

    /// <summary>
    /// Mounts a filesystem at one non-root virtual path.
    /// </summary>
    void Mount(UPath target, IFileSystem fileSystem);

    /// <summary>
    /// Adds a package or other read-only layer to the aggregate root namespace.
    /// </summary>
    void AddRootContainer(IFileSystem fileSystem);

    /// <summary>
    /// Enumerates the physical fallback followed by added root layers.
    /// </summary>
    IEnumerable<IFileSystem> GetRootContainers();
}
