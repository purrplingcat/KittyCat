using PurrplingCore.Toolkit.Pak;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs.FileSystems;

public class PakFileSystem : FileSystem
{
    private readonly PakArchive _pak;
    private readonly string[] _sortedPaths;

    public PakFileSystem(PakArchive pak)
    {
        _pak = pak ?? throw new ArgumentNullException(nameof(pak));

        _sortedPaths = [.. 
            pak.GetAllEntries()
                   .Select(e => e.Path)
        ];
        Array.Sort(_sortedPaths, StringComparer.OrdinalIgnoreCase);
    }

    protected override bool FileExistsImpl(UPath path)
    {
        return _pak.FileExists(ConvertPathToInternal(path));
    }

    protected override Stream OpenFileImpl(UPath path, FileMode mode, FileAccess access, FileShare share)
    {        
        if (access != FileAccess.Read)
        {
            throw new UnauthorizedAccessException("PCAT archive is read-only.");
        }

        if (mode != FileMode.Open)
        {
            throw new NotSupportedException("Cannot create new files in a PCAT archive.");
        }

        return _pak.OpenFile(ConvertPathToInternal(path));
    }

    protected override bool CanWatchImpl(UPath path)
    {
        return false;
    }

    protected override bool DirectoryExistsImpl(UPath path)
    {
        if (path == UPath.Root)
            return true;

        // Normalize: ensure single trailing slash
        var directory = path.FullName.TrimEnd('/') + '/';

        // Use binary search to find the first candidate
        int index = FindStartIndex(directory);
        if (index >= _sortedPaths.Length)
            return false;

        // Check only the candidate at the found index (sorted list guarantees locality)
        return _sortedPaths[index].StartsWith(directory, StringComparison.OrdinalIgnoreCase);
    }

    protected override void CreateDirectoryImpl(UPath path)
    {
        throw new NotSupportedException(SR.PakArchiveReadOnly);
    }

    protected override void MoveDirectoryImpl(UPath srcPath, UPath destPath)
    {
        throw new NotSupportedException(SR.PakArchiveReadOnly);
    }

    protected override void DeleteDirectoryImpl(UPath path, bool isRecursive)
    {
        throw new NotSupportedException(SR.PakArchiveReadOnly);
    }

    protected override void CopyFileImpl(UPath srcPath, UPath destPath, bool overwrite)
    {
        throw new NotSupportedException(SR.PakArchiveReadOnly);
    }

    protected override void ReplaceFileImpl(UPath srcPath, UPath destPath, UPath destBackupPath, bool ignoreMetadataErrors)
    {
        throw new NotSupportedException(SR.PakArchiveReadOnly);
    }

    protected override long GetFileLengthImpl(UPath path)
    {
        var entry = _pak.GetEntry(ConvertPathToInternal(path));
        return entry.UncompressedSize;
    }

    protected override void MoveFileImpl(UPath srcPath, UPath destPath)
    {
        throw new NotSupportedException(SR.PakArchiveReadOnly);
    }

    protected override void DeleteFileImpl(UPath path)
    {
        throw new NotSupportedException(SR.PakArchiveReadOnly);
    }

    protected override FileAttributes GetAttributesImpl(UPath path)
    {
        throw new NotImplementedException();
    }

    protected override void SetAttributesImpl(UPath path, FileAttributes attributes)
    {
        throw new NotSupportedException(SR.PakArchiveReadOnly);
    }

    protected override DateTime GetCreationTimeImpl(UPath path)
    {
        return _pak.PackedDate;
    }

    protected override void SetCreationTimeImpl(UPath path, DateTime time)
    {
        throw new NotSupportedException(SR.PakArchiveReadOnly);
    }

    protected override DateTime GetLastAccessTimeImpl(UPath path)
    {
        return _pak.PackedDate;
    }

    protected override void SetLastAccessTimeImpl(UPath path, DateTime time)
    {
        throw new NotSupportedException(SR.PakArchiveReadOnly);
    }

