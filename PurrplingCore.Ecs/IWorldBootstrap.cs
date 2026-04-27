using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PurrplingCore.Ecs.Extensions;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Ecs.Systems.Builder;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static PurrplingCore.Ecs.SortedSystemSet;

namespace PurrplingCore.Ecs;

public interface IWorldBootstrap
{
    int Order { get; }
    public void Setup(ManagedWorld world);
}

public delegate void InGroupAction<T>(GroupBuilder<T> builder) where T : SystemGroup;

public sealed class WorldBuilder
{
    private readonly ManagedWorld _world;

    internal WorldBuilder(ManagedWorld world)
    {
        _world = world;
    }

    public void InUpdate(InGroupAction<UpdateSystemGroup> configure) =>
        configure(new GroupBuilder<UpdateSystemGroup>(_world, _world.UpdateSystems));

    public void InDraw(InGroupAction<DrawSystemGroup> configure) =>
        configure(new GroupBuilder<DrawSystemGroup>(_world, _world.DrawSystems));

    public void InInitialize(InGroupAction<InitializeSystemGroup> configure) =>
        configure(new GroupBuilder<InitializeSystemGroup>(_world, _world.InitializeSystems));

    public void InFixedUpdate(InGroupAction<FixedUpdateSystemGroup> configure) =>
        configure(new GroupBuilder<FixedUpdateSystemGroup>(_world, _world.FixedUpdateSystems));

    public void InGroup(string groupName, InGroupAction<SystemGroup> configure)
    {
        var rootBuilder = new GroupBuilder<SystemRoot>(_world, _world.SystemRoot);
        rootBuilder.InGroup(groupName, configure);
    }

    public void InGroup<TGroup>(InGroupAction<TGroup> configure) where TGroup : SystemGroup
    {
        var rootBuilder = new GroupBuilder<SystemRoot>(_world, _world.SystemRoot);
        rootBuilder.InGroup(configure);
    }

    public bool HasTopLevelGroup<TGroup>() where TGroup : SystemGroup
    {
        var rootBuilder = new GroupBuilder<SystemRoot>(_world, _world.SystemRoot);

        return rootBuilder.Has<TGroup>();
    }

    public void AddTopLevelGroup<TGroup>() where TGroup : SystemGroup
    {
        var rootBuilder = new GroupBuilder<SystemRoot>(_world, _world.SystemRoot);

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

public abstract class BootstrapBase : IWorldBootstrap
{
    public virtual int Order => 0;

    protected abstract void OnSetup(WorldBuilder builder);

    public void Setup(ManagedWorld world)
    {
        OnSetup(new WorldBuilder(world));
    }
}

internal sealed class DelegateBuilderBootstrap(Action<WorldBuilder> configure) : BootstrapBase
{
    protected override void OnSetup(WorldBuilder builder)
    {
        configure(builder);
    }
}

public readonly struct GroupBuilder<TGroup> where TGroup : SystemGroup
{
    private readonly ManagedWorld _world;
    private readonly SystemGroup _targetGroup;

    internal GroupBuilder(ManagedWorld world, SystemGroup targetGroup)
    {
        _world = world;
        _targetGroup = targetGroup;
    }

    public GroupBuilder<TGroup> Add<TSystem>() where TSystem : BaseSystem
    {
        _targetGroup.Add(_world.CreateSystem<TSystem>());
        return this;
    }

    public GroupBuilder<TGroup> Add<TSystem>(Func<IServiceProvider, TSystem> factory) where TSystem : BaseSystem
    {
        _targetGroup.Add(factory(_world.Services));
        return this;
    }

    public GroupBuilder<TGroup> Add(BaseSystem system)
    {
        _targetGroup.Add(system);
        return this;
    }

    public GroupBuilder<TGroup> AddBefore<TTarget, TNew>() where TTarget : BaseSystem where TNew : BaseSystem
    {
        _targetGroup.AddBefore<TTarget>(_world.CreateSystem<TNew>());
        return this;
    }

    public GroupBuilder<TGroup> AddBefore<TTarget>(Func<IServiceProvider, BaseSystem> factory) where TTarget : BaseSystem
    {
        _targetGroup.AddBefore<TTarget>(factory(_world.Services));
        return this;
    }
    public GroupBuilder<TGroup> AddBefore<TTarget>(BaseSystem newSystem) where TTarget : BaseSystem
    {
        _targetGroup.AddBefore<TTarget>(newSystem);
        return this;
    }

    public GroupBuilder<TGroup> AddAfter<TTarget, TNew>() where TTarget : BaseSystem where TNew : BaseSystem
    {
        _targetGroup.AddAfter<TTarget>(_world.CreateSystem<TNew>());
        return this;
    }

    public GroupBuilder<TGroup> AddAfter<TTarget>(Func<IServiceProvider, BaseSystem> factory) where TTarget : BaseSystem
    {
        _targetGroup.AddAfter<TTarget>(factory(_world.Services));
        return this;
    }

    public GroupBuilder<TGroup> AddAfter<TTarget>(BaseSystem newSystem) where TTarget : BaseSystem
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

        configureGroup(new GroupBuilder<SystemGroup>(_world, group));
    }

    public GroupBuilder<TGroup> AddGroup(string name)
    {
        AddGroup(name, static _ => { });
        return this;
    }

    public void AddGroup<TSubGroup>(InGroupAction<SystemGroup> configureGroup) where TSubGroup : SystemGroup
    {
        var subGroup = _world.CreateSystem<TSubGroup>();
        _targetGroup.Add(subGroup);

        configureGroup(new GroupBuilder<SystemGroup>(_world, subGroup));
    }

    public GroupBuilder<TGroup> AddGroup<TSubGroup>() where TSubGroup : SystemGroup
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

        configureGroup(new GroupBuilder<SystemGroup>(_world, subGroup));
    }

