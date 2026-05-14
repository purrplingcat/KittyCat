using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Toolkit.DI.Configuration;

public sealed class GameServices<TGame>(IServiceProvider provider) : IServicesConfiguration where TGame : Game, IGame
{
    public void ConfigureServices(IServiceCollection services)
    {
        if (services.Any(service => service.ServiceType == typeof(TGame)))
        {
            throw new InvalidOperationException($"A service of type '{typeof(TGame)}' is already registered. Only one game can be registered.");
        }

        // Add services declared by GameServiceAttribute on a game class
        AddEmbeddedServices(services); 

        services.AddSingleton<TGame>() // Main game service singleton
                .ExposeMonoGameService<IGraphicsDeviceService>()
                .ExposeMonoGameService<IGraphicsDeviceManager>()
                .ExposeMonoGameService<GraphicsDeviceManager>()
                .Expose((TGame game) => game.Services)
                .AddAlias<Microsoft.Xna.Framework.Game, TGame>()
                .AddAlias<IGame, TGame>()
                .AddAlias<Game, TGame>();
    }

    private void AddEmbeddedServices(IServiceCollection services)
    {
        var gameType = typeof(TGame);
        var servicesAttrs = gameType.GetCustomAttributes<GameServicesAttribute>();

        foreach (var attr in servicesAttrs)
        {
            var serviceConfiguration = (IServicesConfiguration)ActivatorUtilities.CreateInstance(provider, attr.ConfigurationType);
            serviceConfiguration.ConfigureServices(services);
        }
    }
}

internal sealed class LambdaServices(IServiceProvider provider, Action<IServiceCollection, IServiceProvider> configure) : IServicesConfiguration
{
    public void ConfigureServices(IServiceCollection services) => configure(services, provider);
}
