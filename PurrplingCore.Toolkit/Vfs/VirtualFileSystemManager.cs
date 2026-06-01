using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

public class VirtualFileSystemManager : IVirtualFileSystemManager, IDisposable
{
    private readonly IFileSystem _baseFs;
    private IFileSystem _topmostFileSystem;
    private bool _disposed;

    public static PhysicalFileSystem Physical { get; } = new PhysicalFileSystem();

    IFileSystem IVirtualFileSystemManager.Physical => Physical;
    public IFileSystem Base => _baseFs;

    public VirtualFileSystemManager(IHostEnvironment env)
    {
        _baseFs = CreatePlatformFileSystem(env);
        _topmostFileSystem = _baseFs;
    }

    public IFileSystem GetFileSystem()
    {
        return _topmostFileSystem;
    }

    internal static IFileSystem CreatePlatformFileSystem(IHostEnvironment env)
    {
        return env.PlatformType switch
        {
            PlatformType.Desktop => Physical.CreateSubFileSystem(env.BaseDirectory),
            _ => throw new NotSupportedException($"Platform {env.PlatformType} is not supported"),
        };
    }

    public void SetFileSystem(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem, nameof(fileSystem));
        _topmostFileSystem = fileSystem;
    }

    public T? FindFileSystem<T>() where T : IFileSystem
    {
        var current = _topmostFileSystem;
        while (current != null)
        {
            if (current is T matched)
            {
                return matched;
            }
            current = current.GetLowerLevel();
        }

        return default;
    }

    public IFileSystem? FindFileSystem(string name)
    {
        var current = _topmostFileSystem;
        while (current != null)
        {
            if (current is FileSystem fs && fs.Name == name)
            {
                return fs;
            }
            if (current.GetType().Name == name)
            {
                return current;
            }
            current = current.GetLowerLevel();
        }

        return default;
    }

    #region Disposing
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _topmostFileSystem.Dispose();
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
    public static IFileSystem Chain(this IVirtualFileSystemManager vfs, Func<IFileSystem, IFileSystem> factory)
    {
        var chainedFs = factory.Invoke(vfs.GetFileSystem());
        
        vfs.SetFileSystem(chainedFs);
        return chainedFs;
    }

    public static T GetRequiredFileSystem<T>(this IVirtualFileSystemManager vfs) where T : IFileSystem
    {
        return vfs.FindFileSystem<T>() 
            ?? throw new InvalidCastException($"FileSystem '{typeof(T)}' not found in chain.");
    }

    public static bool TryGetFileSystem<T>(this IVirtualFileSystemManager vfs, [MaybeNullWhen(false)] out T result) 
        where T : IFileSystem
    {
        result = vfs.FindFileSystem<T>();
        return result != null;
    }

    public static IFileSystem GetBaseFileSystem(this IVirtualFileSystemManager vfs)
    {
        var fs = vfs.GetFileSystem();

        while (fs.GetLowerLevel() is IFileSystem lowerFs)
        {
            fs = lowerFs;
        }

        Debug.Assert(fs != null, "Base file system cannot be null");
        return fs;
    }
}

public interface IFileSystemBootstrap
{
    void Initialize(IVirtualFileSystemManager vfs);
}
