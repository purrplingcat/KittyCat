using Friflo.Engine.ECS.Systems;
using Friflo.Json.Fliox.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Extensions;
using PurrplingCore.Toolkit.Systems;
using System.Reflection;

namespace PurrplingCore.Toolkit;

public interface ISystemGroupFactory
{
    TGroup CreateGroup<TGroup>() where TGroup : SystemGroup;
}

public interface ISystemGroupFactory<out TGroup> where TGroup : SystemGroup
{
    TGroup CreateGroup();
}

internal class SystemGroupFactory(IServiceProvider provider, IOptions<SystemTreeConfiguration> options) : ISystemGroupFactory
{
    private readonly IServiceProvider _provider = provider;
    private readonly SystemTreeConfiguration _config = options.Value;

    [Factory]
    public TGroup CreateGroup<TGroup>() where TGroup : SystemGroup
    {
        var group = ActivatorUtilities.CreateInstance<TGroup>(_provider);

        if (_config.TryGetGroup(typeof(TGroup), out var memberTypes))
        {
            group.Add(
                SortByDependency(memberTypes).Select(CreateSystem)
            );
        }

        return group;
    }

    private BaseSystem CreateSystem(Type type)
    {
        return (BaseSystem)ActivatorUtilities.CreateInstance(_provider, type);
    }

    #region Dependency Sorting with Order Attribute
    public static List<Type> SortByDependency(IEnumerable<Type> types)
    {
        var typesInList = types.ToList();
        if (typesInList.Count <= 1) return typesInList;

        // --- Step 1: Build the graph and in-degree map ---
        var graph = typesInList.ToDictionary(t => t, t => new HashSet<Type>());
        var inDegree = typesInList.ToDictionary(t => t, t => 0);

        foreach (var type in typesInList)
        {
            var runAfters = type.GetCustomAttributes<RunAfterAttribute>().SelectMany(attr => attr.Types);
            foreach (var dep in runAfters.Where(typesInList.Contains)) 
            {
                if (graph[dep].Add(type))
                {
                    inDegree[type]++;
                }
            }

            var runBefores = type.GetCustomAttributes<RunBeforeAttribute>().SelectMany(attr => attr.Types);
            foreach (var sub in runBefores.Where(typesInList.Contains)) 
            { 
                if (graph[type].Add(sub)) 
                {
                    inDegree[sub]++;
                }
            }
        }

        // --- Step 2: Topological sort with priority queue based on Order attribute ---
        var sorted = new List<Type>();
        var comparer = new SystemOrderComparer(typesInList);
        var priorityQueue = new SortedSet<Type>(comparer);
        foreach (var type in typesInList.Where(t => inDegree[t] == 0))
        {
            priorityQueue.Add(type);
        }

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Min!;
            priorityQueue.Remove(current);
            sorted.Add(current);

            foreach (var neighbor in graph[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                {
                    priorityQueue.Add(neighbor);
                }
            }
        }

        if (sorted.Count != typesInList.Count)
            throw new InvalidOperationException("Circular dependency detected.");

        return sorted;
    }

    private class SystemOrderComparer : IComparer<Type>
    {
        private readonly Dictionary<Type, int> _orderCache = new();
        public SystemOrderComparer(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                _orderCache[type] = type.GetCustomAttribute<OrderAttribute>()?.Order ?? 0;
            }
        }

        public int Compare(Type? x, Type? y)
        {
            if (x is null || y is null) return 0;
            int orderX = _orderCache[x];
            int orderY = _orderCache[y];
            return orderX.CompareTo(orderY);
        }
    }
    #endregion
}

internal sealed class SystemGroupFactory<TGroup>(ISystemGroupFactory factory) : ISystemGroupFactory<TGroup> where TGroup : SystemGroup
{
    private readonly ISystemGroupFactory _factory = factory;
    public TGroup CreateGroup() => _factory.CreateGroup<TGroup>();
}
