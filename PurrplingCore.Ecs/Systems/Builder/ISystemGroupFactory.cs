using Friflo.Engine.ECS.Systems;

namespace PurrplingCore.Ecs.Systems.Builder;

public interface ISystemGroupFactory
{
    TGroup CreateGroup<TGroup>() where TGroup : SystemGroup;
}

public interface ISystemGroupFactory<out TGroup> where TGroup : SystemGroup
{
    TGroup CreateGroup();
}
