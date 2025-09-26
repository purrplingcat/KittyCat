using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.DI;
using System;
using System.Runtime.CompilerServices;

namespace KittyCat.Services;

[Singleton]
public class ContentManagerProvider : IContentManagerProvider
{
    private readonly ContentManager _defaultContent;
    private readonly IServiceProvider _provider;
    private readonly string _rootDir;

    public ContentManager Default => _defaultContent;

    public ContentManagerProvider(IServiceProvider provider, string rootDir = "Content")
    {
        _provider = provider;
        _rootDir = rootDir;
        _defaultContent = CreateContentManager();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContentManager CreateContentManager() => CreateContentManager(_rootDir);

    public virtual ContentManager CreateContentManager(string rootDirectory)
    {
        return new ContentManager(_provider, rootDirectory);
    }
}
