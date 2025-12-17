using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        return TitleContainer.OpenStream(contentManager.RootDirectory + DirectorySeparatorChar + path);
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
