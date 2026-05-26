using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace PurrplingCore.Toolkit.DI;

public interface IStartupService
{
    int Order { get; }
    void OnStartup();
}

public interface ICleanupService
{
    int Order { get; }
    void OnCleanup();
}

public interface IGameService
{
    void Initialize();
    void LoadContent();
    void UnloadContent();
}

internal sealed class AutoActivatorOptions
{
    public HashSet<Type> AutoActivators { get; } = [];
}

internal sealed class AutoActivator : IStartupService
{
    private readonly IServiceProvider _services;
    private readonly AutoActivatorOptions _options;
    private readonly ILogger _logger;

    public AutoActivator(IServiceProvider services, IOptions<AutoActivatorOptions> options, ILogger<AutoActivator> logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        _services = services;
        _options = options.Value;
        _logger = logger ?? NullLogger<AutoActivator>.Instance;
    }

    public int Order => int.MaxValue; // Run after all other startup/cleanup services
    public void OnStartup()
    {
        foreach (var activator in _options.AutoActivators)
        {
            _ = _services.GetRequiredService(activator);
            _logger.LogTrace("Auto-activated service: {serviceType}", activator);
        }
    }
}

internal sealed class StartupServiceExecutor(
    IEnumerable<IStartupService> startups,
    IEnumerable<ICleanupService> cleanups,
    ILogger<StartupServiceExecutor> logger
)
{
    public void Startup()
    {
        foreach (var startup in startups.OrderBy(x => x.Order))
        {
            logger.LogTrace("Startup: {serviceType}, Order: {Order}",
                startup.GetType(), startup.Order
            );
            startup.OnStartup();
        }
    }

    public void CleanUp()
    {
        foreach (var cleanup in cleanups.OrderBy(x => x.Order))
        {
            logger.LogTrace("Cleanup: {serviceType}, Order: {Order}",
                cleanup.GetType(), cleanup.Order
            );
            cleanup.OnCleanup();
        }
    }
}
