namespace PurrplingCore.Ecs.Systems.Builder;

/// <summary>
/// Base record for an element declared in a system set.
/// </summary>
public abstract record SetElement(string Id);

/// <summary>
/// Declares a system type as an element of a system set.
/// </summary>
public record SystemEntry(string Id, Type SystemType) : SetElement(Id);

/// <summary>
/// Declares a reference to another system set.
/// </summary>
public record SetReference(string Id, string TargetSetId) : SetElement(Id);

/// <summary>
/// Stores named system sets and validates references between them.
/// </summary>
public class SystemConfiguration
{
    private readonly Dictionary<string, SystemSet> _sets = [];

    /// <summary>
    /// Creates and registers a new system set.
    /// </summary>
    public SystemSet CreateSet(string setId)
    {
        if (_sets.ContainsKey(setId))
            throw new InvalidOperationException($"Set '{setId}' already exists!");

        var set = new SystemSet(setId, this);
        _sets[setId] = set;
        return set;
    }

    /// <summary>
    /// Gets a previously registered system set by identifier.
    /// </summary>
    public SystemSet GetSet(string setId) => _sets[setId];

    internal void ValidateHierarchy(string sourceId, string targetId)
    {
        if (sourceId == targetId) throw new InvalidOperationException($"Sada '{sourceId}' nemůže obsahovat sebe sama.");

        var visited = new HashSet<string>();
        var stack = new Stack<string>();
        stack.Push(targetId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!_sets.TryGetValue(current, out var set)) continue;

            foreach (var reference in set.Elements.OfType<SetReference>())
            {
                if (reference.TargetSetId == sourceId)
                    throw new InvalidOperationException($"Cyklický odkaz mezi sadami '{sourceId}' a '{current}'.");
                if (visited.Add(reference.TargetSetId)) stack.Push(reference.TargetSetId);
            }
        }
    }
}

/// <summary>
/// Declares systems, nested set references, and execution dependencies.
/// </summary>
public class SystemSet(string id, SystemConfiguration registry)
{
    /// <summary>
    /// Gets the identifier of this set.
    /// </summary>
    public string Id { get; } = id;
    private readonly SystemConfiguration _registry = registry;
    private readonly Dictionary<string, SetElement> _elementsMap = [];
    private readonly Dictionary<string, HashSet<string>> _dependencies = [];

    /// <summary>
    /// Gets the elements declared in this set.
    /// </summary>
    public IReadOnlyCollection<SetElement> Elements => _elementsMap.Values;

    /// <summary>
    /// Adds a system type to this set.
    /// </summary>
    public void AddSystem(Type type, string? id = null)
    {
        var system = new SystemEntry(id ?? type.Name, type);
        _elementsMap.Add(system.Id, system);
    }

    /// <summary>
    /// Adds a reference to another set after validating that it does not create a cycle.
    /// </summary>
    public void AddReference(string targetSetId, string? id = null)
    {
        _registry.ValidateHierarchy(Id, targetSetId);

        var reference = new SetReference(id ?? targetSetId, targetSetId);
        _elementsMap.Add(reference.Id, reference);
    }

    /// <summary>
    /// Declares that the first element must run before the second element.
    /// </summary>
    public void RunBefore(string beforeId, string afterId)
    {
        if (!_dependencies.ContainsKey(beforeId)) _dependencies[beforeId] = [];
        _dependencies[beforeId].Add(afterId);
    }

    /// <summary>
    /// Declares that the second element must run before the first element.
    /// </summary>
    public void RunAfter(string afterId, string beforeId) => RunBefore(beforeId, afterId);

    /// <summary>
    /// Resolves this set's elements into execution order.
    /// </summary>
    public IReadOnlyList<SetElement> ResolveOrder()
    {
        var inDegree = _elementsMap.ToDictionary(e => e.Key, _ => 0);

        foreach (var dep in _dependencies)
            foreach (var target in dep.Value)
                if (inDegree.TryGetValue(target, out int value)) inDegree[target] = ++value;

        var queue = new Queue<SetElement>(Elements.Where(e => inDegree[e.Id] == 0));
        var sorted = new List<SetElement>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(current);
            if (_dependencies.TryGetValue(current.Id, out var targets))
                foreach (var tId in targets)
                {
                    if (!inDegree.ContainsKey(tId)) continue;
                    inDegree[tId]--;
                    if (inDegree[tId] == 0) queue.Enqueue(_elementsMap[tId]);
                }
        }

        if (sorted.Count != _elementsMap.Count) throw new InvalidOperationException($"Cyklus v řazení sady '{Id}'.");
        return sorted;
    }
}
