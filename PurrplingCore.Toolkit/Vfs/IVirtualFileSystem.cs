using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Hosting;
using System.Text;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

public record FileSystemLayer(IFileSystem FileSystem, int Order);

public interface IVirtualFileSystem
{
    Stream Open(string path, FileMode mode, FileAccess access);
    Stream OpenRead(string path);
    Stream OpenWrite(string path);
    bool FileExists(string path);
    bool DirectoryExists(string path);
}

internal partial class VirtualFileSystemManager(IHostEnvironment env, ILogger logger) 
    : IVirtualFileSystemManager, IVirtualFileSystem
{
    private readonly PhysicalFileSystem _physicalFs = new();
    private readonly MountFileSystem _rootFs = new();
    private readonly string _baseDirectory = env.BaseDirectory;
    private readonly ILogger _logger = logger;

    public IFileSystem Root => _rootFs;
    public string BaseDirectory => _baseDirectory;

    public IFileSystem CreateSubFileSystem(params string[] path)
    {
        var combinedPath = Path.Combine(path);

        if (!Path.IsPathRooted(combinedPath))
        {
            combinedPath = Path.Combine(_baseDirectory, combinedPath);
        }

        UPath safePath = _physicalFs.ConvertPathFromInternal(combinedPath);
        return _physicalFs.GetOrCreateSubFileSystem(safePath);
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
        UPath path = SanitizePath(target);
        LogMount(_logger, fs, path);
        _rootFs.Mount(target, fs);
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
        return _rootFs.OpenFile(SanitizePath(path), FileMode.OpenOrCreate, FileAccess.Write);
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
    [LoggerMessage(Level = LogLevel.Trace, Message = "Mount {Fs} as '{Path}'")]
    static partial void LogMount(ILogger logger, IFileSystem fs, UPath path);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Open file for READ: {Path}")]
    static partial void LogFileOpenRead(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Open file for WRITE: {Path}")]
    static partial void LogFileOpenWrite(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Open file \"{Path}\" Mode: {Mode}, Access: {Access}")]
    static partial void LogFileOpen(ILogger logger, FileMode mode, FileAccess access, string path);
    #endregion
}

public static class VfsDebugExtensions
{
    public static void LogVfsStructure(this ILogger logger, IFileSystem? fs, LogLevel level = LogLevel.Trace)
    {
        if (fs == null) return;
        if (!logger.IsEnabled(level)) return;

        var tree = fs.DumpStructure();
        foreach (var line in tree.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries))
        {
            logger.Log(level, "{line}", line);
        }
    }

    public static string DumpStructure(this IFileSystem? fs, int indentLevel = 0)
    {
        if (fs == null) return "null";

        var sb = new StringBuilder();
        string indent = new(' ', indentLevel * 2);
        string typeName = fs.GetType().Name;

        switch (fs)
        {
            case MountFileSystem mountFs:
                var mounts = mountFs.GetMounts();
                sb.AppendLine($"{indent}📁 {typeName} (Mounts: {mounts.Count})");
                foreach (var kvp in mounts)
                {
                    sb.AppendLine($"{indent}  🔗 [{kvp.Key}] ->");
                    sb.Append(kvp.Value.DumpStructure(indentLevel + 2));
                }
                break;

            case AggregateFileSystem aggFs:
                var layers = aggFs.GetFileSystems();
                sb.AppendLine($"{indent}🥞 {typeName} (Layers: {layers.Count})");

                // Zio bere vrstvy odzadu (nejvyšší index = nejvyšší priorita)
                // Takže to vypíšeme shora dolů, ať vidíš, co přebíjí co.
                for (int i = layers.Count - 1; i >= 0; i--)
                {
                    sb.Append(layers[i].DumpStructure(indentLevel + 1));
                }
                break;

            case SubFileSystem subFs:
                // Zio's SubFileSystem obvykle publikuje SubPath, 
                // k podkladovému FS se dostaneme přes Fallback
                sb.AppendLine($"{indent}✂️ {typeName} (SubPath: {subFs.SubPath})");
                if (subFs.Fallback != null)
                    sb.Append(subFs.Fallback.DumpStructure(indentLevel + 1));
                break;

            case ReadOnlyFileSystem roFs:
                sb.AppendLine($"{indent}🔒 {typeName}");
                if (roFs.Fallback != null)
                    sb.Append(roFs.Fallback.DumpStructure(indentLevel + 1));
                break;

            case ZipArchiveFileSystem zipFs:
                sb.AppendLine($"{indent}📦 {typeName}");
                break;

            case PhysicalFileSystem physFs:
                sb.AppendLine($"{indent}💻 {typeName}");
                break;

            default:
                if (fs is ComposeFileSystem composeFs && composeFs.Fallback != null)
                {
                    sb.AppendLine($"{indent}🔄 {typeName}");
                    sb.Append(composeFs.Fallback.DumpStructure(indentLevel + 1));
                }
                else
                {
                    sb.AppendLine($"{indent}📄 {typeName}");
                }
                break;
        }

        return sb.ToString();
    }
}
