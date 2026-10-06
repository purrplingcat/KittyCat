using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Ecs;

namespace PurrplingCore.Ecs.WorldBuilding;

internal sealed class DeriveWorldModule(
    IServiceProvider provider,
    WorldSignature signature,
    int order = 0) : IWorldModule
{
    private readonly IServiceProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    private readonly WorldSignature _signature = signature;

    public int Order { get; } = order;

    public void Setup(IWorldBlueprint world)
    {
        var modules = _provider.GetKeyedServices<IWorldModule>(_signature.MarkerType);
        DefaultWorldFactory.ApplyModules(world, modules);
    }
}