    public void GetOrCreateGroup<TSubGroup>(InGroupAction<TSubGroup> configureGroup) where TSubGroup : SystemGroup
    {
        var subGroup = _targetGroup.FindSystem<TSubGroup>(recursive: false);

        if (subGroup == null)
        {
            subGroup = _world.CreateSystem<TSubGroup>();
            _targetGroup.Add(subGroup);
        }

        configureGroup(new GroupBuilder<TSubGroup>(_world, subGroup));
    }

    public void InGroup(string name, InGroupAction<SystemGroup> configureGroup)
    {
        var subGroup = _targetGroup.FindGroup(name, recursive: false) 
            ?? throw new InvalidOperationException($"Group '{name}' not found in '{_targetGroup.Name}'");

        configureGroup(new GroupBuilder<SystemGroup>(_world, subGroup));
    }

    public void InGroup<TSubGroup>(InGroupAction<TSubGroup> configureGroup) 
        where TSubGroup : SystemGroup
    {
        var subGroup = _targetGroup.FindSystem<TSubGroup>(recursive: false)
            ?? throw new InvalidOperationException($"Group '{typeof(TGroup)}' not found in '{_targetGroup.Name}'");

        configureGroup(new GroupBuilder<TSubGroup>(_world, subGroup));
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
    None,
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

    public readonly record struct SystemEntry(Type Type, SystemOrder Order = SystemOrder.None)
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

    public SortedSystemSet Add<TSystem>(SystemOrder order = SystemOrder.None) where TSystem: BaseSystem
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

public class WorldSystemsOptions
{
    public HashSet<Type> CustomTopLevelSystems { get; } = [];
}

internal sealed class DeferredSystemBakeBootstrap(IOptionsSnapshot<WorldSystemsOptions> options) : IWorldBootstrap
{
    private readonly IOptionsSnapshot<WorldSystemsOptions> _options = options;

    public int Order => int.MinValue;

    public void Setup(ManagedWorld world)
    {
        var options = _options.Value; // TODO: Vyřešit tuhle blbost
        var registry = world.Services.GetRequiredKeyedService<SystemRegistry>(world.Tag);
        var provider = new DefferedSystemProvider(registry, world.Services);

        foreach (var custom in options.CustomTopLevelSystems)
        {
            world.AddTopLevelSystem(world.CreateSystem(custom));
        }

        foreach (var topLevel in world.SystemRoot.OfType<SystemGroup>())
        {
            var type = topLevel.GetType();
            provider.AddInstance(type, topLevel);
            provider.FillGroup(topLevel, type);
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
}

internal class DefferedSystemProvider(SystemRegistry registry, IServiceProvider services)
{
    private readonly Dictionary<Type, BaseSystem> _instances = [];

    public void AddInstance(Type type, BaseSystem instance)
    {
        _instances[type] = instance;
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
