using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PurrplingCore.Toolkit.DI.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace PurrplingCore.Toolkit.DI;

public class GameHostBuilder : IGameHostBuilder
{
    private readonly List<Action<IServiceCollection, GameHostBuilderContext>> _serviceActions = [];
    private readonly List<Action<ILoggingBuilder>> _loggingActions = [];
    private readonly List<IGameHostPlugin> _plugins = [];
    private IServiceProviderFactory _serviceProviderFactory = new DefaultServiceProviderFactory();
    private ILogger _logger = NullLogger.Instance;
    private GameVersion? _gameVersion;
    private bool _hostBuilt;

    public event EventHandler<GameHostResolvedEventArgs>? HostResolved;
    public event EventHandler<ServicesConfiguredEventArgs>? ServicesConfigured;
    public event EventHandler<BuildingEventArgs>? Building;

    private readonly ServiceCollection _services = [];
    
    public IServiceCollection Services => _services;

    public IReadOnlyList<IGameHostPlugin> Plugins => _plugins;

    /// <summary>
    /// Core services configuration for the GameHost and hosted services.
    /// </summary>
    private class GameHostServices(GameVersion version) : IServiceConfiguration
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
            // Cleanup unwanted mess
            services.RemoveAll<IServiceCollection>()
                    .RemoveAll<GameHostBuilderContext>()
                    .RemoveAll<IGameHostBuilder>();

            services.AddSingleton(version); // Add game version as a service
            services.AddSingleton(services); // Add service collection itself as a singleton service
            services.TryAddSingleton(CreateGameHost); // GameHost is resolvable from DI (singleton)
            services.TryAddTransient(typeof(Lazy<>), typeof(LazyService<>));
        }
    }

    /// <inheritdoc />
    public IGameHostBuilder AddGame<TGame>() where TGame : Game
    {
        if (_gameVersion != null)
        {
            throw new InvalidOperationException("Game is already added to the host");
        }

        _gameVersion = GameVersion.Of<TGame>();

        return ConfigureServices(static (services, _) => {
            var gameType = typeof(TGame);
            var servicesAttrs = gameType.GetCustomAttributes<GameServicesAttribute>();

            foreach (var attr in servicesAttrs)
            {
                services.AddServices(attr.CreateConfiguration());
            }

            // Add services from the assembly containing the game type
            services.AddServices(new AssemblyServices(gameType.Assembly))
                    .AddGame<TGame>();
        });
    }

    /// <inheritdoc />
    public IGameHostBuilder ConfigureServices(Action<IServiceCollection, GameHostBuilderContext> configureDelegate)
    {
        _serviceActions.Add(configureDelegate);
        return this;
    }

    /// <inheritdoc />
    public IGameHostBuilder ConfigureLogging(Action<ILoggingBuilder> configureDelegate)
    {
        _loggingActions.Add(configureDelegate);
        return this;
    }

    /// <inheritdoc />
    public IGameHostBuilder UseServiceProviderFactory(IServiceProviderFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory, nameof(factory));

        _serviceProviderFactory = factory;
        return this;
    }

    /// <inheritdoc />
    private void ConfigureLogging(ILoggingBuilder builder)
    {
        _loggingActions.ForEach(action => action(builder));
    }

    protected virtual void ConfigureServices(IServiceCollection services, GameHostBuilderContext context)
    {
        var watch = Stopwatch.StartNew();

        _serviceActions.ForEach(action => action(services, context));
        services.AddLogging(ConfigureLogging);
        services.AddServices(new GameHostServices(_gameVersion ?? GameVersion.Empty));
        ServicesConfigured?.Invoke(services, new ServicesConfiguredEventArgs(services, context));

        watch.Stop();
        _logger?.LogDebug("Service configuration completed in {ElapsedMilliseconds} ms", watch.ElapsedMilliseconds);
    }

    private GameHostBuilderContext CreateContext(ILoggerFactory loggerFactory)
    {
        var gameVersion = _gameVersion ?? GameVersion.Empty;
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var context = new GameHostBuilderContext(loggerFactory, assembly, gameVersion);

        return context;
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
        using var loggerFactory = LoggerFactory.Create(ConfigureLogging);
        using var context = CreateContext(loggerFactory);
        _logger = context.CreateLogger<GameHostBuilder>();

        LogGameInformation(context);
        Validate(context);
        Building?.Invoke(this, new BuildingEventArgs(this, context));
        UsePlugins(context);
        ConfigureServices(_services, context);
        
        // prevent further modifications to the service collection after configuration
        _services.MakeReadOnly();

        var provider = _serviceProviderFactory.CreateServiceProvider(_services);
        var host = ResolveHost(provider);

        _logger.LogDebug("GameHost built in {ElapsedMilliseconds} ms", watch.ElapsedMilliseconds);
        return host;
    }

    private GameHost ResolveHost(IServiceProvider provider)
    {
        var host = provider.GetRequiredService<GameHost>();

        HostResolved?.Invoke(this, new GameHostResolvedEventArgs(host));

        return host;
    }

    protected virtual void Validate(GameHostBuilderContext context)
    {
        if (_gameVersion == null)
        {
            throw new GameHostBuildException("No game has added to the host! Did you call AddGame properly?");
        }
    }

    private static void LogGameInformation(GameHostBuilderContext context)
    {
        var logger = context.Logger;

        logger.LogInformation(
            "{GameVersion} on {OS} - {Platform} {PlatformType}", 
            context.GameVersion, 
            context.OperatingSystem,
            context.OperatingSystem.Platform,
            context.PlatformType
        );
        logger.LogInformation(
            "{execAssembly} ({toolkit})", 
            context.HostAssembly.FullName, 
            Assembly.GetExecutingAssembly().FullName
        );
    }

    private void UsePlugins(GameHostBuilderContext context)
    {
        foreach (var plugin in _plugins)
        {
            var watch = Stopwatch.StartNew();
            plugin.OnBuild(this, context);
            _logger?.LogDebug("Plugin '{plugin}' installed in {elapsed} ms", plugin.Name, watch.ElapsedMilliseconds);
        }
    }

    public IGameHostBuilder AddPlugin(IGameHostPlugin plugin)
    {
        _plugins.Add(plugin);
        plugin.OnAdd(this);
        return this;
    }
}

public sealed class GameHostBuildException : InvalidOperationException
{
    public GameHostBuildException() : base()
    {
    }
    
    public GameHostBuildException(string message) : base(message)
    {
    }

    public GameHostBuildException(string message, Exception innerException) : base(message, innerException) 
    {
    }
}
