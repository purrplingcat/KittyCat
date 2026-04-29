using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Extensions;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Ecs.Systems.Builder;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Metadata;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static PurrplingCore.Ecs.SortedSystemSet;

namespace PurrplingCore.Ecs;

public interface IWorldBootstrap
{
    int Order { get; }
    public void Setup(ManagedWorld world);
}

public delegate void InGroupAction<T>(RuntimeGroup<T> builder) where T : SystemGroup;
public delegate void WorldBuildAction(RuntimeWorldBuilder builder);

public readonly struct RuntimeWorldBuilder
{
    private readonly ManagedWorld _world;

    internal RuntimeWorldBuilder(ManagedWorld world)
    {
        _world = world;
    }

    public void InUpdate(InGroupAction<UpdateSystemGroup> configure) =>
        configure(new RuntimeGroup<UpdateSystemGroup>(_world, _world.UpdateSystems));

    public void InDraw(InGroupAction<DrawSystemGroup> configure) =>
        configure(new RuntimeGroup<DrawSystemGroup>(_world, _world.DrawSystems));

    public void InInitialize(InGroupAction<InitializeSystemGroup> configure) =>
        configure(new RuntimeGroup<InitializeSystemGroup>(_world, _world.InitializeSystems));

    public void InFixedUpdate(InGroupAction<FixedUpdateSystemGroup> configure) =>
        configure(new RuntimeGroup<FixedUpdateSystemGroup>(_world, _world.FixedUpdateSystems));

    public void InGroup(string groupName, InGroupAction<SystemGroup> configure)
    {
        var rootBuilder = new RuntimeGroup<SystemRoot>(_world, _world.SystemRoot);
        rootBuilder.InGroup(groupName, configure);
    }

    public void InGroup<TGroup>(InGroupAction<TGroup> configure) where TGroup : SystemGroup
    {
        var rootBuilder = new RuntimeGroup<SystemRoot>(_world, _world.SystemRoot);
        rootBuilder.InGroup(configure);
    }

    public bool HasTopLevelGroup<TGroup>() where TGroup : SystemGroup
    {
        var rootBuilder = new RuntimeGroup<SystemRoot>(_world, _world.SystemRoot);

        return rootBuilder.Has<TGroup>();
    }

    public void AddTopLevelGroup<TGroup>() where TGroup : SystemGroup
    {
        var rootBuilder = new RuntimeGroup<SystemRoot>(_world, _world.SystemRoot);

        if (rootBuilder.Has<TGroup>())
        {
            throw new InvalidOperationException(
                $"Group '{typeof(TGroup)}' is already exists in system root."
            );
        }

        rootBuilder.AddGroup<TGroup>();
    }

    public void InStore(Action<EntityStore> configure)
    {
        configure(_world.Store);
    }

    public void Configure(Action<ManagedWorld> configure)
    {
        configure(_world);
    }
}

public abstract class RuntimeBootstrap : IWorldBootstrap
{
    public virtual int Order => 0;

    protected abstract void OnSetup(RuntimeWorldBuilder builder);

    public void Setup(ManagedWorld world)
    {
        OnSetup(new RuntimeWorldBuilder(world));
    }
}

internal sealed class DelegateRuntimeBootstrap(WorldBuildAction configure) : RuntimeBootstrap
{
    protected override void OnSetup(RuntimeWorldBuilder builder)
    {
        configure(builder);
    }
}

