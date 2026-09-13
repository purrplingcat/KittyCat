using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Ecs.Diagnostics;
using PurrplingCore.Ecs.Extensions;
using PurrplingCore.Ecs.Systems;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace PurrplingCore.Ecs;

public interface IWorldFactory
{
    World CreateWorld(string defaultWorldName, WorldSignature signature);
}

public static class WorldFactoryExtensions
{
    public static World CreateWorld(this IWorldFactory factory, WorldSignature signature)
    {
        string randomName = $"World_{Guid.NewGuid().ToString("N")[..12]}";
        return factory.CreateWorld(randomName, signature);
    }

    public static World CreateWorld<T>(this IWorldFactory factory, string name) where T : struct, IWorldMarker
    {
        var signature = WorldSignature.For<T>();
        return factory.CreateWorld(name, signature);
    }

    public static World CreateWorld<T>(this IWorldFactory factory) where T : struct, IWorldMarker
    {
        var signature = WorldSignature.For<T>();
        return factory.CreateWorld(signature);
    }

    public static World CreateWorld<T>(this IWorldFactory factory, WorldFlags flags) where T : struct, IWorldMarker
    {
        var signature = WorldSignature.For<T>().WithFlags(flags);
        return factory.CreateWorld(signature);
    }

    public static World CreateWorld<T>(this IWorldFactory factory, string name, WorldFlags flags) where T : struct, IWorldMarker
    {
        var signature = WorldSignature.For<T>().WithFlags(flags);
        return factory.CreateWorld(name, signature);
    }
}

public sealed class DefaultWorldFactory(IServiceScopeFactory scopeFactory) : IWorldFactory
{
    public World CreateWorld(string name, WorldSignature signature)
    {
        var world = ManagedWorld.Create(scopeFactory, name, signature);

        try
        {
            var blueprint = new WorldBlueprint();
            var modules = world.Services.GetKeyedServices<IWorldModule>(signature.MarkerType);

            ConfigureDefaults(blueprint);
            ApplyModules(blueprint, modules);

            blueprint.CreateComposer(world, world.Services)
                     .Compose();

            return world;
        }
        catch
        {
            world.Dispose();
            throw;
        }
    }

    private void ConfigureDefaults(IWorldBlueprint blueprint)
    {
        blueprint.Systems
            .AddRoot<InitializeSystemGroup>()
            .AddRoot<FixedUpdateSystemGroup>()
            .AddRoot<UpdateSystemGroup>()
            .AddRoot<DrawSystemGroup>();
    }

    private static void ApplyModules(IWorldBlueprint blueprint, IEnumerable<IWorldModule> modules)
    {
        foreach (var module in modules.OrderBy(module => module.Order))
        {
            ArgumentNullException.ThrowIfNull(module);
            module.Setup(blueprint);
        }
    }
}

internal sealed class WorldFactory(IServiceProvider services, ILogger<WorldFactory> logger) : IWorldFactory
{
    public World CreateWorld(string defaultWorldName, WorldSignature signature)
    {
        var worldFactory = services.GetKeyedService<IWorldFactory>(signature.MarkerType) 
            ?? throw new InvalidOperationException(
                $"No world factory registered for type {signature.MarkerType.FullName}"
            );

        var world = worldFactory.CreateWorld(defaultWorldName, signature);

        logger.LogWorldTopology(world);
        logger.LogInformation(
            "World '{Name}' created! Systems: {Systems} Entities: {Entities} Tag: {Tag}",
            world.Name, world.SystemRoot.Count(recursive: true), world.Store.Count, world.Signature
        );

        return world;
    }
}

public sealed class SystemProvider(
    SystemRegistry registry, 
    Func<Type, BaseSystem> resolver, 
    WorldSignature signature)
{
    private readonly Dictionary<Type, BaseSystem> _instances = [];

    public WorldSignature Signature => signature;

    public BaseSystem Provide(Type type)
    {
        if (_instances.TryGetValue(type, out var existingSystem))
        {
            return existingSystem;
        }

        var system = resolver.Invoke(type);
        _instances.Add(type, system);

        if (system is SystemGroup group)
        {
            Populate(group);
        }

        return system;
    }

    public void Populate(SystemGroup group, Type? type = null)
    {
        type ??= group.GetType();
        if (!registry.TryGet(type, out var systemSet)) return;

        var entries = systemSet.GetSortedEntries();
        for (int i = 0; i < entries.Length; i++)
        {
            if (!signature.HasAnyFlag(entries[i].Flags)) 
                continue;

            var system = Provide(entries[i].Type);

            if (system == null) 
                continue;
            if (system == group)
                throw new InvalidOperationException("Cannot add itself to group");
            
            group.Add(system);
        }
    }
}

