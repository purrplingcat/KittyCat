using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.Extensions;

public static class WorldExtensions
{
    public static void AddTopLevelGroup(this World world, SystemGroup group)
    {
        var groupType = group.GetType();

        foreach (var sys in world.SystemRoot)
        {
            if (ReferenceEquals(sys, group))
                return;

            if (sys.GetType() == groupType)
            {
                throw new InvalidOperationException($"System of type {groupType.Name} already exists in the world.");
            }
        }

        world.SystemRoot.Add(group);
    }

    public static T GetSystem<T>(this World world, bool recursive = true) where T : BaseSystem
    {
        return world.FindSystem<T>(recursive) 
            ?? throw new InvalidOperationException($"System of type {typeof(T).Name} not found in the world.");
    }

    public static SystemGroup GetGroup(this World world, string name, bool recursive = true)
    {
        return world.FindGroup(name, recursive) 
            ?? throw new InvalidOperationException($"System group with name '{name}' not found in the world.");
    }

    public static UpdateSystemGroup GetUpdateGroup(this World world)
    {
        return GetSystem<UpdateSystemGroup>(world, recursive: false);
    }

    public static FixedUpdateSystemGroup GetFixedUpdateGroup(this World world)
    {
        return GetSystem<FixedUpdateSystemGroup>(world, recursive: false);
    }
    public static DrawSystemGroup GetDrawGroup(this World world)
    {
        return GetSystem<DrawSystemGroup>(world, recursive: false);
    }

    public static InitializeSystemGroup GetInitializeGroup(this World world)
    {
        return GetSystem<InitializeSystemGroup>(world, recursive: false);
    }
}
