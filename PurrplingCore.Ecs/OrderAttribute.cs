using Friflo.Engine.ECS.Systems;

namespace PurrplingCore.Ecs;

/// <summary>
/// Assigns an integer execution order to a system or group.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class OrderAttribute(int order) : Attribute
{
    /// <summary>
    /// Gets the configured execution order.
    /// </summary>
    public int Order { get; } = order;
}

/// <summary>
/// Associates tags with a system or group.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class TagsAttribute(params object[] tags) : Attribute
{
    /// <summary>
    /// Gets the tags associated with the annotated type.
    /// </summary>
    public object[] Tags { get; } = tags;
}

/// <summary>
/// Declares that a system should run before the specified system.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RunBeforeAttribute(Type targetType) : Attribute
{
    /// <summary>
    /// Gets the system type that must run after the annotated system.
    /// </summary>
    public Type TargetType { get; } = targetType;
}

/// <summary>
/// Declares that a system should run after the specified system.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RunAfterAttribute(Type targetType) : Attribute
{
    /// <summary>
    /// Gets the system type that must run before the annotated system.
    /// </summary>
    public Type TargetType { get; } = targetType;
}

/// <summary>
/// Declares that a system should run before the specified system type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RunBeforeAttribute<TSystem>() : RunBeforeAttribute(typeof(TSystem)) where TSystem : BaseSystem
{
}

/// <summary>
/// Declares that a system should run after the specified system type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RunAfterAttribute<TSystem>() : RunAfterAttribute(typeof(TSystem)) where TSystem : BaseSystem
{
}

/// <summary>
/// Declares the system group that contains the annotated type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class UpdateInGroupAttribute(Type groupType) : Attribute
{
    /// <summary>
    /// Gets the system group type.
    /// </summary>
    public Type GroupType { get; } = groupType;
}

/// <summary>
/// Provides commonly used integer execution-order values.
/// </summary>
public static class Order
{
    /// <summary>
    /// The earliest possible execution order.
    /// </summary>
    public const int First = int.MinValue;

    /// <summary>
    /// An early execution order before the default order.
    /// </summary>
    public const int Earlier = -10000;

    /// <summary>
    /// An early execution order before the default order.
    /// </summary>
    public const int Early = -1000;

    /// <summary>
    /// The default execution order.
    /// </summary>
    public const int Default = 0;

    /// <summary>
    /// A late execution order after the default order.
    /// </summary>
    public const int Late = 1000;

    /// <summary>
    /// A late execution order after the default order.
    /// </summary>
    public const int Later = 10000;

    /// <summary>
    /// The latest possible execution order.
    /// </summary>
    public const int Last = int.MaxValue;
}
