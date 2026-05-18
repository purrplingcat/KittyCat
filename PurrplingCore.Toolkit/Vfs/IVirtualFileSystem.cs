using PurrplingCore.Toolkit.Hosting;
using Zio;

namespace PurrplingCore.Toolkit.Vfs;

public record FileSystemLayer(IFileSystem FileSystem, int Order);

public interface IVirtualFileSystem
{
    Stream Open(string path, FileMode mode, FileAccess access);
    Stream OpenRead(string path);
    Stream OpenWrite(string path);
    bool FileExists(string path);
    bool DirectoryExists(string path);
}
