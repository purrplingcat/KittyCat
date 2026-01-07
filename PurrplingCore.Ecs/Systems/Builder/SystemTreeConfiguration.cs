using Friflo.Engine.ECS.Systems;
using PurrplingCore.Toolkit.Collections;
using System.Diagnostics.CodeAnalysis;

namespace PurrplingCore.Ecs.Systems.Builder;

public sealed class SystemTreeConfiguration
{
    private readonly Hierarchy<Type> _hierarchy = new();
    private readonly Dictionary<Type, IReadOnlyCollection<Type>> _sortedHierarchy = [];

    public void AddChild<TParent, TChild>()
        where TParent : SystemGroup
        where TChild : BaseSystem
    {
        _hierarchy.AddChild(typeof(TParent), typeof(TChild));
    }

    public void AddChild(Type parentType, Type childType)
    {
        if (_hierarchy.AddChild(parentType, childType)) 
        { 
            _sortedHierarchy.Remove(parentType);
        }
    }

    public bool TryGetGroup(Type nodeType, [MaybeNullWhen(false)] out IReadOnlyCollection<Type> childrenTypes)
    {
        if (!_sortedHierarchy.ContainsKey(nodeType) && _hierarchy.TryGetChildren(nodeType, out childrenTypes))
        {
            childrenTypes = [.. SystemGroupFactory.OrderByDependency(childrenTypes)];
            _sortedHierarchy.Add(nodeType, childrenTypes);
            return true;
        }
        
        return _sortedHierarchy.TryGetValue(nodeType, out childrenTypes);
    }
}
