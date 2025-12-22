using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.Collections;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Extensions;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;

namespace PurrplingCore.Ecs.Systems.Builder;

internal sealed class SystemGroupFactory<TGroup>(ISystemGroupFactory factory) : ISystemGroupFactory<TGroup> where TGroup : SystemGroup
{
    private readonly ISystemGroupFactory _factory = factory;
    public TGroup CreateGroup() => _factory.CreateGroup<TGroup>();
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
                OrderByDependency(memberTypes).Select(CreateSystem)
            );
        }

        return group;
    }

    private BaseSystem CreateSystem(Type type)
    {
        return (BaseSystem)_provider.GetRequiredService(type);
    }

    #region Dependency Sorting with Order Attribute
    public static IEnumerable<Type> OrderByDependency(IEnumerable<Type> types)
    {
        var typesArray = types.ToArray();
        int count = typesArray.Length;
        if (count <= 1) return typesArray;

        var typesSet = new HashSet<Type>(typesArray);
        var orders = new Dictionary<Type, int>(count);
        var inDegree = new Dictionary<Type, int>(count);
        var graph = new Hierarchy<Type>();

        for (int i = 0; i < count; i++)
        {
            var type = typesArray[i];
            orders[type] = type.GetCustomAttribute<OrderAttribute>()?.Order ?? 0;
            inDegree[type] = 0;
        }

        foreach (var type in typesArray)
        {
            foreach (var attr in type.GetCustomAttributes<RunAfterAttribute>())
            {
                if (attr.Types == null) continue;

                foreach (var dep in attr.Types)
                {
                    if (!typesSet.Contains(dep)) continue;

                    if (graph.AddChild(dep, type))
                    {
                        inDegree[type]++;
                    }
                }
            }

            foreach (var attr in type.GetCustomAttributes<RunBeforeAttribute>())
            {
                if (attr.Types == null) continue;

                foreach (var sub in attr.Types)
                {
                    if (!typesSet.Contains(sub)) continue;

                    if (graph.AddChild(type, sub))
                    {
                        inDegree[sub]++;
                    }
                }
            }
        }

        var queue = new PriorityQueue<Type, int>();
        var sortedResult = new List<Type>(typesArray.Length);

        foreach (var type in typesArray)
        {
            if (inDegree[type] == 0)
            {
                queue.Enqueue(type, orders[type]);
            }
        }

        while (queue.TryDequeue(out var current, out _))
        {
            sortedResult.Add(current);

            if (graph.TryGetChildren(current, out var children))
            {
                foreach (var child in children)
                {
                    inDegree[child]--;
                    if (inDegree[child] == 0)
                    {
                        queue.Enqueue(child, orders[child]);
                    }
                }
            }
        }

        if (sortedResult.Count != typesArray.Length)
        {
            throw new InvalidOperationException("Sorting failed. Some systems are locked in a circular dependency.");
        }

        return sortedResult;
    }
    #endregion
}
