using Microsoft.Extensions.Logging;
using System.IO;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

internal partial class VirtualFileSystemManager 
    : IVirtualFileSystemManager, IVirtualFileSystem, IDisposable
{
    private readonly AggregateFileSystem _shadow;
    private readonly MountFileSystem _rootFs;
    private readonly ILogger _logger;
    private bool _disposed;

    public IFileSystem Root => _rootFs;

    public VirtualFileSystemManager(ILogger logger)
    {
        _shadow = new AggregateFileSystem();
        _rootFs = new MountFileSystem(_shadow);
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
        if (fs == _rootFs) 
            throw new ArgumentException("Cannot mount itself", nameof(fs));

        UPath path = SanitizePath(target);
        
        if (_shadow.FileExists(path) || _shadow.DirectoryExists(path))
            LogCoverWarn(_logger, path);

        LogMount(_logger, fs, path);
        _rootFs.Mount(target, fs);
    }

    public void AddShadow(string target, IFileSystem fs)
    {
        if (fs == _rootFs)
            throw new ArgumentException("Cannot shadow itself", nameof(fs));

        UPath path = SanitizePath(target);
        var layer = new MountFileSystem();
        
        if (_rootFs.IsMounted(path)) 
            LogCoverWarn(_logger, path);

        layer.Mount(path, fs);
        _shadow.AddFileSystem(layer);
    }

    public void AddShadow(IFileSystem fs)
    {
        if (fs == _rootFs)
            throw new ArgumentException("Cannot shadow itself", nameof(fs));
        
        foreach (var mount in _rootFs.GetMounts())
        {
            if (_shadow.FileExists(mount.Key) || _shadow.DirectoryExists(mount.Key))
                LogCoverWarn(_logger, mount.Key);
        }

        _shadow.AddFileSystem(fs);
    }

    #region IVirtualFileSystem implementation
    bool IVirtualFileSystem.FileExists(string path)
    {
        return _rootFs.FileExists(SanitizePath(path));
    }

    Stream IVirtualFileSystem.OpenRead(string path)
    {
        LogFileOpenRead(_logger, path);
        return _rootFs.OpenFile(SanitizePath(path), FileMode.Open, FileAccess.Read);
    }

    Stream IVirtualFileSystem.OpenWrite(string path)
    {
        LogFileOpenWrite(_logger, path);
        return _rootFs.OpenFile(SanitizePath(path), FileMode.Create, FileAccess.Write);
    }

    bool IVirtualFileSystem.DirectoryExists(string path)
    {
        return _rootFs.DirectoryExists(SanitizePath(path));
    }

    Stream IVirtualFileSystem.Open(string path, FileMode mode, FileAccess access)
    {
        LogFileOpen(_logger, mode, access, path);
        return _rootFs.OpenFile(SanitizePath(path), mode, access);
    }
    #endregion

    #region Logging
    [LoggerMessage(EventId = 15, Level = LogLevel.Warning, Message = "Shadow '{Target}' is hidden by active mount. Shadow FS will not be used for this path.")]
    static partial void LogCoverWarn(ILogger logger, UPath target);

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
                _shadow.Dispose();
                _rootFs.Dispose();
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
