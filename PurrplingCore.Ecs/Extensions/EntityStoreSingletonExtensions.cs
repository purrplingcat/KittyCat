using Friflo.Engine.ECS;

namespace PurrplingCore.Ecs.Extensions;

/// <summary>
/// Accesses components on named, unique entities in an ECS store.
/// </summary>
public static class EntityStoreSingletonExtensions
{
    /// <summary>
    /// Creates a named singleton or replaces its component when already present.
    /// </summary>
    /// <remarks>Call outside query iteration when the entity or component might need to be created.</remarks>
    public static void SetSingleton<T>(this EntityStore store, string name, T value) where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var index = store.ComponentIndex<UniqueEntity, string>();
        var entities = index[name];

        switch (entities.Count)
        {
            case 0:
                // No entity found, create a new one.
                store.CreateEntity(new UniqueEntity(name), value);
                return;
            case 1:
                // Exactly one entity found, update its component.
                entities[0].AddComponent(value);
                return;
            default:
                throw new InvalidOperationException(
                    $"Unable to set singleton '{name}': multiple entities found."
                );
        }
    }

    /// <summary>
    /// Returns a reference to a component on the uniquely named entity.
    /// </summary>
    public static ref T GetSingleton<T>(this EntityStore store, string name) where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrEmpty(name);
        return ref store.GetUniqueEntity(name).GetComponent<T>();
    }
}