    protected override DateTime GetLastWriteTimeImpl(UPath path)
    {
        return _pak.PackedDate;
    }

    protected override void SetLastWriteTimeImpl(UPath path, DateTime time)
    {
        throw new NotSupportedException(SR.PakArchiveReadOnly);
    }

    protected override void CreateSymbolicLinkImpl(UPath path, UPath pathToTarget)
    {
        throw new NotSupportedException(SR.PakArchiveReadOnly);
    }

    protected override bool TryResolveLinkTargetImpl(UPath linkPath, out UPath resolvedPath)
    {
        throw new NotSupportedException(SR.PakArchiveReadOnly);
    }

    protected override IEnumerable<FileSystemItem> EnumerateItemsImpl(UPath path, SearchOption searchOption, SearchPredicate? searchPredicate)
    {
        foreach (var item in EnumerateCore(path))
        {
            var fsItem = item;
            if (searchPredicate == null || searchPredicate(ref fsItem))
            {
                yield return fsItem;
            }

            if (item.IsDirectory && searchOption == SearchOption.AllDirectories)
            {
                foreach (var subItem in EnumerateItemsImpl(item.Path, searchOption, searchPredicate))
                {
                    yield return subItem;
                }
            }
        }
    }

    protected override IEnumerable<UPath> EnumeratePathsImpl(UPath path, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
    {
        bool wantsFiles = searchTarget == SearchTarget.Both || searchTarget == SearchTarget.File;
        bool wantsDirs = searchTarget == SearchTarget.Both || searchTarget == SearchTarget.Directory;

        foreach (var item in EnumerateCore(path))
        {
            if ((item.IsDirectory && wantsDirs) || (!item.IsDirectory && wantsFiles))
            {
                yield return item.Path;
            }

            if (item.IsDirectory && searchOption == SearchOption.AllDirectories)
            {
                foreach (var subItemPath in EnumeratePathsImpl(item.Path, searchPattern, searchOption, searchTarget))
                {
                    yield return subItemPath;
                }
            }
        }
    }

    protected override IFileSystemWatcher WatchImpl(UPath path)
    {
        throw new NotSupportedException("PCAT archive does not support file system watching.");
    }

    protected override string ConvertPathToInternalImpl(UPath path)
    {
        return path.FullName.TrimStart('/'); // Ensure no leading slash for internal representation
    }

    protected override UPath ConvertPathFromInternalImpl(string innerPath)
    {
        var path = new UPath(innerPath);
        return path.ToAbsolute();
    }

    private int FindStartIndex(string prefix)
    {
        int index = Array.BinarySearch(_sortedPaths, prefix, StringComparer.OrdinalIgnoreCase);
        return index < 0 ? ~index : index;
    }

    private IEnumerable<FileSystemItem> EnumerateCore(UPath path)
    {
        string query = path == UPath.Root ? "/" : path.FullName + "/";
        int startIndex = path == UPath.Root ? 0 : FindStartIndex(query);
        string? lastYieldedDir = null;

        for (int i = startIndex; i < _sortedPaths.Length; i++)
        {
            string entryPath = _sortedPaths[i];

            if (!entryPath.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                break;

            ReadOnlySpan<char> relativePath = entryPath.AsSpan(query.Length);
            int slashIndex = relativePath.IndexOf('/');

            if (slashIndex == -1)
            {
                yield return new FileSystemItem(this, (UPath)entryPath, false);
            }
            else
            {
                ReadOnlySpan<char> currentDirSpan = relativePath[..slashIndex];
                if (lastYieldedDir == null || !currentDirSpan.Equals(lastYieldedDir, StringComparison.OrdinalIgnoreCase))
                {
                    lastYieldedDir = currentDirSpan.ToString();
                    yield return new FileSystemItem(this, UPath.Combine(path, lastYieldedDir), true);
                }
            }
        }
    }
}
