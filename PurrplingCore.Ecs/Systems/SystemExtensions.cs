using Friflo.Engine.ECS.Systems;

namespace PurrplingCore.Ecs.Systems;

/// <summary>
/// Provides lifecycle operations for Friflo system groups and roots.
/// </summary>
public static class SystemExtensions
{
    /// <summary>
    /// Initializes child groups and systems that implement <see cref="IInitializableSystem"/>.
    /// </summary>
    public static void Initialize(this SystemGroup root)
    {
        foreach (var child in root.ChildSystems)
        {
            if (child is SystemGroup subgroup)
            {
                subgroup.Initialize();
            }

            if (child is IInitializableSystem initializable)
            {
                initializable.Initialize();
            }
        }
    }

    /// <summary>
    /// Disposes and removes all child systems recursively.
    /// </summary>
    public static void Cleanup(this SystemGroup root)
    {
        // Convert child systems to array for safe removal
        foreach (var child in root.ChildSystems.ToArray())
        {
            if (child is SystemGroup subgroup)
            {
                subgroup.Cleanup();
            }
            if (child is IDisposable disposable)
            {
                disposable.Dispose();
            }

            root.Remove(child);
        }
    }

    /// <summary>
    /// Removes all child systems without disposing them.
    /// </summary>
    public static void Clear(this SystemGroup root)
    {
        // Convert child systems to array for safe removal
        foreach (var child in root.ChildSystems.ToArray())
        {
            root.Remove(child);
        }
    }

    /// <summary>
    /// Removes all entity stores from a system root.
    /// </summary>
    public static void RemoveAllStores(this SystemRoot systemRoot)
    {
        ArgumentNullException.ThrowIfNull(systemRoot, nameof(systemRoot));

        // Convert stores to array for safe removal
        foreach (var store in systemRoot.Stores.ToArray())
        {
            systemRoot.RemoveStore(store);
        }
    }

    /// <summary>
    /// Cleans up a system root, removes its stores, and disposes the root when supported.
    /// </summary>
    public static void Destroy(this SystemRoot root)
    {
        root.Cleanup();
        root.RemoveAllStores();

        if (root is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
