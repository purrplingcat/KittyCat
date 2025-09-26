using KittyCat.Ecs.Systems;
using PurrplingCore.Toolkit.Systems;

namespace KittyCat.Ecs;

public static class SystemBuilderExtensions
{
    public static ISystemBuilder AddPhysicsSystems(this ISystemBuilder builder)
    {
        return builder.AddSystem<PhysicsSystem>()
                      .AddSystem<PhysicsCleanupSystem>();
    }
}
