using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Toolkit.DI;
using System.Reflection;

namespace PurrplingCore.Toolkit.Hosting.Services;

internal sealed class GameServices<TGame>(IServiceProvider provider) : IServiceModule where TGame : class, IGame
{
    public void ConfigureServices(IServiceCollection services)
    {
        var gameType = typeof(TGame);
        if (services.Any(service => service.ServiceType == gameType))
        {
            throw new InvalidOperationException($"A service of type '{typeof(TGame)}' is already registered. Only one game can be registered.");
        }

        // Add services declared by GameServiceAttribute on a game class
        AddEmbeddedServices(services); 

        services.AddSingleton<TGame>() // Main game service singleton
                .AddAlias<IGame, TGame>()
                .AddAliases(gameType, gameType.GetServiceTypeAliases());
    }

    private void AddEmbeddedServices(IServiceCollection services)
    {
        var gameType = typeof(TGame);
        var servicesAttrs = gameType.GetCustomAttributes<GameServicesAttribute>();

        foreach (var attr in servicesAttrs)
        {
            var serviceConfiguration = (IServiceModule)ActivatorUtilities.CreateInstance(provider, attr.ConfigurationType);
            serviceConfiguration.ConfigureServices(services);
        }
    }
}

internal sealed class LambdaServices(IServiceProvider provider, Action<IServiceCollection, IServiceProvider> configure) : IServiceModule
{
    public void ConfigureServices(IServiceCollection services) => configure(services, provider);
}
