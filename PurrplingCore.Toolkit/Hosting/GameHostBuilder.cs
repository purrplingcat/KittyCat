using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting.Services;
using PurrplingCore.Toolkit.Messaging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;


namespace PurrplingCore.Toolkit.Hosting;

public class GameHostBuilder : IGameHostBuilder
{
    public const string GameTypePropertyKey = "GameType";

    private readonly string[] _args;
    
    private bool _hasGame;
    private bool _hostBuilt;

    private readonly ServiceCollection _services = [];
    private readonly ILoggingBuilder _logging;
    private readonly HostEnvironment _env;
    private readonly ConfigurationManager _config;
    private IServiceFactoryAdapter _serviceProviderFactory;

    public IServiceCollection Services => _services;
    public ILoggingBuilder Logging => _logging;

    public IConfigurationManager Configuration => _config;

    public IHostEnvironment Environment => _env;

    public IDictionary<object, object> Properties { get; }

    public GameHostBuilder(string[]? args = null)
    {
        _logging = new LoggingBuilder(_services);
        _env = new HostEnvironment();
        _config = new ConfigurationManager();
        _args = args ?? [];
        Properties = new Dictionary<object, object>();

        UseServiceProviderFactory(new DefaultServiceProviderFactory());
        Initialize();
    }

    private void Initialize()
    {
        _config.AddEnvironmentVariables(prefix: "DOTNET_");
        _config.AddCommandLine(_args);
        _env.EnvironmentName = _config[HostDefaults.EnvironmentKey] ?? _env.EnvironmentName;

        if (_env.IsProduction() && !GameCrashLogger.IsRegistered)
        {
            // Fallback when the entry point did not register the process-wide crash logger.
            GameCrashLogger.Register(_env);
        }

        if (_env.IsDevelopment())
        {
            var opts = new ServiceProviderOptions()
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            };

            UseServiceProviderFactory(new DefaultServiceProviderFactory(opts));
        }
    }

    /// <inheritdoc />
    public IGameHostBuilder AddGame<TGame>() where TGame : class, IGame
    {
        if (_hasGame)
        {
            GameHostBuildException.ThrowGameAlreadyAddedException();
        }

        _hasGame = true;
        _env.GameVersion = GameVersion.Of<TGame>();
        _services.AddServiceModule(new AssemblyServices(typeof(TGame).Assembly));
        _services.AddServiceModule<GameServices<TGame>>();
        Properties[GameTypePropertyKey] = typeof(TGame);

        return this;
    }

    /// <inheritdoc 
    [MemberNotNull(nameof(_serviceProviderFactory))]
    public IGameHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        where TContainerBuilder : notnull
    {
        ArgumentNullException.ThrowIfNull(factory, nameof(factory));

        _serviceProviderFactory = new ServiceProviderFactoryAdapter<TContainerBuilder>(factory);
        return this;
    }

    protected virtual void AddComonServices(IServiceCollection services)
    {
        services.AddLogging();
        services.AddSingleton(_serviceProviderFactory);
        services.AddSingleton<IConfiguration>(_config);
        services.AddSingleton<IHostEnvironment>(_env);
        services.TryAddSingleton<IMessageBus, NullMessageBus>();
        services.TryAddTransient(typeof(Lazy<>), typeof(LazyService<>));
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

            if (!_hasGame)
                GameHostBuildException.ThrowNoGameAddedException();

            AddComonServices(_services);
            _services.MakeReadOnly();

            var gameServices = _services.Clone();
            var provider = _serviceProviderFactory.CreateServiceProvider(_services);

            return ResolveHost(provider, gameServices, Properties);
    }

    private static GameHost ResolveHost(
        IServiceProvider provider, 
        IServiceCollection gameServices, 
        IDictionary<object, object> properties
    )
    {
        var env = provider.GetRequiredService<IHostEnvironment>();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("GameHost");
        var host = new GameHost(provider, gameServices, properties, env, logger);

        // Ensure that the startup service executor is resolved after the host is created
        host.Initialize();

        return host;
    }

    private sealed class LoggingBuilder(IServiceCollection services) : ILoggingBuilder
    {
        public IServiceCollection Services => services;
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

    [StackTraceHidden]
    public static void ThrowNoGameAddedException()
    {
        throw new GameHostBuildException(
            "No game has added to the host! " +
            "Did you call IGameHostBuilder.AddGame properly?"
        );
    }

    [StackTraceHidden]
    public static void ThrowGameAlreadyAddedException()
    {
        throw new InvalidOperationException("Game is already added to the host!");
    }
}

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection Clone(this IServiceCollection serviceCollection)
    {
        IServiceCollection clone = new ServiceCollection();
        foreach (var service in serviceCollection)
        {
            clone.Add(service);
        }
        return clone;
    }
}

public static class AssemblyExtensions
{
    public static bool IsDebugBuild(this Assembly assembly)
    {
        // Vytáhneme atribut z metadata assembly
        var attributes = assembly.GetCustomAttributes(typeof(DebuggableAttribute), false);

        if (attributes.Length == 0)
            return false;

        var debuggable = (DebuggableAttribute)attributes[0];

        // Zásadní rozdíl: Debug build má vždy zakázané kompilátorové optimalizace, 
        // aby se dalo krokovat řádek po řádku. Release je má povolené (tento flag chybí).
        return debuggable.DebuggingFlags.HasFlag(DebuggableAttribute.DebuggingModes.DisableOptimizations);
    }
}

public static class HostDefaults
{
    /// <summary>
    /// The configuration key used to set <see cref="IHostEnvironment.ApplicationName"/>.
    /// </summary>
    public static readonly string ApplicationKey = "applicationName";

    /// <summary>
    /// The configuration key used to set <see cref="IHostEnvironment.EnvironmentName"/>.
    /// </summary>
    public static readonly string EnvironmentKey = "environment";

    /// <summary>
    /// The configuration key used to set <see cref="IHostEnvironment.ContentRootPath"/>
    /// and <see cref="IHostEnvironment.ContentRootFileProvider"/>.
    /// </summary>
    public static readonly string ContentRootKey = "contentRoot";
}
