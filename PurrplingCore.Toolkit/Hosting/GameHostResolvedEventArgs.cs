using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Toolkit.Hosting;

public class GameHostResolvedEventArgs(GameHost host) : EventArgs
{
    public GameHost GameHost => host;
}

public class ServicesConfiguredEventArgs(IServiceCollection services, GameHostBuilderContext context) : EventArgs
{
    public IServiceCollection Services => services;
    public GameHostBuilderContext Context => context;
}

public class BuildingEventArgs(IGameHostBuilder builder, GameHostBuilderContext context) : EventArgs
{
    public IGameHostBuilder Builder => builder;
    public GameHostBuilderContext Context => context;
}
