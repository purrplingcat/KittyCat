using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Modding;

namespace PurrplingCore.Toolkit.DI;

public interface IGameHostBuilder
{
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
}
