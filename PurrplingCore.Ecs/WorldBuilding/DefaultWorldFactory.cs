using System.Diagnostics;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Creates managed worlds and composes their default groups and registered modules.
/// </summary>
public sealed class DefaultWorldFactory(IServiceScopeFactory scopeFactory, ILogger<DefaultWorldFactory> logger) : IWorldFactory
{
    /// <summary>
    /// Creates a managed world and composes its registered systems and modules.
    /// </summary>
    public World CreateWorld(string name, WorldSignature signature)
    {
        var watch = Stopwatch.StartNew();
        var world = ManagedWorld.Create(scopeFactory, name, signature);

        try
        {
            var blueprint = new WorldBlueprint();
            var modules = world.Services.GetKeyedServices<IWorldModule>(signature.MarkerType);

            ConfigureDefaults(blueprint);
            ApplyModules(blueprint, modules);
            blueprint.CreateComposer(world, world.Services).Compose();

            watch.Stop();
            logger.LogDebug("World '{WorldName}' creation took {ElapsedMilliseconds} ms", name, watch.ElapsedMilliseconds);
            
            return world;
        }
        catch
        {
            watch.Stop();
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
            module.Setup(blueprint);
        }
    }
}
