using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Toolkit;
using System.Xml.Linq;

namespace PurrplingCore.Ecs.Systems;

internal sealed class TaggedSystemGroup<TTag>() : SystemGroup(typeof(TTag).FullName)
{
}

internal sealed class TaggedSystemRoot<TTag>() : SystemRoot(typeof(TTag).FullName)
{
}

public abstract class BaseSystemGroup : SystemGroup
{
    private readonly string _name;

    public override string Name => _name;

    public BaseSystemGroup() : base(nameof(BaseSystemGroup))
    {
        _name = GetType().Name;
    }

    protected BaseSystemGroup(string name) : base(name)
    {
        _name = name;
    }

    public override string ToString() => $"'{_name}' Group - child systems: {ChildSystems.Count}";
}
