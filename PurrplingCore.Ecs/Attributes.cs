using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.Attributes;

/// <summary>
/// Declares the system group, execution order, and world flags for a system.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class SystemAttribute(Type groupType) : Attribute
{
    SystemAttribute() : this(typeof(UpdateSystemGroup)) { }

    /// <summary>
    /// Gets the system group that contains the annotated system.
    /// </summary>
    public Type GroupType { get; } = groupType;

    /// <summary>
    /// Gets or sets the position of the system within its ordering category.
    /// </summary>
    public SystemOrder Order { get; set; } = SystemOrder.Default;

    /// <summary>
    /// Gets or sets the world flags required for the system to run.
    /// </summary>
    public WorldFlags Flags { get; set; } = WorldFlags.None;
}

/// <summary>
/// Declares a system group using a group type as a generic argument.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SystemAttribute<T> : SystemAttribute where T : SystemGroup
{
    /// <summary>
    /// Initializes the attribute for the specified system group type.
    /// </summary>
    public SystemAttribute() : base(typeof(T)) { }
}

/// <summary>
/// Declares that a system should run before another system when both are in the same group.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RunBeforeAttribute(Type targetSystemType) : Attribute
{
    /// <summary>
    /// Gets the system type that must run after the annotated system.
    /// </summary>
    public Type TargetType { get; } = targetSystemType;
}

/// <summary>
/// Declares that a system should run after another system when both are in the same group.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RunAfterAttribute(Type targetSystemType) : Attribute
{
    /// <summary>
    /// Gets the system type that must run before the annotated system.
    /// </summary>
    public Type TargetType { get; } = targetSystemType;
}

/// <summary>
/// Restricts a system to worlds identified by a marker type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class TargetWorldAttribute(Type worldMarkerType) : Attribute
{
    /// <summary>
    /// Gets the marker type of a world that can contain the annotated system.
    /// </summary>
    public Type WorldMarkerType { get; } = typeof(IWorldMarker).IsAssignableFrom(worldMarkerType)
        ? worldMarkerType
        : throw new ArgumentException("Type must implement IWorldMarker");
}

/// <summary>
/// Restricts a system to the world identified by the generic marker type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class TargetWorldAttribute<T> : TargetWorldAttribute where T : IWorldMarker
{
    /// <summary>
    /// Initializes the attribute for the specified world marker type.
    /// </summary>
    public TargetWorldAttribute() : base(typeof(T))
    {
    }
}

/// <summary>
/// Marks a system group as a top-level group in a world's system hierarchy.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TopLevelGroupAttribute() : SystemAttribute(typeof(SystemRoot))
{
}

internal sealed class CoreSystemAttribute : Attribute
{
}
