using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;

namespace PurrplingCore.Toolkit.Hosting;

public interface IGameHostBuilder
{
    /// <summary>
    /// Gets the service collection for immediate registration 
    /// of infrastructure and bootstrap services.
    /// </summary>
    IServiceCollection Services { get; }

    IConfigurationManager Configuration {  get; }

    IHostEnvironment Environment { get; }

    /// <summary>
    /// Logging builder
    /// </summary>
    ILoggingBuilder Logging { get; }

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
    IGameHostBuilder UseServiceProviderFactory<TContainer>(IServiceProviderFactory<TContainer> factory) 
        where TContainer : notnull;

    /// <summary>
    /// Builds the <see cref="GameHost"/> instance by finalizing service registrations and initializing plugins.
    /// </summary>
    /// <returns>A fully configured and initialized <see cref="GameHost"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the host is built more than once.</exception>
    GameHost Build();
}
