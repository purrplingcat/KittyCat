using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Toolkit.DI;

public class ServiceDefinition<TService>
{
    /// <summary>
    /// Gets the type of the <see cref="TService"/>
    /// </summary>
    public Type BaseType { get; }
    
    /// <summary>
    /// Implementation type of the factory
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Indexing key
    /// </summary>
    public object? Tag { get; }

    public ServiceDefinition(Type type) : this(type, null)
    {
    }

    public ServiceDefinition(Type type, object? tag)
    {
        BaseType = typeof(TService);
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Tag = tag;

        if (!BaseType.IsAssignableFrom(type))
        {
            throw new ArgumentException($"Type '{type.FullName}' does not implement {BaseType.FullName}.", nameof(type));
        }
    }
}
