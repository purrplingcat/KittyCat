using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PurrplingCore.Toolkit.DI;

public interface IGameHostPlugin 
{
    string Name { get; }
    void OnBuild(IGameHostBuilder builder, GameHostBuilderContext context);
    void OnAdd(IGameHostBuilder gameHostBuilder);
}

public abstract class GameHostPlugin : IGameHostPlugin
{
    private bool _installed;
    private ILogger? _logger;

    public virtual string Name { get; }

    protected ILogger Logger => _logger ?? NullLogger.Instance;

    public GameHostPlugin() : this(null)
    {
    }

    protected GameHostPlugin(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            name = GetType().Name;
        }

        Name = name;
    }

    public abstract void OnAdd(IGameHostBuilder builder);
    protected abstract void OnInstall(IGameHostBuilder builder, GameHostBuilderContext context);

    public void OnBuild(IGameHostBuilder builder, GameHostBuilderContext context)
    {
        if (_installed) return;

        _installed = true;
        _logger = context.CreateLogger(Name);
        OnInstall(builder, context);
    }
}
