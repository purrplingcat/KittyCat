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

    /// <summary>
    /// Enumerates items under the specified virtual <paramref name="path"/> and returns the corresponding
    /// physical file system paths for items that reside on a <see cref="PhysicalFileSystem"/> and actually
    /// exist on the host file system. The enumeration respects the provided <paramref name="option"/>
    /// (search depth) and <paramref name="searchTarget"/> (files, directories, or both). Only non-empty
    /// physical paths for which the underlying file system reports existence are yielded.
    /// </summary>
    /// <param name="fs">The virtual file system to search.</param>
    /// <param name="path">The virtual path to enumerate.</param>
    /// <param name="option">The search option that controls recursion depth.</param>
    /// <param name="searchTarget">Specifies whether to return files, directories, or both.</param>
    /// <returns>
    /// An <see cref="IEnumerable{String}"/> of absolute physical file system paths corresponding to the
    /// matching items in the virtual file system. The sequence may be empty if no matching physical paths are found.
    /// </returns>
    public static IEnumerable<string> GetPhysicalPaths(this IFileSystem fs, UPath path, SearchOption option, SearchTarget searchTarget)
    {
        foreach (var item in fs.EnumerateItems(path, option))
        {
            if (searchTarget == SearchTarget.Directory && !item.IsDirectory) continue;
            if (searchTarget == SearchTarget.File && item.IsDirectory) continue;

            if (item.FileSystem is PhysicalFileSystem physFs)
            {
                var physicalPath = physFs.ConvertPathToInternal(item.AbsolutePath);
                if (!string.IsNullOrEmpty(physicalPath) && Path.Exists(physicalPath))
                {
                    yield return physicalPath;
                }
            }
        }
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
