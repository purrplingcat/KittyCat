using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Creates and caches system instances, then populates their child groups from a registry.
/// </summary>
public sealed class SystemProvider(SystemRegistry registry, Func<Type, BaseSystem> resolver, WorldSignature signature)
{
    private readonly Dictionary<Type, BaseSystem> _instances = [];

    /// <summary>
    /// Gets the world signature used to filter systems by flags.
    /// </summary>
    public WorldSignature Signature => signature;

    /// <summary>
    /// Gets or creates the system instance for the specified type.
    /// </summary>
    public BaseSystem Provide(Type type)
    {
        if (_instances.TryGetValue(type, out var existingSystem))
            return existingSystem;

        var system = resolver.Invoke(type);
        _instances.Add(type, system);

        if (system is SystemGroup group)
            Populate(group);

        return system;
    }

    /// <summary>
    /// Adds the registered systems for a group, respecting order and world flags.
    /// </summary>
    public void Populate(SystemGroup group, Type? type = null)
    {
        type ??= group.GetType();
        if (!registry.TryGet(type, out var systemSet)) return;

        var entries = systemSet.GetSortedEntries();
        for (int index = 0; index < entries.Length; index++)
        {
            if (!signature.HasAnyFlag(entries[index].Flags))
                continue;

            var system = Provide(entries[index].Type);
            if (system == null)
                continue;
            if (system == group)
                throw new InvalidOperationException("Cannot add itself to group");

            if (system.ParentGroup != null)
                throw new InvalidOperationException(
                    $"Cannot add system '{system.Name}' to group '{group.Name}': " +
                    $"System is already apart of '{system.ParentGroup.Name}' group"
                );

            group.Add(system);
        }
    }
}