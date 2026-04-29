using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Ecs.DI;

public delegate void WorldRegistryConfigurator(SystemRegistry registry);

internal sealed class DefaultWorldBuilder(IServiceCollection services, WorldType worldType) : IWorldBuilder
{
    public IServiceCollection Services { get; } = services;
    public WorldType WorldType { get; } = worldType;
}
