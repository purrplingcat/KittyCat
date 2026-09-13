using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

public class VirtualFileSystemManager : IVirtualFileSystemManager, IDisposable
{
    private readonly IFileSystem _baseFs;
    private readonly MountFileSystem _root;
    private bool _disposed;

    public static PhysicalFileSystem Physical { get; } = new PhysicalFileSystem();

    IFileSystem IVirtualFileSystemManager.Physical => Physical;
    public IFileSystem Base => _baseFs;
    internal MountFileSystem Root => _root;

    public VirtualFileSystemManager(IHostEnvironment env)
    {
        _baseFs = CreatePlatformFileSystem(env);
        _root = new MountFileSystem(_baseFs);
    }

    public IFileSystem GetFileSystem()
    {
        return _root;
    }

    internal static IFileSystem CreatePlatformFileSystem(IHostEnvironment env)
    {
        return env.PlatformType switch
        {
            PlatformType.Desktop => Physical.CreateSubFileSystem(env.BaseDirectory),
            _ => throw new NotSupportedException($"Platform {env.PlatformType} is not supported"),
        };
    }

    #region Disposing
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _root.Dispose();
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

public static class VirtualFileSystemManagerExtensions
{
    public static IFileSystem GetBaseFileSystem(this IVirtualFileSystemManager vfs)
    {
        return vfs.Base;
    }
}
