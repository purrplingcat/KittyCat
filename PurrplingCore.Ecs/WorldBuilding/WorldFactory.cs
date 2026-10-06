using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Diagnostics;
using PurrplingCore.Ecs.Extensions;

namespace PurrplingCore.Ecs.WorldBuilding;

internal sealed class WorldFactory(IServiceProvider services, ILogger<WorldFactory> logger) : IWorldFactory
{
    public World CreateWorld(string defaultWorldName, WorldSignature signature)
    {
        var worldFactory = services.GetKeyedService<IWorldFactory>(signature.MarkerType)
            ?? throw new InvalidOperationException($"No world factory registered for type {signature.MarkerType.FullName}");

        var world = worldFactory.CreateWorld(defaultWorldName, signature);

        logger.LogWorldTopology(world);
        logger.LogInformation(
            "World '{Name}' created! Systems: {Systems} Entities: {Entities} Tag: {Tag}",
            world.Name, world.SystemRoot.Count(recursive: true), world.Store.Count, world.Signature
        );

        return world;
    }
}