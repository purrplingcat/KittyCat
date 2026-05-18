using Microsoft.Extensions.Logging;
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
                int count = mounts.Count + (mountFs.Fallback != null ? 1 : 0);
                sb.AppendLine($"{indent}📁 {typeName} (Mounts: {count})");
                if (mountFs.Fallback != null)
                {
                    sb.AppendLine($"{indent}  🔗 [{UPath.Root}] ->");
                    sb.Append(mountFs.Fallback.DumpStructure(indentLevel + 2));
                }
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
