using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Stores and orders systems registered for one system group.
/// </summary>
public class SortedSystemSet(Type? key = null)
{
    private readonly List<SystemEntry> _entries = [];
    private readonly List<SystemEntry> _firstOrdered = [];
    private readonly List<SystemEntry> _lastOrdered = [];
    private readonly HashSet<Type> _types = [];
    private SystemEntry[]? _sorted;

    /// <summary>
    /// Gets the type used as this set's group key, if one was provided.
    /// </summary>
    public Type? Key { get; } = key;

    /// <summary>
    /// Describes a system and its ordering constraints within a group.
    /// </summary>
    public readonly record struct SystemEntry(Type Type, SystemOrder Order = SystemOrder.Default)
    {
        /// <summary>
        /// Gets the system types that must run after this system.
        /// </summary>
        public readonly HashSet<Type> RunBefore { get; init; } = [];

        /// <summary>
        /// Gets the system types that must run before this system.
        /// </summary>
        public readonly HashSet<Type> RunAfter { get; init; } = [];

        /// <summary>
        /// Gets the world flags required for this system to run.
        /// </summary>
        public readonly WorldFlags Flags { get; init; }
    }

    /// <summary>
    /// Adds an entry unless its system type is already registered.
    /// </summary>
    public SortedSystemSet Add(SystemEntry entry)
    {
        if (!_types.Add(entry.Type))
            return this;

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

        _sorted = null;
        return this;
    }

    /// <summary>
    /// Adds a system type with the specified ordering category.
    /// </summary>
    public SortedSystemSet Add<TSystem>(SystemOrder order = SystemOrder.Default) where TSystem : BaseSystem
    {
        return Add(new SystemEntry(typeof(TSystem), order));
    }

    /// <summary>
    /// Sorts entries according to their before and after constraints.
    /// </summary>
    public static IReadOnlyList<SystemEntry> Sort(IEnumerable<SystemEntry> bucketEntries)
    {
        var entries = bucketEntries.ToList();
        if (entries.Count == 0) return [];

        var nodeMap = entries.ToDictionary(entry => entry.Type);
        var adjacencyList = entries.ToDictionary(entry => entry.Type, _ => new List<Type>());
        var inDegrees = entries.ToDictionary(entry => entry.Type, _ => 0);

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

        foreach (var entry in inDegrees.Where(entry => entry.Value == 0))
            queue.Enqueue(entry.Key);

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
            throw new InvalidOperationException("Circular reference detected!");

        return sortedResult;
    }

    /// <summary>
    /// Gets entries grouped by their first, default, and last ordering categories.
    /// </summary>
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

    /// <summary>
    /// Gets the cached entries sorted within each ordering category.
    /// </summary>
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