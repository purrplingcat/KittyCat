using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using PurrplingCore.Toolkit.Vfs;
using System.Collections;
using Zio;

namespace PurrplingCore.Toolkit.Hosting;

public class VirtualFileProvider(IHostEnvironment env) : IFileProvider
{
    private readonly IFileSystem _fileSystem = VirtualFileSystemManager.CreatePlatformFileSystem(env);

    public IFileInfo GetFileInfo(string subpath)
    {
        return new VirtualFileInfo(NormalizePath(subpath), _fileSystem);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return new DirectoryContents(NormalizePath(subpath), _fileSystem);
    }

    public IChangeToken Watch(string filter)
    {
        return NullChangeToken.Singleton;
    }

    private UPath NormalizePath(UPath subpath)
    {
        if (!subpath.IsAbsolute)
        {
            return UPath.Combine(_fileSystem.ConvertPathFromInternal(env.BaseDirectory), subpath);
        }

        return _fileSystem.ConvertPathFromInternal(subpath.FullName);
    }
}

internal readonly struct DirectoryContents(UPath path, IFileSystem fs) : IDirectoryContents
{
    public bool Exists => fs.DirectoryExists(path);

    public IEnumerator<IFileInfo> GetEnumerator()
    {
        foreach (var subpath in fs.EnumeratePaths(path))
        {
            yield return new VirtualFileInfo(subpath, fs);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

internal readonly struct VirtualFileInfo(UPath path, IFileSystem fs) : IFileInfo
{
    public bool Exists => fs.FileExists(path);

    public long Length => fs.GetFileLength(path);
    public string? PhysicalPath => null;
    public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

    public string Name => path.GetName();

    public bool IsDirectory => fs.DirectoryExists(path);

    public Stream CreateReadStream()
    {
        return fs.OpenRead(path);
    }
}
