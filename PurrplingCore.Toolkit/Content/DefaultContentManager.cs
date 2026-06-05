
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit.Vfs;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Content;

public class DefaultContentManager(
    IServiceProvider serviceProvider,
    string rootDirectory,
    ILogger logger,
    IFileSystem? fileSystem = null
) : ContentManager(serviceProvider, rootDirectory)
{
    private readonly IFileSystem? _fileSystem = fileSystem ?? serviceProvider.GetService<IFileSystem>();
    private readonly ILogger _logger = logger;

    public override T Load<T>(string assetName)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetName, nameof(assetName));

        if (LoadedAssets.TryGetValue(assetName, out var cachedAsset) && cachedAsset is T result)
        {
            return result;
        }

        _logger.LogDebug("Loading asset: {AssetName}", assetName);
        return base.Load<T>(assetName);
    }

    protected override Stream OpenStream(string assetName)
    {
        if (_fileSystem != null)
        {
            var path = Path.Combine(RootDirectory, assetName);
            if (!Path.HasExtension(assetName))
            {
                Path.ChangeExtension(path, ".xnb");
            }

            _fileSystem.OpenRead(path);
        }

        return base.OpenStream(assetName);
    }
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
        string indent = new string(' ', indentLevel * 2);
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
