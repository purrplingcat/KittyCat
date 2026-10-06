using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Toolkit;
using System.Xml.Linq;

namespace PurrplingCore.Ecs.Systems;

/// <summary>
/// A system group whose type is used as its identifying tag.
/// </summary>
internal sealed class TaggedSystemGroup<TTag>() : SystemGroup(typeof(TTag).FullName)
{
}

/// <summary>
/// A system root whose type is used as its identifying tag.
/// </summary>
internal sealed class TaggedSystemRoot<TTag>() : SystemRoot(typeof(TTag).FullName)
{
}

/// <summary>
/// A system group named after its derived type or an explicit name.
/// </summary>
public abstract class BaseSystemGroup : SystemGroup
{
    private readonly string _name;

    /// <summary>
    /// Gets the name assigned to this system group.
    /// </summary>
    public override string Name => _name;

    /// <summary>
    /// Initializes the group using its derived type name.
    /// </summary>
    public BaseSystemGroup() : base(nameof(BaseSystemGroup))
    {
        _name = GetType().Name;
    }

    /// <summary>
    /// Initializes the group with an explicit name.
    /// </summary>
    protected BaseSystemGroup(string name) : base(name)
    {
        _name = name;
    }

    /// <summary>
    /// Returns the group name and number of child systems.
    /// </summary>
    public override string ToString() => $"'{_name}' Group - child systems: {ChildSystems.Count}";
}
