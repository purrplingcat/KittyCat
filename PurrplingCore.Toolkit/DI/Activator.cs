using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PurrplingCore.Toolkit.DI;

public sealed class ActivationDescriptor<TBase> : ServiceDefinition<TBase>
{
    public Type InstanceType { get; }

    public ActivationDescriptor(Type instanceType) : base(instanceType, instanceType)
    {
        InstanceType = instanceType;
    }
}

public abstract class Activator<T>(in IServiceProvider provider, in IEnumerable<ActivationDescriptor<T>> descriptors)
{
    private readonly IServiceProvider provider = provider;
    private readonly ActivationDescriptor<T>[] descriptors = descriptors.ToArray();

    protected IEnumerable<T> Create(params object[] parameters)
    {
        foreach (var descriptor in descriptors)
        {
            yield return (T)ActivatorUtilities.CreateInstance(provider, descriptor.InstanceType, parameters);
        }
    }
}

public static partial class ServiceExtensions
{
    public static IServiceCollection AddActivation<TBase, TInstance>(this IServiceCollection services)
        where TBase : class
        where TInstance : class, TBase
    {
        services.TryAddSingleton(new ActivationDescriptor<TBase>(typeof(TInstance)));
        return services;
    }
}
