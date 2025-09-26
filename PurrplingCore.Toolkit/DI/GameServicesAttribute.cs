namespace PurrplingCore.Toolkit.DI;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GameServicesAttribute : Attribute
{
    public GameServicesAttribute(Type configurationType)
    {
        ArgumentNullException.ThrowIfNull(configurationType);

        if (!typeof(IServiceConfiguration).IsAssignableFrom(configurationType))
        {
            throw new ArgumentException($"Type '{configurationType}' must implement '{typeof(IServiceConfiguration)}'.", nameof(configurationType));
        }

        if (configurationType.GetConstructor(Type.EmptyTypes) == null)
        {
            throw new ArgumentException($"Type '{configurationType}' must have a public parameterless constructor.", nameof(configurationType));
        }

        ConfigurationType = configurationType;
    }

    public Type ConfigurationType { get; }

    public IServiceConfiguration CreateConfiguration()
    {
        return (IServiceConfiguration)Activator.CreateInstance(ConfigurationType)!;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class GameServicesAttribute<TConfiguration>() : GameServicesAttribute(typeof(TConfiguration)) 
    where TConfiguration : IServiceConfiguration, new()
{
}
