using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace PurrplingCore.Toolkit.DI.Configuration;

public class AssemblyServices(params Assembly[] assemblies) : IServiceConfiguration
{
    private object _lock = new();
    private readonly Assembly[] _assemblies = assemblies;
    public virtual object? Key => null;

    #region service registration helpers
    private class ServiceSource<TSystemAtribute>(Type[] types) where TSystemAtribute : SystemAttribute
    {
        private readonly Type[] _types = [.. types.Where(t => Attribute.IsDefined(t, typeof(TSystemAtribute)))];

        public IEnumerable<(Type ServiceType, Type ImplementationType, TSystemAtribute Attribute)> GetServices()
        {
            foreach (var type in _types)
            {
                var attr = type.GetCustomAttribute<TSystemAtribute>();
                if (attr != null)
                {
                    var serviceType = attr.RegisterAs ?? type;
                    yield return (serviceType, type, attr);
                }
            }
        }

        public void RegisterAsSingletnon(IServiceCollection services)
        {
            foreach (var (serviceType, implementationType, attr) in GetServices())
            {
                services.AddSingleton(serviceType, implementationType);
                services.AddAliases(serviceType, implementationType.GetServiceTypeAliases(attr.WithInterfaces));
            }
        }

        public void RegisterAsScoped(IServiceCollection services)
        {
            foreach (var (serviceType, implementationType, attr) in GetServices())
            {
                services.AddScoped(serviceType, implementationType);
                services.AddAliases(serviceType, implementationType.GetServiceTypeAliases(attr.WithInterfaces));
            }
        }

        public void RegisterAsTransient(IServiceCollection services)
        {
            foreach (var (serviceType, implementationType, attr) in GetServices())
            {
                services.AddTransient(serviceType, implementationType);
                services.AddAliases(serviceType, implementationType.GetServiceTypeAliases(attr.WithInterfaces));
            }
        }
    }

    private class ConfigurationSource(Type[] types)
    {
        private readonly IEnumerable<Type> types = types.Where(IsConfigurationType);

        private static bool IsConfigurationType(Type type)
        {
            return Attribute.IsDefined(type, typeof(ServiceConfiguration)) 
                && typeof(IServiceConfiguration).IsAssignableFrom(type);
        }

        public IEnumerable<IServiceConfiguration> GetConfigurations()
        {
            foreach (var type in types)
            {
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    throw new InvalidOperationException($"The service configuration '{type.FullName}' must have a parameterless constructor.");
                }

                if (Activator.CreateInstance(type) is IServiceConfiguration config)
                {
                    yield return config;
                }
            }
        }

        public void Configure(IServiceCollection services)
        {
            foreach (var config in GetConfigurations())
            {
                config.Configure(services);
            }
        }
    }
    #endregion

    public void Configure(IServiceCollection services)
    {
        var watch = Stopwatch.StartNew();

        Parallel.ForEach(_assemblies, assembly =>
        {
            var types = assembly.GetTypes();
            var singleton = new ServiceSource<SingletonAttribute>(types);
            var scoped = new ServiceSource<ScopedAttribute>(types);
            var transient = new ServiceSource<TransientAttribute>(types);
            var configurations = new ConfigurationSource(types);

            lock (_lock)
            {
                singleton.RegisterAsSingletnon(services);
                scoped.RegisterAsScoped(services);
                transient.RegisterAsTransient(services);
                configurations.Configure(services);
            }
        });
    }
}
