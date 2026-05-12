using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit.Content;

namespace PurrplingCore.Toolkit;

public interface IContentManagerProvider
{
    public ContentManager Default { get; }
    public ContentManager CreateContentManager(string? root = null);
}

public sealed class DefaultContentManagerProvider : IContentManagerProvider
{
    private readonly IServiceProvider _services;
    private readonly ContentManagerOptions _options;

    public ContentManager Default { get; }

    public DefaultContentManagerProvider(IServiceProvider services, IOptions<ContentManagerOptions> options)
    {
        _services = services;
        _options = options.Value;
        Default = CreateContentManager();
    }

    public ContentManager CreateContentManager(string? root = null)
    {
        return new ExtensibleContentManager(_services, root ?? _options.ContentRoot);
    }
}
