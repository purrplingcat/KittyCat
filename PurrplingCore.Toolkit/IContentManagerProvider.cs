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

/// <summary>
/// Creates content managers using literal root paths or the configured default root.
/// </summary>
public sealed class DefaultContentManagerProvider : IContentManagerProvider
{
    private readonly IServiceProvider _services;
    private readonly ContentManagerOptions _options;

    /// <summary>
    /// Gets the content manager created with the configured default root.
    /// </summary>
    public ContentManager ContentManager { get; }

    /// <summary>
    /// Creates the provider and its default content manager from the configured services and root.
    /// </summary>
    public DefaultContentManagerProvider(IServiceProvider services, IOptions<ContentManagerOptions> options)
    {
        _services = services;
        _options = options.Value;
        ContentManager = CreateContentManager();
    }

    /// <summary>
    /// Creates a content manager using the supplied root without logical package resolution.
    /// </summary>
    /// <param name="root">The literal root path, or null to use the configured default. An empty root is preserved.</param>
    /// <returns>A new content manager using the supplied or configured root directory.</returns>
    public ContentManager CreateContentManager(string? root = null)
    {
        var loggerFactory = _services.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("ContentManager") ?? NullLogger.Instance;

        return new DefaultContentManager(_services, root ?? _options.ContentRoot, logger);
    }
}
