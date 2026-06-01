using System.Diagnostics.CodeAnalysis;
using Zio;

namespace PurrplingCore.Toolkit.Vfs;

/// <summary>
/// Manages the virtual file system (VFS), combining global layered file systems 
/// and target-specific exclusive mount points.
/// </summary>
public interface IVirtualFileSystemManager
{
    IFileSystem Physical { get; }
    IFileSystem Base { get; }

    T? FindFileSystem<T>() where T : IFileSystem;
    IFileSystem? FindFileSystem(string name);
    IFileSystem GetFileSystem();
    void SetFileSystem(IFileSystem fileSystem);
}
