using Friflo.Engine.ECS;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Ecs.Queries;


public interface IAutoQuery
{
    QueryFilter Filter { get; }

    void UpdateStore(EntityStore store);
    void Cleanup();
}

public abstract class AutoQueryBase : IAutoQuery
{
    private readonly ConditionalWeakTable<EntityStore, ArchetypeQuery> _queries = [];
    private readonly ConditionalWeakTable<EntityStore, ArchetypeQuery>.CreateValueCallback _createCallback;

    public QueryFilter Filter { get; } = new QueryFilter();
    

    public AutoQueryBase()
    {
        _createCallback = new ConditionalWeakTable<EntityStore, ArchetypeQuery>.CreateValueCallback(CreateQuery);
    }

    public void UpdateStore(EntityStore store)
    {
        
        SetQuery(_queries.GetValue(store, _createCallback));
    }

    public void Cleanup()
    {
        _queries.Clear();
    }

    protected abstract ArchetypeQuery CreateQuery(EntityStore store);
    protected abstract void SetQuery(ArchetypeQuery query);
}


public sealed class AutoQuery : AutoQueryBase
{
    public ArchetypeQuery Query { get; private set; } = null!;

    protected override ArchetypeQuery CreateQuery(EntityStore store)
    {
        return store.Query(Filter);
    }

    protected override void SetQuery(ArchetypeQuery query)
    {
        Query = query;
    }
}

public sealed class AutoQuery<T1> : AutoQueryBase
    where T1 : struct, IComponent
{
    public ArchetypeQuery<T1> Query { get; private set; } = null!;

    protected override ArchetypeQuery CreateQuery(EntityStore store)
    {
        return store.Query<T1>(Filter);
    }

    protected override void SetQuery(ArchetypeQuery query)
    {
        Query = (ArchetypeQuery<T1>)query;
    }
}

public sealed class AutoQuery<T1, T2> : AutoQueryBase
    where T1 : struct, IComponent
    where T2 : struct, IComponent
{
    public ArchetypeQuery<T1, T2> Query { get; private set; } = null!;

    protected override ArchetypeQuery CreateQuery(EntityStore store)
    {
        return store.Query<T1, T2>(Filter);
    }

    protected override void SetQuery(ArchetypeQuery query)
    {
        Query = (ArchetypeQuery<T1, T2>)query;
    }
}

public sealed class AutoQuery<T1, T2, T3> : AutoQueryBase
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
{
    public ArchetypeQuery<T1, T2, T3> Query { get; private set; } = null!;

    protected override ArchetypeQuery CreateQuery(EntityStore store)
    {
        return store.Query<T1, T2, T3>(Filter);
    }

    protected override void SetQuery(ArchetypeQuery query)
    {
        Query = (ArchetypeQuery<T1, T2, T3>)query;
    }
}

public sealed class AutoQuery<T1, T2, T3, T4> : AutoQueryBase
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
{
    public ArchetypeQuery<T1, T2, T3, T4> Query { get; private set; } = null!;
    
    protected override ArchetypeQuery CreateQuery(EntityStore store)
    {
        return store.Query<T1, T2, T3, T4>(Filter);
    }

    protected override void SetQuery(ArchetypeQuery query)
    {
        Query = (ArchetypeQuery<T1, T2, T3, T4>)query;
    }
}

public sealed class AutoQuery<T1, T2, T3, T4, T5> : AutoQueryBase
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
    where T5 : struct, IComponent
{
    public ArchetypeQuery<T1, T2, T3, T4, T5> Query { get; private set; } = null!;
    
    protected override ArchetypeQuery CreateQuery(EntityStore store)
    {
        return store.Query<T1, T2, T3, T4, T5>(Filter);
    }

    protected override void SetQuery(ArchetypeQuery query)
    {
        Query = (ArchetypeQuery<T1, T2, T3, T4, T5>)query;
    }
}
