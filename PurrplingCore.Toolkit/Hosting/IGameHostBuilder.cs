using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Toolkit.DI;

namespace PurrplingCore.Toolkit.Hosting;

public interface IGameHostBuilder : IApplicationHostBuilder
{
    IGameFeatureCollection GameFeatures { get; }

    /// <summary>
    /// Registers the game instance and its metadata.
    /// This defines the identity of the host, applies any
    /// <see cref="GameHostServicesAttribute"/> modules directly to <see cref="Services"/>,
    /// and must be called before building.
    /// </summary>
    /// <typeparam name="TGame">The type of the game class.</typeparam>
    IGameHostBuilder AddGame<TGame>() where TGame : class, IGame;

    /// <summary>
    /// Sets the factory to be used for creating the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="factory">The service provider factory to use.</param>
    new IGameHostBuilder UseServiceProviderFactory<TContainer>(IServiceProviderFactory<TContainer> factory)
        where TContainer : notnull;

    /// <summary>
    /// Builds the <see cref="GameHost"/> instance by finalizing service registrations and initializing plugins.
    /// </summary>
    /// <returns>A fully configured and initialized <see cref="GameHost"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the host is built more than once.</exception>
    new GameHost Build();
}
