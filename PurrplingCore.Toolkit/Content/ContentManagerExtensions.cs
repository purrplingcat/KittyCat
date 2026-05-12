using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Zio;

namespace PurrplingCore.Toolkit.Content;

public interface IContentLoader<out T>
{
    T Load(ContentManager contentManager, string path);
}

public interface IContentLoader
{
    T Load<T>(ContentManager contentManager, string path);
}

public static class ContentManagerExtensions
{
    public const string DirectorySeparatorChar = "/";

    public static Stream OpenStream(this ContentManager contentManager, string path)
    {
        var fs = contentManager.ServiceProvider.GetService<IFileSystem>();

        if (fs != null)
        {
            var uPath = UPath.Combine(UPath.Root, contentManager.RootDirectory, path);

            if (fs.FileExists(uPath))
            {
                return fs.OpenFile(uPath, FileMode.Open, FileAccess.Read);
            }
        }

        return TitleContainer.OpenStream(Path.Combine(contentManager.RootDirectory, path));
    }

    /// <summary>
    /// Loads the content using a custom content loader.
    /// </summary>
    public static T Load<T>(this ContentManager contentManager, string path, IContentLoader contentLoader)
    {
        return contentLoader.Load<T>(contentManager, path);
    }

    /// <summary>
    /// Loads the content using a custom content loader.
    /// </summary>
    public static T Load<T>(this ContentManager contentManager, string path, IContentLoader<T> contentLoader)
    {
        return contentLoader.Load(contentManager, path);
    }
}
