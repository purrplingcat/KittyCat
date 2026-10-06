using Friflo.Engine.ECS;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Ecs.Queries;


/// <summary>
/// Represents a query that can be rebound to different entity stores.
/// </summary>
public interface IAutoQuery
{
    /// <summary>
    /// Gets the filter applied when creating queries for stores.
    /// </summary>
    QueryFilter Filter { get; }

    /// <summary>
    /// Binds this query to the specified entity store.
    /// </summary>
    void UpdateStore(EntityStore store);

    /// <summary>
    /// Clears cached queries associated with entity stores.
    /// </summary>
    void Cleanup();
}

/// <summary>
/// Base implementation for queries cached per entity store.
/// </summary>
public abstract class AutoQueryBase : IAutoQuery
{
    private readonly ConditionalWeakTable<EntityStore, ArchetypeQuery> _queries = [];
    private readonly ConditionalWeakTable<EntityStore, ArchetypeQuery>.CreateValueCallback _createCallback;

    /// <summary>
    /// Gets the filter used to create this query for each store.
    /// </summary>
    public QueryFilter Filter { get; } = new QueryFilter();
    

    /// <summary>
    /// Initializes the query and its per-store cache.
    /// </summary>
    public AutoQueryBase()
    {
        _createCallback = new ConditionalWeakTable<EntityStore, ArchetypeQuery>.CreateValueCallback(CreateQuery);
    }

    /// <summary>
    /// Creates or retrieves this query for the specified store and makes it current.
    /// </summary>
    public void UpdateStore(EntityStore store)
    {
        
        SetQuery(_queries.GetValue(store, _createCallback));
    }

    /// <summary>
    /// Clears all cached store queries.
    /// </summary>
    public void Cleanup()
    {
        _queries.Clear();
    }

    /// <summary>
    /// Creates the underlying query for an entity store.
    /// </summary>
    protected abstract ArchetypeQuery CreateQuery(EntityStore store);

    /// <summary>
    /// Sets the current query after it has been created or retrieved from the cache.
    /// </summary>
    protected abstract void SetQuery(ArchetypeQuery query);
}


/// <summary>
/// An automatically rebound query that selects entities without component constraints.
/// </summary>
public sealed class AutoQuery : AutoQueryBase
{
    /// <summary>
    /// Gets the query bound to the current entity store.
    /// </summary>
    public ArchetypeQuery Query { get; private set; } = null!;

    /// <summary>
    /// Creates an untyped query for the specified store.
    /// </summary>
    protected override ArchetypeQuery CreateQuery(EntityStore store)
    {
        return store.Query(Filter);
    }

    /// <summary>
    /// Sets the current untyped query.
    /// </summary>
    protected override void SetQuery(ArchetypeQuery query)
    {
        Query = query;
    }
}

/// <summary>
/// An automatically rebound query for entities with one component type.
/// </summary>
public sealed class AutoQuery<T1> : AutoQueryBase
    where T1 : struct, IComponent
{
    /// <summary>
    /// Gets the typed query bound to the current entity store.
    /// </summary>
    public ArchetypeQuery<T1> Query { get; private set; } = null!;

    /// <summary>
    /// Creates a query for the specified store.
    /// </summary>
    protected override ArchetypeQuery CreateQuery(EntityStore store)
    {
        return store.Query<T1>(Filter);
    }

    /// <summary>
    /// Sets the current typed query.
    /// </summary>
    protected override void SetQuery(ArchetypeQuery query)
    {
        Query = (ArchetypeQuery<T1>)query;
    }
}

/// <summary>
/// An automatically rebound query for entities with two component types.
/// </summary>
public sealed class AutoQuery<T1, T2> : AutoQueryBase
    where T1 : struct, IComponent
    where T2 : struct, IComponent
{
    /// <summary>
    /// Gets the typed query bound to the current entity store.
    /// </summary>
    public ArchetypeQuery<T1, T2> Query { get; private set; } = null!;

    /// <summary>
    /// Creates a query for the specified store.
    /// </summary>
    protected override ArchetypeQuery CreateQuery(EntityStore store)
    {
        return store.Query<T1, T2>(Filter);
    }

    /// <summary>
    /// Sets the current typed query.
    /// </summary>
    protected override void SetQuery(ArchetypeQuery query)
    {
        Query = (ArchetypeQuery<T1, T2>)query;
    }
}

/// <summary>
/// An automatically rebound query for entities with three component types.
/// </summary>
public sealed class AutoQuery<T1, T2, T3> : AutoQueryBase
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
{
    /// <summary>
    /// Gets the typed query bound to the current entity store.
    /// </summary>
    public ArchetypeQuery<T1, T2, T3> Query { get; private set; } = null!;

    /// <summary>
    /// Creates a query for the specified store.
    /// </summary>
    protected override ArchetypeQuery CreateQuery(EntityStore store)
    {
        return store.Query<T1, T2, T3>(Filter);
    }

    /// <summary>
    /// Sets the current typed query.
    /// </summary>
    protected override void SetQuery(ArchetypeQuery query)
    {
        Query = (ArchetypeQuery<T1, T2, T3>)query;
    }
}

/// <summary>
/// An automatically rebound query for entities with four component types.
/// </summary>
public sealed class AutoQuery<T1, T2, T3, T4> : AutoQueryBase
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
{
    /// <summary>
    /// Gets the typed query bound to the current entity store.
    /// </summary>
    public ArchetypeQuery<T1, T2, T3, T4> Query { get; private set; } = null!;
    
    /// <summary>
    /// Creates a query for the specified store.
    /// </summary>
    protected override ArchetypeQuery CreateQuery(EntityStore store)
    {
        return store.Query<T1, T2, T3, T4>(Filter);
    }

    /// <summary>
    /// Sets the current typed query.
    /// </summary>
    protected override void SetQuery(ArchetypeQuery query)
    {
        Query = (ArchetypeQuery<T1, T2, T3, T4>)query;
    }
}

/// <summary>
/// An automatically rebound query for entities with five component types.
/// </summary>
public sealed class AutoQuery<T1, T2, T3, T4, T5> : AutoQueryBase
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
    where T5 : struct, IComponent
{
    /// <summary>
    /// Gets the typed query bound to the current entity store.
    /// </summary>
    public ArchetypeQuery<T1, T2, T3, T4, T5> Query { get; private set; } = null!;
    
    /// <summary>
    /// Creates a query for the specified store.
    /// </summary>
    protected override ArchetypeQuery CreateQuery(EntityStore store)
    {
        return store.Query<T1, T2, T3, T4, T5>(Filter);
    }

    /// <summary>
    /// Sets the current typed query.
    /// </summary>
    protected override void SetQuery(ArchetypeQuery query)
    {
        Query = (ArchetypeQuery<T1, T2, T3, T4, T5>)query;
    }
}
