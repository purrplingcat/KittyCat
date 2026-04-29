using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PurrplingCore.Ecs.Diagnostics;
using PurrplingCore.Ecs.Extensions;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.Extensions;

namespace PurrplingCore.Ecs;

public class WorldManager(IWorldFactory factory, EcsOptions options, ILogger<WorldManager>? logger)
{
    private readonly Dictionary<string, ManagedWorld> _worldsByName = [];
    private readonly List<ManagedWorld> _worlds = [];
    private readonly EcsOptions _options = options;
    private readonly ILogger<WorldManager> _logger = logger ?? NullLogger<WorldManager>.Instance;
    private readonly object _lock = new();

    public IReadOnlyCollection<ManagedWorld> Worlds => _worlds.AsReadOnly();

    public ManagedWorld? GetWorld(string name) => _worldsByName.GetValueOrDefault(name);

    public IEnumerable<ManagedWorld> GetWorlds(WorldType tag)
    {
        return _worlds.Where(w => w.WorldType == tag);
    }

    public ManagedWorld CreateWorld(string? name = null)
    {
        return CreateWorld(WorldType.Default, name);
    }

    public ManagedWorld CreateWorld<T>(string? name = null) where T : struct, IWorldMarker
    {
        var worldType = WorldType.For<T>();
        return CreateWorld(worldType, name);
    }

    public ManagedWorld CreateWorld(WorldType worldType, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(worldType, nameof(worldType));
        name ??= $"World_{Guid.NewGuid():N}";

        if (ContainsWorld(name))
        {
            throw new InvalidOperationException($"A world with the name '{name}' already exists.");
        }

        var initOptions = options.GetWorldInitOptions(worldType);
        var world = factory.CreateWorld(name, worldType);
        var bootstraps = world.Services.GetKeyedServices<IWorldBootstrap>(worldType);

        if (initOptions.AutoCreateSystems)
        {
            SetupSystems(worldType, world);
        }

        ApplyBootstraps(world, bootstraps);
        AddWorld(world);

        // Logging the world state after creation and bootstrap application
        _logger.LogWorldTopology(world);
        _logger.LogInformation(
            "World '{Name}' created! Systems: {Systems} Entities: {Entities} Tag: {Tag}", 
            world.Name, world.SystemRoot.Count(recursive: true), world.Store.Count, world.WorldType
         );

        return world;
    }

    private static void SetupSystems(WorldType worldType, ManagedWorld world)
    {
        // Using SystemProvider to ensure that systems are created with DI support
        // and that group hierarchies are properly filled based on the registry
        // also using predefined instances from the world.SystemRoot.ChildSystems
        // to avoid creating multiple instances of the same system
        // if they are already created as top-level systems in the world
        // like default groups or manually added systems in the root group.
        var registry = world.Services.GetRequiredKeyedService<SystemRegistry>(worldType);
        var provider = new SystemProvider(registry, world.Services, world.SystemRoot.ChildSystems);
        foreach (var topLevelEntry in registry.GetOrCreate<SystemRoot>().GetSortedEntries())
        {
            var topLevel = provider.Resolve(topLevelEntry.Type);
            world.AddTopLevelSystem(topLevel);

            if (topLevel is SystemGroup group)
            {
                provider.FillGroup(group, group.GetType());
            }
        }
    }

    private void ApplyBootstraps(ManagedWorld world, IEnumerable<IWorldBootstrap> bootstraps)
    {
        try
        {
            world.creating = true;
            foreach (var bootstrap in bootstraps.OrderBy(bs => bs.Order))
            {
                bootstrap.Setup(world);
                _logger.LogDebug("Applied bootstrap {Bootstrap}", bootstrap.GetType());
            }
        }
        finally
        {
            world.creating = false;
        }
    }

    public void AddWorld(ManagedWorld world)
    {
        lock (_lock)
        {
            if (!string.IsNullOrEmpty(world.Name))
            {
                if (_worldsByName.ContainsKey(world.Name))
                {
                    throw new InvalidOperationException($"World '{world.Name}' already exists.");
                }
                _worldsByName.Add(world.Name, world);
            }
            _worlds.Add(world);
            world.Destroyed += OnWorldDisposed;
        }
    }

    public bool ContainsWorld(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;

        lock (_lock)
        {
            return _worldsByName.ContainsKey(name);
        }
    }

    public bool ContainsWorld(IWorld world)
    {
        if (world is not ManagedWorld managedWorld) return false;
        lock (_lock)
        {
            return _worlds.Contains(managedWorld);
        }
    }

    private void OnWorldDisposed(object? sender, EventArgs e)
    {
        if (sender is ManagedWorld world)
        {
            lock (_lock)
            {

                _worlds.Remove(world);
                if (!string.IsNullOrEmpty(world.Name))
                {
                    _worldsByName.Remove(world.Name);
                }
            }
        }
    }

    public bool DestroyWorld(string name)
    {
        lock (_lock)
        {
            if (_worldsByName.TryGetValue(name, out var world))
            {
                world.Dispose();
                return true;
            }

            return false;
        }
    }
}