public class SystemRegistry
{
    private readonly Dictionary<Type, SortedSystemSet> _groups = [];

    public SortedSystemSet this[Type key] => _groups[key];

    public SortedSystemSet GetOrCreate(Type key)
    {
        if (!_groups.TryGetValue(key, out var systemSet))
        {
            systemSet = new SortedSystemSet();
            _groups[key] = systemSet;
        }

        return systemSet;
    }


    public bool TryGet(Type key, [MaybeNullWhen(false)] out SortedSystemSet systemSet)
    {
        return _groups.TryGetValue(key, out systemSet);
    }

    public static SystemRegistry Create(IEnumerable<SystemMetadata> systems)
    {
        var registry = new SystemRegistry();

        foreach (var info in systems)
        {
            var entry = new SortedSystemSet.SystemEntry(info.SystemType, info.Order)
            {
                Flags = info.Flags,
                RunBefore = [.. info.RunBefore],
                RunAfter = [.. info.RunAfter]
            };

            registry.GetOrCreate(info.GroupType).Add(entry);
        }

        return registry;
    }
}

public static class SystemRegistryExtensions
{
    /// <summary>
    /// Adds all concrete systems from an assembly using their declared system metadata.
    /// </summary>
    public static SystemRegistry AddFromAssembly(this SystemRegistry systems, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(systems);
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var typeInfo in assembly.DefinedTypes)
        {
            if (typeInfo.IsAbstract || typeInfo.ContainsGenericParameters || !typeof(BaseSystem).IsAssignableFrom(typeInfo))
            {
                continue;
            }

            systems.Register(typeInfo.AsType());
        }

        return systems;
    }

    /// <summary>
    /// Gets the manual composition set for a Friflo system group.
    /// Systems added to the returned set are placed directly in that group.
    /// </summary>
    public static SortedSystemSet In<TGroup>(this SystemRegistry systems) where TGroup : SystemGroup
    {
        ArgumentNullException.ThrowIfNull(systems);
        return systems.GetOrCreate(typeof(TGroup));
    }

    /// <summary>
    /// Registers a system of type <typeparamref name="TSystem"/> in the appropriate system group based on its metadata.
    /// </summary>
    public static SystemRegistry Add<TSystem>(this SystemRegistry registry) where TSystem : BaseSystem
    {
        var metadata = SystemMetadata.For<TSystem>();
        var entry = metadata.ToSystemEntry();

        registry.GetOrCreate(metadata.GroupType)
                .Add(entry);

        return registry;
    }

    public static SystemRegistry Add<TSystem>(this SystemRegistry registry, SystemMetadata systemMetadata) where TSystem : BaseSystem
    {
        var entry = systemMetadata.ToSystemEntry();

        registry.GetOrCreate(systemMetadata.GroupType)
                .Add(entry);

        return registry;
    }

    public static SystemRegistry AddRoot<TSytemGroup>(this SystemRegistry registry) where TSytemGroup : SystemGroup
    {
        var metadata = SystemMetadata.For<TSytemGroup>();
        var entry = metadata.ToSystemEntry();

        registry.GetOrCreate(typeof(SystemRoot))
                .Add(entry);

        return registry;
    }

    /// <summary>
    /// Registers the specified system type in its metadata-defined system group.
    /// </summary>
    /// <param name="registry">The system registry to update.</param>
    /// <param name="systemType">The type of system to register.</param>
    /// <returns>The updated system registry.</returns>
    public static SystemRegistry Register(this SystemRegistry registry, Type systemType)
    {
        var metadata = SystemMetadata.For(systemType);
        var entry = metadata.ToSystemEntry();

        registry.GetOrCreate(metadata.GroupType)
                .Add(entry);

        return registry;
    }

    public static TSystem Provide<TSystem>(this SystemProvider provider) where TSystem : BaseSystem
    {
        return (TSystem)provider.Provide(typeof(TSystem));
    }
}
