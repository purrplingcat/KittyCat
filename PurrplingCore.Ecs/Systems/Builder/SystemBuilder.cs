using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.DI;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PurrplingCore.Ecs.Systems.Builder;

public interface ISystemBuilder 
{
    IServiceCollection Services { get; }

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
    ISystemBuilder AddSystemGroup<TGroup>(Action<ISystemBuilder> configure) where TGroup : SystemGroup;
}

internal sealed class SystemBuilder<TGroup>(IServiceCollection services) : ISystemBuilder where TGroup : SystemGroup
{
    private readonly IServiceCollection _services = services;

    public IServiceCollection Services => _services;

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

        return this;
    }

    public ISystemBuilder AddSystemGroup<TNestedGroup>(Action<ISystemBuilder> configure) where TNestedGroup : SystemGroup
    {
        AddSystemGroup<TNestedGroup>();
        configure(new SystemBuilder<TNestedGroup>(_services));

        return this;
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
        /*_services.Configure<SystemTreeConfiguration>(
            static config => config.AddChild<TGroup, TChild>()
        );*/
    }
}

public static class SystemBuilderExtensions
{
    public static ISystemBuilder AddSystemGroup<TTag>(this ISystemBuilder builder) where TTag : struct, ITag
    {
        return builder.AddSystemGroup<TaggedSystemGroup<TTag>>();
    }

    public static ISystemBuilder AddSystemsFromAssembly(this ISystemBuilder builder, Assembly assembly)
    {
        // 1. Najdeme všechny konkrétní třídy dědící z BaseSystem
        var systemTypes = assembly.GetTypes()
            .Where(t => typeof(BaseSystem).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);

        foreach (var systemType in systemTypes)
        {
            // 2. Zkusíme najít atribut [UpdateInGroup]
            var groupAttr = systemType.GetCustomAttribute<UpdateInGroupAttribute>();

            if (groupAttr != null)
            {
                // A: Má atribut -> jde do specifické skupiny
                //builder.AddSystemToGroup(groupAttr.GroupType, systemType);
            }
            else
            {
                // B: Nemá atribut -> jde do aktuální skupiny builderu (např. SystemRoot)
                //builder.AddSystem(systemType);
            }
        }

        return builder;
    }
}