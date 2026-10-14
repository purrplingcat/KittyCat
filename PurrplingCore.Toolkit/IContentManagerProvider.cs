using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit.Content;

namespace PurrplingCore.Toolkit;

/// <summary>
/// Provides access to the default content manager
/// and allows creating new content managers with specified roots.
/// </summary>
public interface IContentManagerProvider
{
    /// <summary>
    /// Gets the default content manager.
    /// </summary>
    public ContentManager ContentManager { get; }

    /// <summary>
    /// Creates a new content manager with the specified root.
    /// </summary>
    /// <param name="root">The root directory for the content manager. If null, the default root is used.</param>
    /// <returns>A new instance of <see cref="ContentManager"/>.</returns>
    public ContentManager CreateContentManager(string? root = null);
}

public class ContentPathResolver
{
    private readonly Dictionary<string, string> _roots = new(StringComparer.OrdinalIgnoreCase);

    public void Map(string packageName, string path) => _roots[packageName] = path;

    public bool Unmap(string packageName) => _roots.Remove(packageName);

    public string? Resolve(string packageName) => _roots.TryGetValue(packageName, out var path) ? path : null;
    
    public bool TryResolve(string packageName, [MaybeNullWhen(false)] out string path) => _roots.TryGetValue(packageName, out path);
}

public sealed class DefaultContentManagerProvider : IContentManagerProvider
{
    private readonly IServiceProvider _services;
    private readonly ContentPathResolver _resolver;
    private readonly ContentManagerOptions _options;

    public ContentManager ContentManager { get; }

    public DefaultContentManagerProvider(IServiceProvider services, ContentPathResolver resolver, IOptions<ContentManagerOptions> options)
    {
        _services = services;
        _options = options.Value;
        _resolver = resolver;
        ContentManager = CreateContentManager();
    }

    public ContentManager CreateContentManager(string? root = null)
    {
        var loggerFactory = _services.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("ContentManager") ?? NullLogger.Instance;
        root ??= _options.ContentRoot;

        if (_resolver.TryResolve(root, out var resolvedRoot))
        {
            root = resolvedRoot;
        }

        return new DefaultContentManager(_services, root, logger);
    }
}
