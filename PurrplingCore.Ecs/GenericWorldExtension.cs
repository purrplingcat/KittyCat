using Friflo.Engine.ECS;

namespace PurrplingCore.Ecs;

internal sealed class GenericWorldExtension<TExtension> : WorldExtension<TExtension> where TExtension : class
{
    private readonly IServiceProvider _provider;
    private readonly Func<EntityStore, IServiceProvider, TExtension> _factory;

    public GenericWorldExtension(World world, IServiceProvider provider, Func<EntityStore, IServiceProvider, TExtension> factory) : base(world)
    {
        _provider = provider;
        _factory = factory;
    }

    protected override TExtension Create(EntityStore store)
    {
        return _factory(store, _provider);
    }
}
