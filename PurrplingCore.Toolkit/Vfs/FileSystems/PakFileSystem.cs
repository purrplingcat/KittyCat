using PurrplingCore.Toolkit.Pak;
using System.IO.Compression;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs.FileSystems;

public class PakFileSystem : FileSystem
{
    private readonly PakArchive _pak;
    private readonly List<Node> _sortedPaths;
    private readonly ReaderWriterLockSlim _entriesLock = new();
    private bool _disposed;

    private const char DirectorySeparator = '/';

    public string? Source { get; }

    public PakFileSystem(PakArchive pak)
    {
        _pak = pak ?? throw new ArgumentNullException(nameof(pak));

        _sortedPaths = CreateDirectoryStructure(pak.GetAllEntries());
        _sortedPaths.Sort();
        _sortedPaths.EnsureCapacity(_sortedPaths.Count);
    }

    public PakFileSystem(string pakFilePath) : this(new PakArchive(pakFilePath))
    {
        Source = pakFilePath;
    }

    private readonly struct Node(UPath path, bool directory) : IComparable<Node>
    {
        public UPath Path => path;
        public bool IsDirectory => directory;
        public bool IsNull => path.IsNull;
        public string FullName => path.FullName;

        public int CompareTo(Node other)
        {
            return string.Compare(FullName, other.FullName, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return FullName;
        }

        public Node GetParent() => new(path.GetDirectory(), directory: true);
    }

    private List<Node> CreateDirectoryStructure(IEnumerable<PakArchive.PakEntry> entries)
    {
        var hashset = new SortedSet<Node>();

        foreach (var entry in entries)
        {
            var node = new Node(ConvertPathFromInternal(entry.Path), directory: false);
            while (!node.IsNull)
            {
                hashset.Add(node);
                node = node.GetParent();
            }
        }

        return [.. hashset];
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

        var directory = new Node(path, directory: true);
        int index = _sortedPaths.BinarySearch(directory);
        if (index < 0) return false;

        return _sortedPaths[index].IsDirectory;
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
        var attrs = FileAttributes.ReadOnly | FileAttributes.Archive;
        attrs |= DirectoryExists(path) ? FileAttributes.Directory : FileAttributes.Normal;
        attrs |= path == UPath.Root ? FileAttributes.Directory : FileAttributes.None;

        return attrs;
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
        return EnumeratePathsStr(path, "*", searchOption, SearchTarget.Both).Select(p => new FileSystemItem(this, p, p[p.Length - 1] == DirectorySeparator));
    }

    /// <inheritdoc />
    protected override IEnumerable<UPath> EnumeratePathsImpl(UPath path, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
    {
        return EnumeratePathsStr(path, searchPattern, searchOption, searchTarget).Select(x => new UPath(x));
    }

    private IEnumerable<string> EnumeratePathsStr(UPath path, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
    {
        var search = SearchPattern.Parse(ref path, ref searchPattern);

        _entriesLock.EnterReadLock();
        IEnumerable<Node> entries;
        try
        {
            entries = GetEntriesInDirectory(path.FullName).Where(p => p.FullName.Length > path.FullName.Length);

            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                entries = entries.Where(node => node.Path.IsInDirectory(path, false));
            }
        }
        finally
        {
            _entriesLock.ExitReadLock();
        }

        if (!entries.Any())
        {
            return [];
        }

        if (searchTarget == SearchTarget.File)
        {
            entries = entries.Where(e => !e.IsDirectory);
        }
        else if (searchTarget == SearchTarget.Directory)
        {
            entries = entries.Where(e => e.IsDirectory);
        }

        if (!string.IsNullOrEmpty(searchPattern))
        {
            entries = entries.Where(e => search.Match(GetName(e.Path)));
        }

        return entries.Select(e => e.FullName);
    }

    private static readonly char[] s_slashChars = ['/', '\\'];

    private static ReadOnlySpan<char> GetName(UPath entry)
    {
        var name = entry.FullName.TrimEnd(s_slashChars);
        var index = name.LastIndexOfAny(s_slashChars);
        return index == -1 ? name.AsSpan() : name.AsSpan(index + 1);
    }

    private IEnumerable<Node> GetEntriesInDirectory(string srcDir)
    {
        return _sortedPaths.Where(e =>
        {
            if (!e.FullName.StartsWith(srcDir, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (e.FullName.Length == srcDir.Length)
            {
                return true;
            }

            // ensure that we are matching only subdirectories/files
            return e.Path.IsInDirectory(srcDir, recursive: true);
        });
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

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _pak.Dispose();
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }
}
