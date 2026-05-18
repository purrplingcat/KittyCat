using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

public interface IVirtualFileSystemManager
{
    IFileSystem Root { get; }
    void Mount(string target, IFileSystem fs);
    void AddContentLayer(string target, IFileSystem fs);
    void AddContentLayer(IFileSystem fs);
}
