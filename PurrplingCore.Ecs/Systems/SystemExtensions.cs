using Friflo.Engine.ECS.Systems;

namespace PurrplingCore.Ecs.Systems;

public static class SystemExtensions
{
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

    public static void RemoveAllStores(this SystemRoot systemRoot)
    {
        ArgumentNullException.ThrowIfNull(systemRoot, nameof(systemRoot));

        // Convert stores to array for safe removal
        foreach (var store in systemRoot.Stores.ToArray())
        {
            systemRoot.RemoveStore(store);
        }
    }

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
