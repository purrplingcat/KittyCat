using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class SystemAttribute(Type groupType) : Attribute
{
    SystemAttribute() : this(typeof(UpdateSystemGroup)) { }

    public Type GroupType { get; } = groupType;
    public SystemOrder Order { get; set; } = SystemOrder.Default;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SystemAttribute<T> : SystemAttribute where T : BaseSystemGroup
{
    public SystemAttribute() : base(typeof(T)) { }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RunBeforeAttribute(Type targetSystemType) : Attribute
{
    public Type TargetType { get; } = targetSystemType;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RunAfterAttribute(Type targetSystemType) : Attribute
{
    public Type TargetType { get; } = targetSystemType;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class TargetWorldAttribute(Type worldMarkerType) : Attribute
{
    public Type WorldMarkerType { get; } = typeof(IWorldMarker).IsAssignableFrom(worldMarkerType)
        ? worldMarkerType
        : throw new ArgumentException("Type must implement IWorldMarker");
}

// Generická verze (Pro C# 11+)
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class TargetWorldAttribute<T> : TargetWorldAttribute where T : IWorldMarker
{
    // Předáme base konstruktoru pouze typeof(T), což je pro kompilátor konstanta!
    public TargetWorldAttribute() : base(typeof(T))
    {
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TopLevelGroupAttribute() : SystemAttribute(typeof(SystemRoot))
{
}

internal sealed class CoreSystemAttribute : Attribute
{
}
