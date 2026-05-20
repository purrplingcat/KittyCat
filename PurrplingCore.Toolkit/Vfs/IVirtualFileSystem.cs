using PurrplingCore.Toolkit.Hosting;
using Zio;

namespace PurrplingCore.Toolkit.Vfs;

public record FileSystemLayer(IFileSystem FileSystem, int Order);
