using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PurrplingCore.Toolkit.DI;
using System.Text.RegularExpressions;

namespace PurrplingCore.Toolkit.Systems;

public interface ISystemBuilder 
{
    ISystemBuilder AddSystem<TSystem>() where TSystem : BaseSystem;
    ISystemBuilder AddSystem<TSystem, TImplementation>() 
        where TSystem : BaseSystem 
        where TImplementation : TSystem;
    ISystemBuilder AddSystems(Action<ISystemBuilder> configure);
    ISystemBuilder AddSystems(ISystemConfiguration configuration);
    ISystemBuilder AddSystemFactory<TSystem, TFactory>() 
        where TSystem : BaseSystem 
        where TFactory : IServiceFactory<TSystem>;
    ISystemBuilder AddSystemGroup<TGroup>() where TGroup : SystemGroup;
}

internal sealed class SystemBuilder<TGroup>(IServiceCollection services) : ISystemBuilder where TGroup : SystemGroup
{
    private readonly IServiceCollection _services = services;

    public ISystemBuilder AddSystem<TSystem>() where TSystem : BaseSystem
    {
        AddChild<TSystem>();
        _services.TryAddTransient<TSystem>();

        return this;
    }

    public ISystemBuilder AddSystem<TSystem, TImplementation>()
        where TSystem : BaseSystem
        where TImplementation : TSystem
    {
        AddChild<TSystem>();
        _services.AddTransient<TSystem, TImplementation>();

        return this;
    }

    public ISystemBuilder AddSystemFactory<TSystem, TFactory>()
        where TSystem : BaseSystem
        where TFactory : IServiceFactory<TSystem>
    {
        AddChild<TSystem>();
        _services.TryAddTransient(provider =>
        {
            TFactory factory = ActivatorUtilities.CreateInstance<TFactory>(provider);
            return factory.Create();
        });

        return this;
    }

    public ISystemBuilder AddSystemGroup<TNestedGroup>() where TNestedGroup : SystemGroup
    {
        AddChild<TNestedGroup>();
        _services.AddSystemGroup<TNestedGroup>();

        return new SystemBuilder<TGroup>(_services);
    }

    public ISystemBuilder AddSystems(Action<ISystemBuilder> configure)
    {
        configure(this);
        return this;
    }

    public ISystemBuilder AddSystems(ISystemConfiguration configuration)
    {
        configuration.Configure(this);
        return this;
    }

    private void AddChild<TChild>() where TChild : BaseSystem
    {
        _services.Configure<SystemTreeConfiguration>(
            static config => config.AddChild<TGroup, TChild>()
        );
    }
}
