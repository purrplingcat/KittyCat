using KittyCat.Ecs.Systems;
using PurrplingCore.Ecs.Systems.Builder;

namespace KittyCat.Configuration;

public static class SystemBuilderExtensions
{
    public static ISystemBuilder AddPhysicsSystems(this ISystemBuilder builder)
    {
        return builder.AddSystem<PhysicsSystem>()
                      .AddSystem<PhysicsCleanupSystem>();
    }
}
