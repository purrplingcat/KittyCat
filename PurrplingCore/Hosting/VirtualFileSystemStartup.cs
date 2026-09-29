using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Vfs;
using System;
using System.IO;
using Zio.FileSystems;

namespace PurrplingCore.Hosting;

/// <summary>
/// Registers initialization-time mounts for writable runtime storage locations.
/// </summary>
/// <remarks>
/// These mounts are application policy, not VFS defaults: memory is ephemeral, user data is
/// persistent, cache is rebuildable, and temp data may be removed by the operating system.
/// </remarks>
public class VirtualFileSystemStartup(
    IVirtualFileSystemManager vfs,
    IGame game,
    IHostEnvironment env
) : IStartupService
{
    private readonly IHostEnvironment _env = env;
    private readonly IGame _game = game;
    private readonly IVirtualFileSystemManager _vfs = vfs;

    public int Order => -100;

    public void Startup()
    {
        // Mounts must be created during the game initialization phase, not during host construction.
        _game.Initializing += OnGameInitializing;
    }

    private void OnGameInitializing(object? sender, EventArgs e)
    {
        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _env.ApplicationName);
        string cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _env.ApplicationName, "Cache");
        string temp = Path.Combine(Path.GetTempPath(), _env.ApplicationName);
        var physical = new PhysicalFileSystem();

        // Ephemeral files held only for the lifetime of the process.
        _vfs.Mount("/memory", new MemoryFileSystem());
        // Persistent per-user game data, such as saves and user settings.
        _vfs.Mount("/user", physical.CreateSubFileSystem(appData));
        // Rebuildable per-user cached data.
        _vfs.Mount("/cache", physical.CreateSubFileSystem(cache));
        // Temporary files that may be discarded by the operating system.
        _vfs.Mount("/temp", physical.CreateSubFileSystem(temp));
    }
}
