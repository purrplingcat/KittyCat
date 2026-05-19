using PurrplingCore.Toolkit.Vfs.FileSystems;
using System.IO.Compression;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

public static class FileSystemEstensions
{
    public static PrefixedFileSystem WithPrefix(this IFileSystem fs, UPath prefix)
    {
        return new PrefixedFileSystem(fs, prefix);
    }
}

public static class VfsExtensions
{
    private static readonly PhysicalFileSystem _phys = new();

    public static void AddPhysicalLayer(this IVirtualFileSystemManager vfs, string folderName, string basePath)
    {
        var path = _phys.ConvertPathFromInternal(basePath);

        vfs.AddFileSystem(_phys.GetOrCreateSubFileSystem(path).WithPrefix(folderName));
    }

    public static void AddPhysicalLayer(this IVirtualFileSystemManager vfs, string basePath)
    {
        var path = _phys.ConvertPathFromInternal(basePath);

        vfs.AddFileSystem(_phys.GetOrCreateSubFileSystem(path));
    }

    public static void MountPhysical(this IVirtualFileSystemManager vfs, string mountPath, string physicalPath)
    {
        var path = _phys.ConvertPathFromInternal(physicalPath);

        vfs.Mount(mountPath, _phys.GetOrCreateSubFileSystem(path));
    }

    public static void MountPhysicalReadOnly(this IVirtualFileSystemManager vfs, string mountPath, string physicalPath)
    {
        var uPath = _phys.ConvertPathFromInternal(physicalPath);
        var subFs = _phys.GetOrCreateSubFileSystem(uPath);
        vfs.Mount(mountPath, new ReadOnlyFileSystem(subFs));
    }

    public static void MountMemory(this IVirtualFileSystemManager vfs, string mountPath)
    {
        vfs.Mount(mountPath, new MemoryFileSystem());
    }

    public static void MountZip(this IVirtualFileSystemManager vfs, string target, string zipPath)
    {
        var uPath = _phys.ConvertPathFromInternal(zipPath);
        var stream = _phys.OpenFile(uPath, FileMode.Open, FileAccess.Read);
        vfs.Mount(target, new ZipArchiveFileSystem(stream, ZipArchiveMode.Read));
    }

    public static void AddZipLayer(this IVirtualFileSystemManager vfs, string zipPath)
    {
        var uPath = _phys.ConvertPathFromInternal(zipPath);
        var stream = _phys.OpenFile(uPath, FileMode.Open, FileAccess.Read);
        vfs.AddFileSystem(new ZipArchiveFileSystem(stream, ZipArchiveMode.Read));
    }
}