public readonly struct RuntimeGroup<TGroup> where TGroup : SystemGroup
{
    private readonly ManagedWorld _world;
    private readonly SystemGroup _targetGroup;

    internal RuntimeGroup(ManagedWorld world, SystemGroup targetGroup)
    {
        _world = world;
        _targetGroup = targetGroup;
    }

    public RuntimeGroup<TGroup> Add<TSystem>() where TSystem : BaseSystem
    {
        _targetGroup.Add(_world.CreateSystem<TSystem>());
        return this;
    }

    public RuntimeGroup<TGroup> Add<TSystem>(Func<IServiceProvider, TSystem> factory) where TSystem : BaseSystem
    {
        _targetGroup.Add(factory(_world.Services));
        return this;
    }

    public RuntimeGroup<TGroup> Add(BaseSystem system)
    {
        _targetGroup.Add(system);
        return this;
    }

    public RuntimeGroup<TGroup> AddBefore<TTarget, TNew>() where TTarget : BaseSystem where TNew : BaseSystem
    {
        _targetGroup.AddBefore<TTarget>(_world.CreateSystem<TNew>());
        return this;
    }

    public RuntimeGroup<TGroup> AddBefore<TTarget>(Func<IServiceProvider, BaseSystem> factory) where TTarget : BaseSystem
    {
        _targetGroup.AddBefore<TTarget>(factory(_world.Services));
        return this;
    }
    public RuntimeGroup<TGroup> AddBefore<TTarget>(BaseSystem newSystem) where TTarget : BaseSystem
    {
        _targetGroup.AddBefore<TTarget>(newSystem);
        return this;
    }

    public RuntimeGroup<TGroup> AddAfter<TTarget, TNew>() where TTarget : BaseSystem where TNew : BaseSystem
    {
        _targetGroup.AddAfter<TTarget>(_world.CreateSystem<TNew>());
        return this;
    }

    public RuntimeGroup<TGroup> AddAfter<TTarget>(Func<IServiceProvider, BaseSystem> factory) where TTarget : BaseSystem
    {
        _targetGroup.AddAfter<TTarget>(factory(_world.Services));
        return this;
    }

    public RuntimeGroup<TGroup> AddAfter<TTarget>(BaseSystem newSystem) where TTarget : BaseSystem
    {
        _targetGroup.AddAfter<TTarget>(newSystem);
        return this;
    }

    public void Insert(int index, BaseSystem system)
    {
        if (index < 0 || index > _targetGroup.ChildSystems.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        _targetGroup.Insert(index, system);
    }

    public void Insert<T>(int index) where T : BaseSystem
    {
        Insert(index, _world.CreateSystem<T>());
    }

    public void Insert<T>(int index, Func<IServiceProvider, BaseSystem> factory) where T : BaseSystem
    {
        Insert(index, factory(_world.Services));
    }

    public void Replace<TTarget, TNew>() where TTarget : BaseSystem where TNew : BaseSystem
    {
        Replace<TTarget>(_world.CreateSystem<TNew>());
    }

    public void Replace<TTarget>(Func<IServiceProvider, BaseSystem> factory) where TTarget : BaseSystem
    {
        Replace<TTarget>(factory(_world.Services));
    }

    public void Replace<TTarget>(BaseSystem newSystem) where TTarget : BaseSystem
    {
        var target = _targetGroup.FindSystem<TTarget>(recursive: false) 
            ?? throw new InvalidOperationException($"System {typeof(TTarget).Name} not found for replacement.");
        Replace(target, newSystem);
    }

    public void Replace(Type target, BaseSystem newSystem)
    {
        var targetSystem = _targetGroup.FindSystem(target, recursive: false) 
            ?? throw new InvalidOperationException($"System of type {target.Name} not found for replacement.");
        Replace(targetSystem, newSystem);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Replace(BaseSystem targetSystem, BaseSystem newSystem)
    {
        ArgumentNullException.ThrowIfNull(targetSystem);
        ArgumentNullException.ThrowIfNull(newSystem);

        int index = _targetGroup.ChildSystems.IndexOf(targetSystem);
        _targetGroup.Remove(targetSystem);
        _targetGroup.Insert(index, newSystem);
    }

    public void Remove<TSystem>() where TSystem : BaseSystem
    {
        var target = _targetGroup.FindSystem<TSystem>(recursive: false);
        if (target is not null)
            _targetGroup.Remove(target);
    }

    public bool Has<TSystem>() where TSystem : BaseSystem
    {
        return _targetGroup.FindSystem<BaseSystem>(recursive: false) != null;
    }

    public bool HasGroup(string name)
    {
        return _targetGroup.FindGroup(name, recursive: false) != null;
    }

    public void AddGroup(string name, InGroupAction<SystemGroup> configureGroup)
    {
        var group = new SystemGroup(name);
        _targetGroup.Add(group);

        configureGroup(new RuntimeGroup<SystemGroup>(_world, group));
    }

    public RuntimeGroup<TGroup> AddGroup(string name)
    {
        AddGroup(name, static _ => { });
        return this;
    }

    public void AddGroup<TSubGroup>(InGroupAction<SystemGroup> configureGroup) where TSubGroup : SystemGroup
    {
        var subGroup = _world.CreateSystem<TSubGroup>();
        _targetGroup.Add(subGroup);

        configureGroup(new RuntimeGroup<SystemGroup>(_world, subGroup));
    }

    public RuntimeGroup<TGroup> AddGroup<TSubGroup>() where TSubGroup : SystemGroup
    {
        AddGroup<TSubGroup>(static _ => { });
        return this;
    }

    public void GetOrCreateGroup(string name, InGroupAction<SystemGroup> configureGroup)
    {
        var subGroup = _targetGroup.FindGroup(name, recursive: false);

        if (subGroup == null)
        {
            subGroup = new SystemGroup(name);
            _targetGroup.Add(subGroup);
        }

        configureGroup(new RuntimeGroup<SystemGroup>(_world, subGroup));
    }

    public void GetOrCreateGroup<TSubGroup>(InGroupAction<TSubGroup> configureGroup) where TSubGroup : SystemGroup
    {
        var subGroup = _targetGroup.FindSystem<TSubGroup>(recursive: false);

        if (subGroup == null)
        {
            subGroup = _world.CreateSystem<TSubGroup>();
            _targetGroup.Add(subGroup);
        }

        configureGroup(new RuntimeGroup<TSubGroup>(_world, subGroup));
    }

    public void InGroup(string name, InGroupAction<SystemGroup> configureGroup)
    {
        var subGroup = _targetGroup.FindGroup(name, recursive: false) 
            ?? throw new InvalidOperationException($"Group '{name}' not found in '{_targetGroup.Name}'");

        configureGroup(new RuntimeGroup<SystemGroup>(_world, subGroup));
    }

    public void InGroup<TSubGroup>(InGroupAction<TSubGroup> configureGroup) 
        where TSubGroup : SystemGroup
    {
        var subGroup = _targetGroup.FindSystem<TSubGroup>(recursive: false)
            ?? throw new InvalidOperationException($"Group '{typeof(TGroup)}' not found in '{_targetGroup.Name}'");

        configureGroup(new RuntimeGroup<TSubGroup>(_world, subGroup));
    }

    public void Configure<TSystem>(Action<TSystem, ManagedWorld> configure) where TSystem : BaseSystem
    {
        var system = _targetGroup.GetSystem<TSystem>(recursive: false);
        configure(system, _world);
    }

    public void Configure(Action<SystemGroup, ManagedWorld> configure)
    {
        configure(_targetGroup, _world);
    }

    public void ConfigureGroup(string name, Action<SystemGroup, ManagedWorld> configure)
    {
        var subGroup = _targetGroup.FindGroup(name, recursive: false)
            ?? throw new InvalidOperationException($"Group '{name}' not found in '{_targetGroup.Name}'");
        
        configure(subGroup, _world);
    }
}

public enum SystemOrder
{
    Default,
    First,
    Last,
}

public class SortedSystemSet()
{
    private readonly List<SystemEntry> _entries = [];
    private readonly List<SystemEntry> _firstOrdered = [];
    private readonly List<SystemEntry> _lastOrdered = [];
    private readonly HashSet<Type> _types = [];
    private SystemEntry[]? _sorted;

    public readonly record struct SystemEntry(Type Type, SystemOrder Order = SystemOrder.Default)
    {
        public readonly HashSet<Type> RunBefore { get; init; } = [];
        public readonly HashSet<Type> RunAfter { get; init; } = [];
    }

    public SortedSystemSet Add(SystemEntry entry)
    {
        if (!_types.Add(entry.Type)) 
            throw new InvalidOperationException($"System {entry.Type} is already added");

        switch (entry.Order)
        {
            case SystemOrder.First:
                _firstOrdered.Add(entry);
                break;
            case SystemOrder.Last:
                _lastOrdered.Add(entry);
                break;
            default:
                _entries.Add(entry);
                break;
        }

        _sorted = null; // Clear sorted systems cache
        return this;
    }

    public SortedSystemSet Add<TSystem>(SystemOrder order = SystemOrder.Default) where TSystem: BaseSystem
    {
        return Add(new SystemEntry(typeof(TSystem), order));
    }

    public static IReadOnlyList<SystemEntry> Sort(IEnumerable<SystemEntry> bucketEntries)
    {
        var entries = bucketEntries.ToList();
        if (entries.Count == 0) return [];

        var nodeMap = entries.ToDictionary(e => e.Type);
        var adjacencyList = entries.ToDictionary(e => e.Type, _ => new List<Type>());
        var inDegrees = entries.ToDictionary(e => e.Type, _ => 0);

        // Validate & Sestav hrany
        foreach (var entry in entries)
        {
            foreach (var target in entry.RunAfter)
            {
                if (nodeMap.ContainsKey(target))
                {
                    adjacencyList[target].Add(entry.Type);
                    inDegrees[entry.Type]++;
                }
            }
            foreach (var target in entry.RunBefore)
            {
                if (nodeMap.ContainsKey(target))
                {
                    adjacencyList[entry.Type].Add(target);
                    inDegrees[target]++;
                }
            }
        }

        var queue = new Queue<Type>();
        var sortedResult = new List<SystemEntry>();

        foreach (var kvp in inDegrees.Where(k => k.Value == 0))
            queue.Enqueue(kvp.Key);

        while (queue.Count > 0)
        {
            var currentKey = queue.Dequeue();
            sortedResult.Add(nodeMap[currentKey]);

            foreach (var neighbor in adjacencyList[currentKey])
            {
                inDegrees[neighbor]--;
                if (inDegrees[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }

        if (sortedResult.Count != entries.Count)
        {
            throw new InvalidOperationException($"Circular reference detected!");
        }

        return sortedResult;
    }

    public ReadOnlySpan<SystemEntry> GetUnsortedEntries()
    {
        int totalCount = _firstOrdered.Count + _entries.Count + _lastOrdered.Count;
        var result = new SystemEntry[totalCount];
        int offset = 0;

        _firstOrdered.CopyTo(result, offset);
        offset += _firstOrdered.Count;

        _entries.CopyTo(result, offset);
        offset += _entries.Count;

        _lastOrdered.CopyTo(result, offset);

        return result;
    }

    public ReadOnlySpan<SystemEntry> GetSortedEntries()
    {
        if (_sorted == null)
        {
            var sortedEntries = new List<SystemEntry>(_types.Count);
            sortedEntries.AddRange(Sort(_firstOrdered));
            sortedEntries.AddRange(Sort(_entries));
            sortedEntries.AddRange(Sort(_lastOrdered));
            _sorted = [.. sortedEntries];
        }

        return _sorted;
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

    public static SystemRegistry CreateWithDefaults()
    {
        var registry = new SystemRegistry();

        registry.GetOrCreate<SystemRoot>()
                .Add<InitializeSystemGroup>(SystemOrder.First)
                .Add<FixedUpdateSystemGroup>()
                .Add<UpdateSystemGroup>()
                .Add<DrawSystemGroup>(SystemOrder.Last);

        return registry;
    }
}

internal class SystemProvider(SystemRegistry registry, IServiceProvider services)
{
    private readonly Dictionary<Type, BaseSystem> _instances = [];

    public SystemProvider(SystemRegistry registry, IServiceProvider services, IEnumerable<BaseSystem> systems) 
        : this(registry, services)
    {
        _instances = systems.ToDictionary(s => s.GetType());
    }

    public SystemProvider(
        SystemRegistry registry, 
        IServiceProvider services, 
        Dictionary<Type, BaseSystem> predefinedInstances
    ) : this(registry, services)
    {
        _instances = predefinedInstances;
    }

    public BaseSystem Resolve(Type type)
    {
        if (_instances.TryGetValue(type, out var existingSystem))
        {
            return existingSystem;
        }

        var system = (BaseSystem)ActivatorUtilities.GetServiceOrCreateInstance(services, type);
        _instances.Add(type, system);

        if (system is SystemGroup group && registry.TryGet(type, out var sortedSet))
        {
            FillGroup(group, sortedSet);
            return group;
        }

        return system;
    }

    private void FillGroup(SystemGroup group, SortedSystemSet systemSet)
    {
        if (group.ChildSystems.Count > 0)
        {
            throw new InvalidOperationException(
                $"Group '{group.Name}' already has child systems, cannot fill it with deferred provider."
            );
        }

        var entries = systemSet.GetSortedEntries();

        for (int i = 0; i < entries.Length; i++)
        {
            var system = Resolve(entries[i].Type);

            if (system == group)
                throw new InvalidOperationException("Cannot add itself to group");

            group.Add(system);
        }
    }

    public void FillGroup(SystemGroup group, Type type)
    {
        if (registry.TryGet(type, out var systemSet))
        {
            FillGroup(group, systemSet);
        }
    }
}

public record struct SystemMetadata(
    Type SystemType,
    Type GroupType,
    Type[] RunBefore,
    Type[] RunAfter,
    Type[] TargetWorlds,
    SystemOrder Order
);

public interface ISystemDiscoverySource
{
    IEnumerable<SystemMetadata> GetMetadata();
}

internal sealed class SystemMetadataStore(
    IOptions<EcsOptions> options,
    IEnumerable<ISystemDiscoverySource> staticSources,
    ILogger<SystemMetadataStore> logger) : IStartupService
{
    private readonly List<SystemMetadata> _allDiscoveredSystems = [];
    private readonly HashSet<Type> _discoveredTypes = [];

    public int Order => -1000;

    private static SystemMetadata CreateSystemMetadata(Type type)
    {
        var groupAttr = type.GetCustomAttribute<SystemAttribute>();
        var groupType = groupAttr?.GroupType ?? typeof(UpdateSystemGroup);
        var order = groupAttr?.Order ?? SystemOrder.Default;
        var runBefore = type.GetCustomAttributes<RunBeforeAttribute>().Select(a => a.TargetType).ToArray();
        var runAfter = type.GetCustomAttributes<RunAfterAttribute>().Select(a => a.TargetType).ToArray();
        var targetWorlds = type.GetCustomAttributes<TargetWorldAttribute>().Select(a => a.WorldMarkerType).ToArray();
        
        return new SystemMetadata(type, groupType, runBefore, runAfter, targetWorlds, order);
    }

    public IEnumerable<SystemMetadata> GetSystemsForWorld(WorldType worldType)
    {
        bool isDefaultWorld = worldType == WorldType.Default;

        return _allDiscoveredSystems.Where(info =>
            info.TargetWorlds.Length == 0
                ? isDefaultWorld
                : info.TargetWorlds.Contains(worldType.MarkerType)
        );
    }

    public void OnStartup()
    {
        _allDiscoveredSystems.Clear();
        _discoveredTypes.Clear();

        DiscoverStaticSources();
        ScanAssemblies(options.Value.Assemblies);

        if (logger.IsEnabled(LogLevel.Trace))
        {
            foreach (var info in _allDiscoveredSystems)
            {
                logger.LogTrace(
                    "Discovered system: {SystemType}, Group: {GroupType}, Worlds: [{TargetWorlds}]",
                    info.SystemType.FullName,
                    info.GroupType.FullName,
                    string.Join(", ", info.TargetWorlds.Select(t => t.GetDisplayName()))
                );
            }
        }

        logger.LogInformation("Cached metadata for {Count} systems.", _allDiscoveredSystems.Count);
    }

    public void Add<T>() where T : class
    {
        var metadata = CreateSystemMetadata(typeof(T));

        if (_discoveredTypes.Add(metadata.SystemType))
        {
            _allDiscoveredSystems.Add(metadata);
            logger.LogDebug("Manually added system: {SystemType}", metadata.SystemType.FullName);
        }
    }

    private void DiscoverStaticSources()
    {
        logger.LogDebug("Adding systems from {Count} static sources...", staticSources.Count());

        foreach (var source in staticSources)
        {
            logger.LogTrace("Source: {Source}", source.GetType().FullName);
            _allDiscoveredSystems.AddRange(source.GetMetadata());
        }
    }

    private void ScanAssemblies(IReadOnlyCollection<Assembly> assembliesToScan)
    {
        logger.LogDebug("Scanning {Count} assemblies for systems...", assembliesToScan.Count);

        foreach (var assembly in assembliesToScan)
        {
            logger.LogTrace("Assembly: {Assembly}", assembly.GetName().Name);

            var systemTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(BaseSystem).IsAssignableFrom(t))
                .Where(t => t.IsDefined(typeof(SystemAttribute), false))
                .Distinct();

            foreach (var type in systemTypes)
            {
                // Skip already discovered types (can happen if multiple sources include the same assembly)
                if (!_discoveredTypes.Add(type)) continue;

                var systemInfo = CreateSystemMetadata(type);
                _allDiscoveredSystems.Add(systemInfo);
            }
        }
    }
}
