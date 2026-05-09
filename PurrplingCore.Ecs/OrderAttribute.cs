using Friflo.Engine.ECS.Systems;

namespace PurrplingCore.Ecs;

[AttributeUsage(AttributeTargets.Class)]
public class OrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}

[AttributeUsage(AttributeTargets.Class)]
public class TagsAttribute(params object[] tags) : Attribute
{
    public object[] Tags { get; } = tags;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RunBeforeAttribute(Type targetType) : Attribute
{
    public Type TargetType { get; } = targetType;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RunAfterAttribute(Type targetType) : Attribute
{
    public Type TargetType { get; } = targetType;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RunBeforeAttribute<TSystem>() : RunBeforeAttribute(typeof(TSystem)) where TSystem : BaseSystem
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RunAfterAttribute<TSystem>() : RunAfterAttribute(typeof(TSystem)) where TSystem : BaseSystem
{
}

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class UpdateInGroupAttribute(Type groupType) : Attribute
{
    public Type GroupType { get; } = groupType;
}

public static class Order
{
    public const int First = int.MinValue;
    public const int Earlier = -10000;
    public const int Early = -1000;
    public const int Default = 0;
    public const int Late = 1000;
    public const int Later = 10000;
    public const int Last = int.MaxValue;
}
