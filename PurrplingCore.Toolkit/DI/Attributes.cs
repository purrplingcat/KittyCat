namespace PurrplingCore.Toolkit.DI;

[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute(Type? registerAs = null) : Attribute
{
    public Type? RegisterAs { get; } = registerAs;
    public bool WithInterfaces { get; set; } = true;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SingletonAttribute : ServiceAttribute { }

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ScopedAttribute : ServiceAttribute { }

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TransientAttribute : ServiceAttribute { }

[AttributeUsage(AttributeTargets.Class)]
public class AliasAttribute(params Type[] aliases) : Attribute
{
    public Type[] Aliases { get; } = aliases ?? throw new ArgumentNullException(nameof(aliases));
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class FactoryAttribute : Attribute
{
}
