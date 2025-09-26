using Friflo.Engine.ECS.Systems;
using System.Diagnostics.CodeAnalysis;

namespace PurrplingCore.Toolkit.Systems;

public sealed class SystemTreeConfiguration
{
    private readonly Dictionary<Type, HashSet<Type>> _hierarchy = [];

    public void AddChild<TParent, TChild>()
        where TParent : SystemGroup
        where TChild : BaseSystem
    {
        if (!_hierarchy.TryGetValue(typeof(TParent), out var children))
        {
            children = [];
            _hierarchy[typeof(TParent)] = children;
        }
        children.Add(typeof(TChild));
    }

    public bool TryGetGroup(Type nodeType, [MaybeNullWhen(false)] out IReadOnlyCollection<Type> childrenTypes)
    {
        if (_hierarchy.TryGetValue(nodeType, out var hashset))
        {
            childrenTypes = hashset;
            return true;
        }

        childrenTypes = null;
        return false;
    }
}
