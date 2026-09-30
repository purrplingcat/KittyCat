using KittyCat.Ecs.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.Hosting;
using System;
using System.Reflection;

namespace KittyCat.Configuration;

public class TestGroup : BaseSystemGroup { }
public class SecondGroup : BaseSystemGroup { }
public class ThirdGroup : BaseSystemGroup { }

public class KittyCatGameServices : IServiceModule
{
    public Assembly Assembly => GetType().Assembly;

    protected virtual ContentManager GetDefaultContentManager(IServiceProvider provider)
    {
        return provider.GetRequiredService<IContentManagerProvider>().ContentManager;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Register world and their systems
        static void ConfigureWorld(IWorldBlueprint world)
        {
            world.AddSystem<PhysicsSystem>();
            world.Systems.In<UpdateSystemGroup>()
                         .Add<PhysicsCleanupSystem>(SystemOrder.Last)
                         .Add<TestGroup>()
                         .Add<ThirdGroup>();
            world.Systems.In<TestGroup>()
                         .Add<SecondGroup>();
        }

        services.AddWorld(ConfigureWorld);
    }
}
