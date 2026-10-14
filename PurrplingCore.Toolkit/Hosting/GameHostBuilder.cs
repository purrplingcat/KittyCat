using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting.Services;
using System.Diagnostics;
using System.Reflection;


namespace PurrplingCore.Toolkit.Hosting;

/// <summary>
/// Builds a GameHost by configuring the game instance, services, and game-specific features.
/// </summary>
/// <remarks>
/// GameHostBuilder extends ApplicationHostBuilder to provide game-specific configuration.
/// It manages game registration, feature collection, and service module discovery from game assemblies.
/// </remarks>
public class GameHostBuilder : ApplicationHostBuilder, IGameHostBuilder
{
    public const string GameTypePropertyKey = "GameType";

    private bool _hasGame;
    private readonly GameFeatureCollection _gameFeatures;
    private GameVersion _gameVersion = GameVersion.Empty;

    public IGameFeatureCollection GameFeatures => _gameFeatures;

    /// <summary>
    /// Initializes a new instance of GameHostBuilder with optional command-line arguments.
    /// </summary>
    /// <remarks>
    /// This constructor sets up the game feature collection and registers a crash logger
    /// for diagnostic purposes.
    /// </remarks>
    public GameHostBuilder(string[]? args = null) : base(args)
    {
        _gameFeatures = new GameFeatureCollection();
        if (!GameCrashLogger.IsRegistered)
        {
            var options = new GameHostCrashLoggerOptions()
            {
                BaseDirectory = Environment.BaseDirectory,
                WriteCrashLog = Environment.IsProduction()
            };
            GameCrashLogger.Register(options);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Registers the game instance and discovers its service modules.
    /// </summary>
    /// <remarks>
    /// This method must be called exactly once before building the host.
    /// It extracts game metadata, applies service modules from GameHostServicesAttribute,
    /// and registers the game assembly services.
    /// </remarks>
    /// <typeparam name="TGame">The game class type implementing IGame.</typeparam>
    /// <returns>The current builder instance for fluent configuration.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a game has already been added.</exception>
    public IGameHostBuilder AddGame<TGame>() where TGame : class, IGame
    {
        if (_hasGame)
        {
            GameHostBuildException.ThrowGameAlreadyAddedException();
        }

        _hasGame = true;
        _gameVersion = GameVersion.Of<TGame>();
        Environment.ApplicationName = _gameVersion.Name;
        // Add services discovered from the game assembly
        Services.AddServiceModule(new AssemblyServices(typeof(TGame).Assembly));
        // Add game-specific service modules
        Services.AddServiceModule<GameServices<TGame>>();

        ConfigureGameHost<TGame>();
        this.SetProperty(GameTypePropertyKey, typeof(TGame));

        return this;
    }

    /// <summary>
    /// Configures host services defined in GameHostServicesAttribute of the game type.
    /// </summary>
    /// <remarks>
    /// This method discovers and applies any GameHostServicesAttribute attributes on the game class,
    /// allowing the game to define its own hosting configuration.
    /// </remarks>
    private void ConfigureGameHost<TGame>() where TGame : class, IGame
    {
        // Apply all GameHostServicesAttribute configurations from the game type
        foreach (var attribute in typeof(TGame).GetCustomAttributes<GameHostServicesAttribute>())
        {
            this.AddGameHostModule(attribute.ConfigurationType, (IGameHostModule)Activator.CreateInstance(attribute.ConfigurationType)!);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Specifies the service provider factory to use for creating the IServiceProvider.
    /// </summary>
    /// <typeparam name="TContainerBuilder">The type of the service container builder.</typeparam>
    /// <param name="factory">The factory to use for creating service providers.</param>
    /// <returns>The current builder instance for fluent configuration.</returns>
    public new IGameHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        where TContainerBuilder : notnull
    {
        base.UseServiceProviderFactory(factory);
        return this;
    }

    /// <summary>
    /// Adds common game services and features to the service collection.
    /// </summary>
    /// <remarks>
    /// This is called during AddCommonServices to register game version and feature collection.
    /// </remarks>
    protected override void AddCommonServices(IServiceCollection services)
    {
        services.AddSingleton(_gameVersion);
        services.AddSingleton<IGameFeatureCollection>(_gameFeatures);

        base.AddCommonServices(services);
    }

    /// <summary>
    /// Build the <see cref="GameHost"/> instance with hosted game and services.
    /// This method can only be called once.
    /// </summary>
    /// <returns>A fully configured and initialized GameHost instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no game has been added or if Build() is called more than once.</exception>
    public override GameHost Build()
    {
        if (!_hasGame)
            GameHostBuildException.ThrowNoGameAddedException();

        return (GameHost)base.Build();
    }

    protected override ApplicationHost CreateHost(IServiceProvider provider, IServiceCollection applicationServices)
    {
        return new GameHost(provider, applicationServices, Environment, _gameVersion, Properties);
    }

    protected override void OnHostCreated(ApplicationHost host)
    {
        base.OnHostCreated(host);
        Properties[typeof(GameHost)] = host;
        // Freeze the feature collection to prevent modifications after host creation
        _gameFeatures.Freeze();
    }
}

/// <summary>
/// Thrown when the GameHost build process encounters errors, such as missing or duplicate game registration.
/// </summary>
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

    /// <summary>
    /// Throws an exception indicating no game has been added to the host.
    /// </summary>
    [StackTraceHidden]
    public static void ThrowNoGameAddedException()
    {
        throw new GameHostBuildException(
            "No game has added to the host! " +
            "Did you call IGameHostBuilder.AddGame properly?"
        );
    }

    /// <summary>
    /// Throws an exception indicating a game has already been added to the host.
    /// </summary>
    [StackTraceHidden]
    public static void ThrowGameAlreadyAddedException()
    {
        throw new InvalidOperationException("Game is already added to the host!");
    }
}

/// <summary>
/// Extension methods for cloning and inspecting service collections and assemblies.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Creates a shallow copy of the service collection.
    /// </summary>
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

/// <summary>
/// Extension methods for inspecting assembly compilation mode (Debug/Release).
/// </summary>
public static class AssemblyExtensions
{
    /// <summary>
    /// Determines if the assembly was built in Debug configuration.
    /// </summary>
    /// <remarks>
    /// An assembly is considered a debug build if it has the DebuggableAttribute
    /// with the DisableOptimizations flag set. This allows code paths to be stepped through line-by-line.
    /// </remarks>
    /// <returns>True if the assembly is a debug build; false for release builds.</returns>
    public static bool IsDebugBuild(this Assembly assembly)
    {
        // Retrieve the DebuggableAttribute from the assembly metadata
        var attributes = assembly.GetCustomAttributes(typeof(DebuggableAttribute), false);

        if (attributes.Length == 0)
            return false;

        var debuggable = (DebuggableAttribute)attributes[0];

        // Key difference: Debug builds have compiler optimizations disabled for line-by-line stepping.
        // Release builds have this flag absent/disabled.
        return debuggable.DebuggingFlags.HasFlag(DebuggableAttribute.DebuggingModes.DisableOptimizations);
    }
}

/// <summary>
/// Configuration key constants used by the hosting infrastructure.
/// </summary>
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
