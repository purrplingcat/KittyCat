using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Graphics;
using System;
using System.Reflection;
using KittyCat.Services;
using KittyCat.Services.Options;
using KittyCat.Scenes;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Systems.Builder;
using KittyCat.Ecs.Systems;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Ecs.DI;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KittyCat.Configuration;

public class TestGroup : BaseSystemGroup { }
public class SecondGroup : BaseSystemGroup { }
public class ThirdGroup : BaseSystemGroup { }

public class KittyCatGameServices : IServiceConfiguration
{
    public Assembly Assembly => GetType().Assembly;

    protected virtual ContentManager GetDefaultContentManager(IServiceProvider provider)
    {
        return provider.GetRequiredService<ContentManagerProvider>().Default;
    }

    public void Configure(IServiceCollection services)
    {
        // Register world and their systems
        static void ConfigureSystems(IWorldBuilder builder)
        {
            builder.Registry.Add<PhysicsSystem>();
            builder.Registry.GetOrCreate<UpdateSystemGroup>()
                 .Add<TestGroup>()
                 .Add<PhysicsCleanupSystem>(SystemOrder.Last);
        }


        services.AddWorld()
                .AddModule(ConfigureSystems);

        //services.AddSystemRoot(ConfigureSystems);
        //services.AddRenderPipeline<World>(builder => { });

        // Register Game helpers & tools
        services.AddSingleton(GetDefaultContentManager)
                .Expose<Resolution, GraphicsManager>(source => source.Resolution);
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInitScene<TScene>(this IServiceCollection services) where TScene : Scene
    {
        return services.Configure<SceneManagerOptions>(options => options.InitialSceneType = typeof(TScene));
    }
}
