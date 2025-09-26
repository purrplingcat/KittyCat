using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PurrplingCore.Toolkit.DI.Configuration;

public class AssemblyServices(params Assembly[] assemblies) : IServiceConfiguration
{
    private readonly Assembly[] _assemblies = assemblies;
    public virtual object? Key => null;

    public void Configure(IServiceCollection services)
    {
        foreach (var assembly in _assemblies)
        {
            var types = assembly.GetTypes();

            var singleton = types.Where(t => Attribute.IsDefined(t, typeof(SingletonAttribute))).ToList();
            var scoped = types.Where(t => Attribute.IsDefined(t, typeof(ScopedAttribute))).ToList();
            var transient = types.Where(t => Attribute.IsDefined(t, typeof(TransientAttribute))).ToList();
            var configurations = types.Where(t => 
                Attribute.IsDefined(t, typeof(ServiceConfiguration)) 
                && typeof(IServiceConfiguration).IsAssignableFrom(t)
            ).ToList();

            singleton.ForEach(type => {
                var attr = type.GetCustomAttribute<SingletonAttribute>();
                var registerAs = attr?.RegisterAs ?? type;
                var withInterfaces = attr?.WithInterfaces ?? false;
                
                services.AddSingleton(registerAs, type);
                RegisterAliases(services, registerAs, type, withInterfaces);
            });

            scoped.ForEach(type => {
                var attr = type.GetCustomAttribute<ScopedAttribute>();
                var registerAs = attr?.RegisterAs ?? type;
                var withInterfaces = attr?.WithInterfaces ?? false;

                services.AddScoped(registerAs, type);
                RegisterAliases(services, registerAs, type, withInterfaces);
            });

            transient.ForEach(type => {
                var attr = type.GetCustomAttribute<TransientAttribute>();
                var registerAs = attr?.RegisterAs ?? type;
                var withInterfaces = attr?.WithInterfaces ?? false;

                services.AddTransient(registerAs, type);
                RegisterAliases(services, registerAs, type, withInterfaces);
            });

            configurations.ForEach(type => {
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    throw new InvalidOperationException($"The service configuration '{type.FullName}' must have a parameterless constructor.");
                }
                if (Activator.CreateInstance(type) is IServiceConfiguration config)
                {
                    config.Configure(services);
                }
            });
        }
    }

    private static void RegisterAliases(IServiceCollection services, Type serviceType, Type implementationType, bool withInterfaces)
    {
        var attrs = implementationType.GetCustomAttributes<AliasAttribute>();
        var aliases = new HashSet<Type>(attrs.SelectMany(attr => attr.Aliases));


        if (withInterfaces)
        {
            aliases.UnionWith(GetInterfaces(implementationType));
        }

        services.AddAliases(serviceType, aliases);
    }

    private static HashSet<Type> GetInterfaces(Type type)
    {
        return type.GetInterfaces()
                .Where(x => !x.IsGenericType && !x.IsGenericTypeDefinition)
                .ToHashSet();
    }
}
