using Friflo.Engine.ECS.Systems;
using PurrplingCore.Toolkit.Collections;
using System.Diagnostics.CodeAnalysis;

namespace PurrplingCore.Ecs.Systems.Builder;

public sealed class SystemTreeConfiguration
{
    private readonly Hierarchy<Type> _hierarchy = new();

    public void AddChild<TParent, TChild>()
        where TParent : SystemGroup
        where TChild : BaseSystem
    {
        _hierarchy.AddChild(typeof(TParent), typeof(TChild));
    }

    public void AddChild(Type parentType, Type childType)
    {
        _hierarchy.AddChild(parentType, childType);
    }

    public bool TryGetGroup(Type nodeType, [MaybeNullWhen(false)] out IReadOnlyCollection<Type> childrenTypes)
    {
        return _hierarchy.TryGetChildren(nodeType, out childrenTypes);
    }
}
