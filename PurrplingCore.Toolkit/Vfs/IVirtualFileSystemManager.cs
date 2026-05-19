using Zio;

namespace PurrplingCore.Toolkit.Vfs;

/// <summary>
/// Manages the virtual file system (VFS), combining global layered file systems 
/// and target-specific exclusive mount points.
/// </summary>
public interface IVirtualFileSystemManager
{
    /// <summary>
    /// Gets the main root file system that should be used by the engine to access all data.
    /// </summary>
    IFileSystem Root { get; }

    /// <summary>
    /// Mounts a file system exclusively to a specific virtual target path.
    /// Any layered content previously accessible at this path will be shadowed (hidden).
    /// </summary>
    /// <param name="target">The absolute virtual path where the file system should be mounted (e.g., "/User/Saves").</param>
    /// <param name="fs">The file system to mount.</param>
    void Mount(string target, IFileSystem fs);

    /// <summary>
    /// Unmounts an exclusive file system from the specified virtual target path, 
    /// revealing any shadowed layered content underneath.
    /// </summary>
    /// <param name="target">The absolute virtual path to unmount.</param>
    void Unmount(string target);

    /// <summary>
    /// Adds a file system as a global fallback layer to the underlying aggregate file system.
    /// </summary>
    /// <param name="fs">The file system to add to the aggregate root.</param>
    void AddFileSystem(IFileSystem fs);

    /// <summary>
    /// Removes a previously added file system layer from the aggregate file system.
    /// </summary>
    /// <param name="fs">The exact file system instance to remove.</param>
    void RemoveFileSystem(IFileSystem fs);
}
