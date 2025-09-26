using Friflo.Engine.ECS.Systems;
using System.Reflection;

namespace PurrplingCore.Toolkit.Extensions;

public static class SystemSorter
{
    private static readonly Dictionary<Type, int> _orderCache = [];
    private static readonly object _orderCacheLock = new();

    public static int GetOrder(this BaseSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);

        var systemType = system.GetType();
        lock (_orderCacheLock)
        {
            if (!_orderCache.TryGetValue(systemType, out var order))
            {
                var recursionStack = new HashSet<Type>();
                order = ComputeOrderRecursive(systemType, recursionStack);
                _orderCache[systemType] = order;
            }

            return order;
        }
    }

    private static int ComputeOrderRecursive(Type type, HashSet<Type> recursionStack)
    {
        if (recursionStack.Contains(type))
        {
            throw new InvalidOperationException($"Circular dependency detected involving type '{type.Name}'.");
        }

        if (_orderCache.TryGetValue(type, out var cachedOrder))
        {
            return cachedOrder;
        }

        recursionStack.Add(type);

        var orderAttribute = type.GetCustomAttribute<OrderAttribute>();
        var runAfterAttributes = type.GetCustomAttributes<RunAfterAttribute>();
        var order = orderAttribute?.Order ?? 0;

        foreach (Type runAfter in runAfterAttributes.SelectMany(static attr => attr.Types))
        {
            var runAfterOrder = ComputeOrderRecursive(runAfter, recursionStack);
            if (runAfterOrder >= order)
            {
                order = runAfterOrder + 1;
            }
        }

        recursionStack.Remove(type);
        return order;
    }

    public static IOrderedEnumerable<BaseSystem> Sort(this IEnumerable<BaseSystem> systems)
    {
        ArgumentNullException.ThrowIfNull(systems);

        return systems.OrderBy(s => s.GetOrder());
    }
}
