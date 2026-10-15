using MonoGame.Framework.Content.Pipeline.Builder;
using PurrplingCore.Toolkit.Content;

namespace PurrplingCore.ContentBuilder;

internal static class BundleContentRootResolver
{
    public static string[] Resolve(IContentCollection collection, string sourceDirectory)
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourcePath in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourcePath).Replace('\\', '/');
            if (!collection.GetContentInfo(relativePath, out var contentInfos))
            {
                continue;
            }

            foreach (var contentInfo in contentInfos)
            {
                var root = contentInfo.ContentRoot.Replace('\\', '/').Trim('/');
                if (root.Length > 0)
                {
                    roots.Add(AssetCatalogSerializer.NormalizeObjectPath(root));
                }
            }
        }

        if (roots.Count == 0)
        {
            return ["Content"];
        }

        return [.. roots.Order(StringComparer.OrdinalIgnoreCase)];
    }
}