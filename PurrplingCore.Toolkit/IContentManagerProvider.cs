using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit.Content;

namespace PurrplingCore.Toolkit;

public interface IContentManagerProvider
{
    public ContentManager ContentManager { get; }
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
