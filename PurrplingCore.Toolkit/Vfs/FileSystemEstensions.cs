using PurrplingCore.Toolkit.Vfs.FileSystems;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

public static class FileSystemEstensions
{
    public static PrefixedFileSystem WithPrefix(this IFileSystem fs, UPath prefix)
    {
        return new PrefixedFileSystem(fs, prefix);
    }

    public static IFileSystem? GetLowerLevel(this IFileSystem fs)
    {
        if (fs is ComposeFileSystem composeFs)
        {
            return composeFs.Fallback;
        }

        return null;
    }

    public static SubFileSystem CreateSubFileSystem(this IFileSystem fs, string path)
    {
        var safePath = fs.ConvertPathFromInternal(path);
        return fs.GetOrCreateSubFileSystem(safePath);
    }

    public static Stream OpenRead(this IFileSystem fs, UPath path)
    {
        return fs.OpenFile(path.ToAbsolute(), FileMode.Open, FileAccess.Read);
    }

    public static Stream OpenWrite(this IFileSystem fs, UPath path)
    {
        return fs.OpenFile(path.ToAbsolute(), FileMode.Create, FileAccess.Write);
    }
}
