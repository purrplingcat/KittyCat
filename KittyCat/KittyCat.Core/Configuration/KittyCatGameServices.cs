using KittyCat.Core;
using KittyCat.Ecs.Systems;
using KittyCat.Scenes;
using KittyCat.Services;
using KittyCat.Services.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Graphics;
using PurrplingCore.Toolkit.Hosting;
using System;
using System.Reflection;
using static Microsoft.Extensions.DependencyInjection.ActivatorUtilities;

namespace KittyCat.Configuration;

public class TestGroup : BaseSystemGroup { }
public class SecondGroup : BaseSystemGroup { }
public class ThirdGroup : BaseSystemGroup { }

public class KittyCatGameServices : IServicesConfiguration
{
    public Assembly Assembly => GetType().Assembly;

    protected virtual ContentManager GetDefaultContentManager(IServiceProvider provider)
    {
        return provider.GetRequiredService<IContentManagerProvider>().ContentManager;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Register world and their systems
        static void ConfigureSystems(IWorldBuilder builder)
        {
            builder.Registry.Register<PhysicsSystem>();
            builder.Registry.GetOrCreateGroup<UpdateSystemGroup>()
                 .Add<TestGroup>()
                 .Add<PhysicsCleanupSystem>(SystemOrder.Last);
        }

        services.AddWorld()
                .AddModule(ConfigureSystems);

        //services.AddSystemRoot(ConfigureSystems);
        //services.AddRenderPipeline<World>(builder => { });                
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInitScene<TScene>(this IServiceCollection services) where TScene : Scene
    {
        return services.Configure<SceneManagerOptions>(options => options.InitialSceneType = typeof(TScene));
    }

    public static IGameHostBuilder UsePurrplingCore(this IGameHostBuilder builder)
    {
        builder.Services.AddWorld();
        builder.Services.AddStartup(GetServiceOrCreateInstance<VirtualFileSystemStartup>);
        builder.Services.Expose<ContentManager, IContentManagerProvider>(provider => provider.ContentManager)
                        .Expose<Resolution, GraphicsManager>(source => source.Resolution);

        return builder;
    }
}
