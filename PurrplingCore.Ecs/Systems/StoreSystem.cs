using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Extensions;

namespace PurrplingCore.Ecs.Systems;

/// <summary>
/// Base for systems that need each store and tick but do not need a component query.
/// </summary>
/// <remarks>
/// This base has no query or command buffer. Use it for component updates or direct store
/// operations outside query iteration. Use Friflo <see cref="QuerySystem"/> for deferred
/// structural changes that require the parent group's shared command buffer.
/// </remarks>
public abstract class StoreSystem : BaseSystem
{
    private EntityStore? _store;

    /// <summary>
    /// Gets the store currently being processed.
    /// </summary>
    protected EntityStore Store => _store!;

    /// <summary>
    /// Updates the system for one attached store.
    /// </summary>
    protected abstract void OnUpdate();

    /// <summary>
    /// Updates the system once for each attached store.
    /// </summary>
    protected sealed override void OnUpdateGroup()
    {
        var stores = SystemRoot.Stores;
        for (var index = 0; index < stores.Count; index++)
        {
            _store = stores[index];
            OnUpdate();
        }
    }
}
