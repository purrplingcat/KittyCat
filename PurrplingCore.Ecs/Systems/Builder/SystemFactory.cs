using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Toolkit.DI;

namespace PurrplingCore.Ecs.Systems.Builder;

public abstract class SystemFactory<TSystem>(SystemCreator creator) : IServiceFactory<TSystem> where TSystem : BaseSystem
{
    private readonly SystemCreator _creator = creator;

    protected abstract TSystem Create(SystemCreator creator);

    public TSystem Create() => Create(_creator);
}

public sealed class SystemCreator
{
    private readonly IServiceProvider _provider;

    internal SystemCreator(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public T CreateSystem<T>() where T : BaseSystem
    {
        return ActivatorUtilities.CreateInstance<T>(_provider);
    }

    public T CreateSystem<T>(params object[] parameters) where T : BaseSystem
    {
        return ActivatorUtilities.CreateInstance<T>(_provider, parameters);
    }

    public IEnumerable<BaseSystem> CreateSystems(params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(_provider);
        ArgumentNullException.ThrowIfNull(types);

        foreach (var type in types)
        {
            yield return (BaseSystem)ActivatorUtilities.CreateInstance(_provider, type);
        }
    }
}