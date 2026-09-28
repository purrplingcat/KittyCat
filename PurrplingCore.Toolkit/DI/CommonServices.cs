using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PurrplingCore.Toolkit.Hosting;

namespace PurrplingCore.Toolkit.DI;

public interface IOrderedService
{
    int Order { get; }
}

public interface IStartupService : IOrderedService
{
    void Startup();
}

public interface ICleanupService : IOrderedService
{
    void Cleanup();
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

    public void Startup()
    {
        foreach (var activator in _options.AutoActivators)
        {
            _ = _services.GetRequiredService(activator);
            _logger.LogTrace("Auto-activated service: {serviceType}", activator);
        }
    }
}

internal sealed partial class StartupServiceExecutor(
    IEnumerable<IStartupService> startups,
    IEnumerable<ICleanupService> cleanups,
    ILoggerFactory loggerFactory,
    IHostEnvironment environment
)
{
    private static readonly TimeSpan SlowServiceThreshold = TimeSpan.FromSeconds(1);
    private static readonly Action<IStartupService> ExecuteStartup = static service => service.Startup();
    private static readonly Action<ICleanupService> ExecuteCleanup = static service => service.Cleanup();

    private readonly ILogger _logger = loggerFactory.CreateLogger("StartupServiceExecutor");
    private readonly IStartupService[] _startups = [.. startups.OrderBy(x => x.Order)];
    private readonly ICleanupService[] _cleanups = [.. cleanups.OrderBy(x => x.Order)];
    private readonly bool _measureDuration = environment.IsDevelopment();

    public void Startup() => ExecuteServices(_startups, ServicePhase.Startup, ExecuteStartup);

    public void CleanUp() => ExecuteServices(_cleanups, ServicePhase.Cleanup, ExecuteCleanup);

    private void ExecuteServices<TService>(TService[] services, ServicePhase phase, Action<TService> executeService)
        where TService : IOrderedService
    {
        var measureDuration = _measureDuration;
        var phaseStartedAt = measureDuration ? Stopwatch.GetTimestamp() : 0;

        foreach (var service in services)
        {
            var serviceType = service.GetType();
            LogServiceStart(_logger, phase, serviceType, service.Order);

            var startedAt = measureDuration ? Stopwatch.GetTimestamp() : 0;
            executeService(service);

            if (measureDuration)
            {
                LogSlowServiceIfNeeded(phase, serviceType, service.Order, startedAt);
            }
        }

        if (measureDuration)
        {
            LogPhaseDuration(phase, services.Length, phaseStartedAt);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LogSlowServiceIfNeeded(ServicePhase phase, Type serviceType, int order, long startedAt)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);

        if (elapsed >= SlowServiceThreshold)
        {
            LogSlowService(_logger, phase, serviceType, order, elapsed.TotalMilliseconds);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LogPhaseDuration(ServicePhase phase, int serviceCount, long startedAt)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var duration = elapsed.TotalMilliseconds;

        LogPhaseCompleted(_logger, phase, serviceCount, duration);

        if (elapsed >= SlowServiceThreshold)
        {
            LogSlowPhase(_logger, phase, serviceCount, duration);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Trace, Message = "{Phase}: {ServiceType}, Order: {Order}")]
    private static partial void LogServiceStart(ILogger logger, ServicePhase phase, Type serviceType, int order);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "{Phase} service {ServiceType} with order {Order} took {Duration} ms")]
    private static partial void LogSlowService(ILogger logger, ServicePhase phase, Type serviceType, int order, double duration);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "{Phase} phase across {ServiceCount} services took {Duration} ms")]
    private static partial void LogSlowPhase(ILogger logger, ServicePhase phase, int serviceCount, double duration);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "{Phase} completed {ServiceCount} services in {Duration} ms")]
    private static partial void LogPhaseCompleted(ILogger logger, ServicePhase phase, int serviceCount, double duration);

    private enum ServicePhase : byte
    {
        Startup,
        Cleanup,
    }
}
