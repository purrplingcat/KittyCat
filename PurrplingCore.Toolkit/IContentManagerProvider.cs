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

public sealed class DefaultContentManagerProvider : IContentManagerProvider
{
    private readonly IServiceProvider _services;
    private readonly ContentManagerOptions _options;

    public ContentManager ContentManager { get; }

    public DefaultContentManagerProvider(IServiceProvider services, IOptions<ContentManagerOptions> options)
    {
        _services = services;
        _options = options.Value;
        ContentManager = CreateContentManager();
    }

    public ContentManager CreateContentManager(string? root = null)
    {
        var loggerFactory = _services.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("ContentManager") ?? NullLogger.Instance;

        return new DefaultContentManager(_services, root ?? _options.ContentRoot, logger);
    }
}
