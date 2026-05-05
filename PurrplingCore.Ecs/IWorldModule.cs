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

public interface IWorldModule
{
    int Order { get; }
    public void Setup(IWorldBuilder builder);
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
        public readonly WorldFlags Flags { get; init; }
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



public readonly record struct SystemMetadata
{
    private static readonly ConcurrentDictionary<Type, SystemMetadata> _metadataCache = new();

    public Type SystemType { get; init; }
    public Type GroupType { get; init; }
    public Type[] RunBefore { get; init; }
    public Type[] RunAfter { get; init; }
    public Type[] TargetWorlds { get; init; }
    public WorldFlags Flags { get; init; }
    public SystemOrder Order { get; init; }

    private static SystemMetadata Create(Type type)
    {
        var groupAttr = type.GetCustomAttribute<SystemAttribute>();

        return new SystemMetadata()
        {
            SystemType = type,
            GroupType = groupAttr?.GroupType ?? typeof(UpdateSystemGroup),
            Order = groupAttr?.Order ?? SystemOrder.Default,
            RunBefore = [.. type.GetCustomAttributes<RunBeforeAttribute>().Select(a => a.TargetType)],
            RunAfter = [.. type.GetCustomAttributes<RunAfterAttribute>().Select(a => a.TargetType)],
            TargetWorlds = [.. type.GetCustomAttributes<TargetWorldAttribute>().Select(a => a.WorldMarkerType)],
            Flags = WorldFlags.None
        };
    }

    public static SystemMetadata For(Type systemType)
    {
        if (!_metadataCache.TryGetValue(systemType, out var metadata))
        {
            metadata = Create(systemType);
            _metadataCache[systemType] = metadata;
        }
        return metadata;
    }

    public static SystemMetadata For<TSystem>() where TSystem : BaseSystem
    {
            return For(typeof(TSystem));
    }
}

