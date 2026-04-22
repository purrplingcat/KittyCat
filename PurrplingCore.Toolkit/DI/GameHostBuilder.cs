using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Messaging;
using System.Diagnostics;
using System.Reflection;

namespace PurrplingCore.Toolkit.DI;

internal class GameHostBuilder : IGameHostBuilder
{
    private readonly List<Action<IServiceCollection, GameHostBuilderContext>> _serviceActions = [];
    private readonly List<Action<ILoggingBuilder>> _loggingActions = [];
    private readonly List<IGameHostPlugin> _plugins = [];
    private IServiceProviderFactory _serviceProviderFactory = new DefaultServiceProviderFactory();
    private ILogger? _logger;
    private bool _hostBuilt;

    public ServiceCollection Services { get; } = [];

    /// <summary>
    /// Core services configuration for the GameHost and hosted services.
    /// </summary>
    private class GameHostConfiguration : IServiceConfiguration
    {
        private GameHost CreateGameHost(IServiceProvider provider)
        {
            // Require IGame service first (to init graphics services etc)
            // This also tries to avoid circular dependency issues with Monogame's Game class
            // The game instance is the kernel of the application, so it must be created first
            var game = provider.GetService<IGame>()
                ?? throw new InvalidOperationException($"No Game service of '{typeof(IGame)}' found");

            return new GameHost(
                provider, game, // like the game is a goaul'd 🐍 (it requires a host)
                logger: provider.GetRequiredService<ILogger<GameHost>>(),
                startupServices: [.. provider.GetServices<IStartupService>().OrderBy(s => s.Order)],
                cleanupServices: [.. provider.GetServices<ICleanupService>().OrderBy(s => s.Order)]
            );
        }

        public void Configure(IServiceCollection services)
        {
            services.TryAddSingleton(CreateGameHost); // GameHost is resolvable from DI (singleton)
            services.TryAddSingleton<IMessageBus, DefaultMessageBus>();
            services.TryAddTransient(typeof(Lazy<>), typeof(LazyService<>));
        }
    }

    /// <summary>
    /// Add a services configuration applied during the Build() process.
    /// </summary>
    /// <param name="configureDelegate">Action to configure <see cref="IServiceCollection"/></param>
    /// <returns>Game host builder itself</returns>
    public IGameHostBuilder ConfigureServices(Action<IServiceCollection, GameHostBuilderContext> configureDelegate)
    {
        _serviceActions.Add(configureDelegate);
        return this;
    }

    /// <summary>
    /// Add a logging configuration applied during the Build() process.
    /// </summary>
    /// <param name="configureDelegate">Logging builder action to configure <see cref="ILoggingBuilder"/></param>
    /// <returns></returns>
    public IGameHostBuilder ConfigureLogging(Action<ILoggingBuilder> configureDelegate)
    {
        _loggingActions.Add(configureDelegate);
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="factory"></param>
    /// <returns></returns>
    public IGameHostBuilder UseServiceProviderFactory(IServiceProviderFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory, nameof(factory));

        _serviceProviderFactory = factory;
        return this;
    }

    private void ConfigureLogging(ILoggingBuilder builder)
    {
        _loggingActions.ForEach(action => action(builder));
    }

    protected virtual void ConfigureServices(IServiceCollection services, GameHostBuilderContext context)
    {
        var watch = Stopwatch.StartNew();

        _serviceActions.ForEach(action => action(services, context));
        services.AddLogging(ConfigureLogging);
        services.AddConfiguration(new GameHostConfiguration());

        watch.Stop();
        _logger?.LogDebug("Service configuration completed in {ElapsedMilliseconds} ms", watch.ElapsedMilliseconds);
    }

    public ILoggerFactory CreateLoggerFactory() => LoggerFactory.Create(ConfigureLogging);

    private GameHostBuilderContext CreateContext()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var loggerFactory = CreateLoggerFactory();
        
        return new GameHostBuilderContext(loggerFactory, assembly);
    }

    /// <summary>
    /// Build the <see cref="GameHost"/> instance with hosted game and services.
    /// This method can only be called once.
    /// </summary>
    /// <returns>The <see cref="GameHost"/> instance</returns>
    public GameHost Build()
    {
        if (_hostBuilt)
        {
            throw new InvalidOperationException("GameHostBuilder can only build once");
        }
        _hostBuilt = true;

        var watch = Stopwatch.StartNew();
        using var context = CreateContext();
        _logger = CreateDiagnosticLogger(context);

        UsePlugins(context);
        ConfigureServices(Services, context);
        // prevent further modifications to the service collection after configuration
        Services.MakeReadOnly();

        var provider = _serviceProviderFactory.CreateServiceProvider(Services);
        var host = provider.GetRequiredService<GameHost>();
        _logger.LogDebug("GameHost built in {ElapsedMilliseconds} ms", watch.ElapsedMilliseconds);

        return host;
    }

    private static ILogger CreateDiagnosticLogger(GameHostBuilderContext context)
    {
        var logger = context.LoggerFactory.CreateLogger<GameHostBuilder>();
        logger.LogDebug("{execAssembly} ({toolkit})", context.HostAssembly.FullName, Assembly.GetExecutingAssembly().FullName);

        return logger;
    }

    private void UsePlugins(GameHostBuilderContext context)
    {
        foreach (var plugin in _plugins)
        {
            var watch = Stopwatch.StartNew();
            plugin.Setup(this, context);
            _logger?.LogDebug("Feature '{feature}' applied in {ElapsedMilliseconds} ms", plugin.Name, watch.ElapsedMilliseconds);
        }
    }

    public IGameHostBuilder AddPlugin(IGameHostPlugin feature)
    {
        _plugins.Add(feature);
        return this;
    }
}
