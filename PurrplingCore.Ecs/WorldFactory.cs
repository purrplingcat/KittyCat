using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Ecs.Diagnostics;
using PurrplingCore.Ecs.Extensions;
using PurrplingCore.Toolkit.Extensions;
using System.Diagnostics.CodeAnalysis;

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
        var builder = new WorldBuilder(world, world.Services);
        var modules = world.Services.GetKeyedServices<IWorldModule>(signature.MarkerType);

        builder.ApplyModules(modules);

        return builder.Build();
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

public sealed record PreBuildContext(SystemRegistry Registry, IServiceProvider Services);
public sealed record InitializeContext(World World, IServiceProvider Services);
public sealed record TearDownContext(World World, IServiceProvider Services);
public sealed record PostBuildContext(World World, SystemProvider SystemProvider, IServiceProvider Services);

public interface IWorldBuilder
{
    SystemRegistry Registry { get; }
    WorldSignature WorldType { get; }
    IServiceProvider Services { get; }

    void OnPreBuild(Action<PreBuildContext> configure);
    void OnPostBuild(Action<PostBuildContext> configure);
    void OnInitialize(Action<InitializeContext> configure);
    void OnTearDown(Action<TearDownContext> configure);
}

public sealed class WorldBuilder : IWorldBuilder
{
    private readonly World _world;
    private readonly IServiceProvider _services;

    private readonly List<Action<PreBuildContext>> _preBuildActions = [];
    private readonly List<Action<PostBuildContext>> _postBuildActions = [];
    private readonly List<Action<InitializeContext>> _initActions = [];
    private readonly List<Action<TearDownContext>> _tearDownActions = [];

    public SystemRegistry Registry { get; } = new();
    public WorldSignature WorldType => _world.Signature;

    public IServiceProvider Services => _services;

    public WorldBuilder(World world, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(world, nameof(world));
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        _world = world;
        _services = services;
    }

    public void ApplyModules(IEnumerable<IWorldModule> modules)
    {
        foreach (var module in modules.OrderBy(m => m.Order))
        {
            module.Setup(this);
        }
    }

    private static void ExecuteHooks<TContext>(List<Action<TContext>> actions, TContext context)
    {
        foreach (var action in actions) 
            action(context);
    }

    private void AttachLifecycleHooks()
    {
        var initActions = _initActions.ToArray();
        var tearDownActions = _tearDownActions.ToArray();
        var services = _services;
        var world = _world;

        if (initActions.Length > 0)
        {
            world.Initialized += (_, _) =>
            {
                var context = new InitializeContext(world, services);
                foreach (var action in initActions) action(context);
            };
        }

        if (tearDownActions.Length > 0)
        {
            world.Destroyed += (_, _) =>
            {
                var context = new TearDownContext(world, services);
                foreach (var action in tearDownActions) action(context);
            };
        }
    }

    private void PopulateRootSystems(SystemProvider provider)
    {
        foreach (var rootSystem in _world.Systems)
        {
            if (rootSystem is SystemGroup group) 
                provider.Populate(group);
        }
    }

    private BaseSystem ResolveSystem(Type type)
    {
        if (_services is IKeyedServiceProvider keyedProvider)
        {
            if (keyedProvider.GetKeyedService(type, WorldType.MarkerType) is BaseSystem keyedSystem)
            {
                return keyedSystem;
            }
        }
    
        return (BaseSystem)ActivatorUtilities.GetServiceOrCreateInstance(_services, type);
    }

    public World Build()
    {
        var provider = new SystemProvider(Registry, ResolveSystem, WorldType);

        ExecuteHooks(_preBuildActions, new PreBuildContext(Registry, _services));
        PopulateRootSystems(provider);
        AttachLifecycleHooks();
        ExecuteHooks(_postBuildActions, new PostBuildContext(_world, provider, _services));

        return _world;
    }

    public void OnPreBuild(Action<PreBuildContext> configure)
    {
        _preBuildActions.Add(configure);
    }

    public void OnPostBuild(Action<PostBuildContext> configure)
    {
        _postBuildActions.Add(configure);
    }

    public void OnInitialize(Action<InitializeContext> configure)
    {
        _initActions.Add(configure);
    }

    public void OnTearDown(Action<TearDownContext> configure)
    {
        _tearDownActions.Add(configure);
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

    public void Populate(SystemGroup group)
    {
        var type = group.GetType();
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

    public SortedSystemSet GetOrCreate<TGroup>() where TGroup : SystemGroup
        => GetOrCreate(typeof(TGroup));

    public static SystemRegistry Create(IEnumerable<SystemMetadata> systems)
    {
        var registry = new SystemRegistry();

        foreach (var info in systems)
        {
            var entry = new SortedSystemSet.SystemEntry(info.SystemType, info.Order);

            entry.RunBefore.UnionWith(info.RunBefore);
            entry.RunAfter.UnionWith(info.RunAfter);

            registry.GetOrCreate(info.GroupType).Add(entry);
        }

        return registry;
    }
}

public static class SystemRegistryExtensions
{
    /// <summary>
    /// Registers a system of type <typeparamref name="TSystem"/> in the appropriate system group based on its metadata.
    /// </summary>
    public static SystemRegistry Add<TSystem>(this SystemRegistry registry) where TSystem : BaseSystem
    {
        var metadata = SystemMetadata.For<TSystem>();
        var entry = new SortedSystemSet.SystemEntry(metadata.SystemType, metadata.Order) 
        { 
            Flags = metadata.Flags,
            RunBefore = [.. metadata.RunBefore],
            RunAfter = [.. metadata.RunAfter]
        };

        registry.GetOrCreate(metadata.GroupType)
                .Add(entry);

        return registry;
    }
}
