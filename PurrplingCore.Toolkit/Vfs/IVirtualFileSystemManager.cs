using Zio;

namespace PurrplingCore.Toolkit.Vfs;

public interface IVirtualFileSystemManager
{
    IFileSystem Root { get; }
    string BaseDirectory { get; }

    IFileSystem CreateSubFileSystem(params string[] paths);
    void Mount(string target, IFileSystem fs);
}
