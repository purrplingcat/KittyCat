using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PurrplingCore.Toolkit.DI;

public interface IGameHostBuilder
{
    /// <summary>
    /// Occurs when a new game host is created.
    /// </summary>
    /// <remarks>Subscribe to this event to be notified when a game host instance is initialized. The event
    /// provides information about the created game host through the <see cref="GameHostResolvedEventArgs"/>
    /// parameter.</remarks>
    event EventHandler<GameHostResolvedEventArgs> HostResolved;

    /// <summary>
    /// Occurs after all service configuration actions have been executed but before the service provider is created.
    /// </summary>
    /// <remarks>
    /// This is the final opportunity to modify the <see cref="IServiceCollection"/> before it is made read-only.
    /// Use this event to perform late-stage service validation or to inject last-minute dependencies.
    /// </remarks>
    event EventHandler<ServicesConfiguredEventArgs> ServicesConfigured;

    event EventHandler<BuildingEventArgs> Building;

    /// <summary>
    /// Gets the service collection for immediate registration 
    /// of infrastructure and bootstrap services.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// List of registered plugins in this builder.
    /// </summary>
    IReadOnlyList<IGameHostPlugin> Plugins { get; }

    /// <summary>
    /// Registers a configuration action for the application services.
    /// This is executed during <see cref="Build"/> after all plugins are initialized.
    /// Use this for late-bound services that depend on the final game state or mods.
    /// </summary>
    IGameHostBuilder ConfigureServices(Action<IServiceCollection, GameHostBuilderContext> configureDelegate);

    /// <summary>
    /// Registers a logging configuration action.
    /// This configuration is used twice: first to create a bootstrap logger for the <see cref="Build"/> 
    /// process (plugins, mod discovery) and later for the final application logger.
    /// </summary>
    IGameHostBuilder ConfigureLogging(Action<ILoggingBuilder> configureDelegate);

    /// <summary>
    /// Registers the game instance and its metadata.
    /// This defines the identity of the host and must be called before building.
    /// </summary>
    /// <typeparam name="TGame">The type of the game class.</typeparam>
    IGameHostBuilder AddGame<TGame>() where TGame : Game;

    /// <summary>
    /// Sets the factory to be used for creating the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="factory">The service provider factory to use.</param>
    IGameHostBuilder UseServiceProviderFactory(IServiceProviderFactory factory);

    /// <summary>
    /// Adds a plugin to the host builder. Plugins are executed early during the <see cref="Build"/> process,
    /// allowing them to contribute to the service collection before final configuration.
    /// </summary>
    /// <param name="plugin">The plugin instance to add.</param>
    IGameHostBuilder AddPlugin(IGameHostPlugin feature);

    /// <summary>
    /// Builds the <see cref="GameHost"/> instance by finalizing service registrations and initializing plugins.
    /// </summary>
    /// <returns>A fully configured and initialized <see cref="GameHost"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the host is built more than once.</exception>
    GameHost Build();
}
