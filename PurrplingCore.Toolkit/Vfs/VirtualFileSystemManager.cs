using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using System.IO;
using System.Reflection.Emit;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

internal partial class VirtualFileSystemManager 
    : IVirtualFileSystemManager, IVirtualFileSystem, IDisposable
{

    private readonly AggregateFileSystem _rootFs;
    private readonly MountFileSystem _fsTab;
    private readonly ILogger _logger;
    private bool _disposed;

    public IFileSystem Root => _fsTab;

    public VirtualFileSystemManager(ILogger logger)
    {
        _rootFs = new AggregateFileSystem();
        _fsTab = new MountFileSystem(_rootFs);
        _logger = logger;
    }

    private static UPath SanitizePath(UPath path)
    {
        if (!path.IsAbsolute)
        {
            path = path.ToAbsolute();
        }

        return path;
    }

    public void Mount(string target, IFileSystem fs)
    {
        if (fs == _fsTab) 
            throw new ArgumentException("Cannot mount itself", nameof(fs));

        UPath path = SanitizePath(target);
        if (_rootFs.FileExists(path) || _rootFs.DirectoryExists(path))
            LogCoverWarn(_logger, path, fs);

        _fsTab.Mount(path, fs);
        LogMount(_logger, fs, path);
    }

    public void AddFileSystem(IFileSystem fs)
    {  
        foreach (var mount in _fsTab.GetMounts())
        {
            if (_rootFs.FileExists(mount.Key) || _rootFs.DirectoryExists(mount.Key))
                LogCoverWarn(_logger, mount.Key, mount.Value);
        }

        _rootFs.AddFileSystem(fs);
    }

    public void Unmount(string target)
    {
        UPath path = SanitizePath(target);

        if (_fsTab.IsMounted(path))
            _fsTab.Unmount(path);
    }

    public void RemoveFileSystem(IFileSystem fs)
    {
        _rootFs.RemoveFileSystem(fs);
    }

    #region IVirtualFileSystem implementation
    bool IVirtualFileSystem.FileExists(string path)
    {
        return _fsTab.FileExists(SanitizePath(path));
    }

    Stream IVirtualFileSystem.OpenRead(string path)
    {
        LogFileOpenRead(_logger, path);
        return _fsTab.OpenFile(SanitizePath(path), FileMode.Open, FileAccess.Read);
    }

    Stream IVirtualFileSystem.OpenWrite(string path)
    {
        LogFileOpenWrite(_logger, path);
        return _fsTab.OpenFile(SanitizePath(path), FileMode.Create, FileAccess.Write);
    }

    bool IVirtualFileSystem.DirectoryExists(string path)
    {
        return _fsTab.DirectoryExists(SanitizePath(path));
    }

    Stream IVirtualFileSystem.Open(string path, FileMode mode, FileAccess access)
    {
        LogFileOpen(_logger, mode, access, path);
        return _fsTab.OpenFile(SanitizePath(path), mode, access);
    }
    #endregion

    #region Logging
    [LoggerMessage(EventId = 15, Level = LogLevel.Warning, Message = "Shadow '{Target}' is hidden by active mount: {FileSystem} \nShadow FS will not be used for this path.")]
    static partial void LogCoverWarn(ILogger logger, UPath target, IFileSystem fileSystem);

    [LoggerMessage(EventId = 10, Level = LogLevel.Trace, Message = "Mount {Fs} as '{Path}'")]
    static partial void LogMount(ILogger logger, IFileSystem fs, UPath path);

    [LoggerMessage(EventId = 3, Level = LogLevel.Trace, Message = "Open file for READ: {Path}")]
    static partial void LogFileOpenRead(ILogger logger, string path);

    [LoggerMessage(EventId = 3, Level = LogLevel.Trace, Message = "Open file for WRITE: {Path}")]
    static partial void LogFileOpenWrite(ILogger logger, string path);

    [LoggerMessage(EventId = 3, Level = LogLevel.Trace, Message = "Open file \"{Path}\" Mode: {Mode}, Access: {Access}")]
    static partial void LogFileOpen(ILogger logger, FileMode mode, FileAccess access, string path);
    #endregion

    #region Disposing
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _rootFs.Dispose();
                _fsTab.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion

}
