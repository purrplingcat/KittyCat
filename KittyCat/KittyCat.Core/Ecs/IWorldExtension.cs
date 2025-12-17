using Friflo.Engine.ECS;

namespace KittyCat.Ecs;

public interface IWorldExtension<TExtension> where TExtension : class
{
    void Destroy(EntityStore store);
    TExtension GetFor(EntityStore store);
    TExtension GetFor(string storeName);
}
