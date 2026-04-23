using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Modding;

namespace PurrplingCore.Toolkit.DI;

public interface IGameHostBuilder
{
    /// <summary>
    /// Occurs when a new game host is created.
    /// </summary>
    /// <remarks>Subscribe to this event to be notified when a game host instance is initialized. The event
    /// provides information about the created game host through the <see cref="GameHostCreatedEventArgs"/>
    /// parameter.</remarks>
    event EventHandler<GameHostCreatedEventArgs> GameHostCreated;

    /// <summary>
    /// Přidá konfiguraci pro služby (IServiceCollection).
    /// </summary>
    IGameHostBuilder ConfigureServices(Action<IServiceCollection, GameHostBuilderContext> configureDelegate);

    /// <summary>
    /// Přidá konfiguraci pro logování (ILoggingBuilder).
    /// </summary>
    IGameHostBuilder ConfigureLogging(Action<ILoggingBuilder> configureDelegate);

    IGameHostBuilder UseServiceProviderFactory(IServiceProviderFactory factory);

    IGameHostBuilder AddPlugin(IGameHostPlugin feature);

    /// <summary>
    /// Sestaví a vrátí finální instanci GameHost.
    /// </summary>
    GameHost Build();
    IGameHostBuilder AddGame<TGame>() where TGame : Game;
}
