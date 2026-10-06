using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Creates managed worlds and composes their default groups and registered modules.
/// </summary>
public sealed class DefaultWorldFactory(IServiceScopeFactory scopeFactory) : IWorldFactory
{
    /// <summary>
    /// Creates a managed world and composes its registered systems and modules.
    /// </summary>
    public World CreateWorld(string name, WorldSignature signature)
    {
        var world = ManagedWorld.Create(scopeFactory, name, signature);

        try
        {
            var blueprint = new WorldBlueprint();
            var modules = world.Services.GetKeyedServices<IWorldModule>(signature.MarkerType);

            ConfigureDefaults(blueprint);
            ApplyModules(blueprint, modules);
            blueprint.CreateComposer(world, world.Services).Compose();

            return world;
        }
        catch
        {
            world.Dispose();
            throw;
        }
    }

    private static void ConfigureDefaults(IWorldBlueprint blueprint)
    {
        blueprint
            .AddRootGroup<InitializeSystemGroup>()
            .AddRootGroup<FixedUpdateSystemGroup>()
            .AddRootGroup<UpdateSystemGroup>()
            .AddRootGroup<DrawSystemGroup>();
    }

    internal static void ApplyModules(IWorldBlueprint blueprint, IEnumerable<IWorldModule> modules)
    {
        foreach (var module in modules.OrderBy(module => module.Order))
        {
            ArgumentNullException.ThrowIfNull(module);
            module.Setup(blueprint);
        }
    }
}