using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Toolkit.Hosting;

namespace PurrplingCore.Toolkit.DI;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GameServicesAttribute : Attribute
{
    public GameServicesAttribute(Type configurationType)
    {
        ArgumentNullException.ThrowIfNull(configurationType);

        if (!typeof(IServiceModule).IsAssignableFrom(configurationType))
        {
            throw new ArgumentException($"Type '{configurationType}' must implement '{typeof(IServiceModule)}'.", nameof(configurationType));
        }

        if (configurationType.GetConstructor(Type.EmptyTypes) == null)
        {
            throw new ArgumentException($"Type '{configurationType}' must have a public parameterless constructor.", nameof(configurationType));
        }

        ConfigurationType = configurationType;
    }

    public Type ConfigurationType { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class GameServicesAttribute<TConfiguration>() : GameServicesAttribute(typeof(TConfiguration)) 
    where TConfiguration : IServiceModule
{
}

/// <summary>
/// Declares a service module that is applied to the boot/host service collection by
/// <see cref="IGameHostBuilder.AddGame{TGame}"/>.
/// </summary>
/// <remarks>
/// Unlike <see cref="GameServicesAttribute"/>, this module runs while the host is still being
/// composed, before the host service provider is built. Use it for services needed during boot
/// or by later game-service modules.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GameHostServicesAttribute : Attribute
{
    /// <summary>
    /// Creates an attribute for a parameterless <see cref="IServiceModule"/> implementation.
    /// </summary>
    /// <param name="configurationType">The module applied directly to host services.</param>
    public GameHostServicesAttribute(Type configurationType)
    {
        ArgumentNullException.ThrowIfNull(configurationType);

        if (!typeof(IServiceModule).IsAssignableFrom(configurationType))
        {
            throw new ArgumentException($"Type '{configurationType}' must implement '{typeof(IServiceModule)}'.", nameof(configurationType));
        }

        if (configurationType.GetConstructor(Type.EmptyTypes) == null)
        {
            throw new ArgumentException($"Type '{configurationType}' must have a public parameterless constructor.", nameof(configurationType));
        }

        ConfigurationType = configurationType;
    }

    /// <summary>
    /// Gets the host service module type.
    /// </summary>
    public Type ConfigurationType { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class GameHostServicesAttribute<TConfiguration>() : GameHostServicesAttribute(typeof(TConfiguration))
    where TConfiguration : class, IServiceModule, new()
{
}
