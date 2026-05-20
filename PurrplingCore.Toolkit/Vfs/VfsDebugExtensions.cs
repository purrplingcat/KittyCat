using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Metadata;
using PurrplingCore.Toolkit.Vfs.FileSystems;
using System.Text;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

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

    public static string GetName(this IFileSystem fs)
    {
        if (fs is FileSystem fs0 && fs0.Name != null)
        {
            return fs0.Name;
        }

        var name = fs.GetType().GetDisplayName();
        return name.EndsWith("FileSystem") 
            ? name[..^10] 
            : name;
    }

    public static string DumpStructure(this IFileSystem? fs, int indentLevel = 0)
    {
        if (fs == null) return "null";

        var sb = new StringBuilder();
        string indent = new(' ', indentLevel * 2);
        string name = fs.GetName();
        
        switch (fs)
        {
            case MountFileSystem mountFs:
                SortedList<UPath, IFileSystem> mounts = new(mountFs.GetMounts());
                sb.AppendLine($"{indent}📁 {name} (Mounts: {mounts.Count})");
                foreach (var kvp in mounts)
                {
                    sb.Append($"{indent}  🔗 [{kvp.Key}] -> ");
                    sb.Append(kvp.Value.DumpStructure(indentLevel + 2).TrimStart());
                }
                sb.AppendFallback(mountFs, indentLevel + 1);
                break;

            case AggregateFileSystem aggFs:
                var layers = aggFs.GetFileSystems();
                sb.AppendLine($"{indent}🥞 {name} (Layers: {layers.Count})");
                for (int i = layers.Count - 1; i >= 0; i--)
                {
                    sb.Append(layers[i].DumpStructure(indentLevel + 1));
                }
                sb.AppendFallback(aggFs, indentLevel + 1);
                break;

            case SubFileSystem subFs:
                // Zio's SubFileSystem obvykle publikuje SubPath, 
                // k podkladovému FS se dostaneme přes Fallback
                sb.AppendLine($"{indent}✂️ {name} (SubPath: {subFs.SubPath})");
                if (subFs.Fallback != null)
                    sb.Append(subFs.Fallback.DumpStructure(indentLevel + 1));
                break;

            case ReadOnlyFileSystem roFs:
                sb.AppendLine($"{indent}🔒 {name}");
                if (roFs.Fallback != null)
                    sb.Append(roFs.Fallback.DumpStructure(indentLevel + 1));
                break;

            case ZipArchiveFileSystem zip:
                sb.AppendLine($"{indent}📦 {name}");
                break;

            case PhysicalFileSystem:
                sb.AppendLine($"{indent}💻 {name}");
                break;

            case PrefixedFileSystem prefixFs:
                sb.Append($"{indent}🏷️ [{prefixFs.Prefix}] -> ");
                sb.Append(prefixFs.Inner.DumpStructure(indentLevel + 1).TrimStart());
                break;

            default:
                if (fs is ComposeFileSystem composeFs && composeFs.Fallback != null)
                {
                    sb.AppendLine($"{indent}📦 {name}");
                    sb.AppendFallback(composeFs, indentLevel);
                }
                else
                {
                    sb.AppendLine($"{indent}📄 {name}");
                }
                break;
        }

        return sb.ToString();
    }

    private static StringBuilder AppendFallback(this StringBuilder sb, ComposeFileSystem fs, int indentLevel)
    {
        if (fs.Fallback != null)
        {
            string indent = new(' ', indentLevel * 2);
            sb.Append($"{indent}↪️ Fallback -> ");
            sb.Append(fs.Fallback.DumpStructure(indentLevel + 1).TrimStart());
        }
        return sb;
    }
}
