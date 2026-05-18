using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

public interface IVirtualFileSystemManager
{
    IFileSystem Root { get; }
    void Mount(string target, IFileSystem fs);
    void AddShadow(string target, IFileSystem fs);
    void AddShadow(IFileSystem fs);
}
