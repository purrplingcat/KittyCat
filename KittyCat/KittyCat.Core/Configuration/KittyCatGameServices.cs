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
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Graphics;
using PurrplingCore.Toolkit.Hosting;
using System;
using System.Linq;
using System.Reflection;
using static Microsoft.Extensions.DependencyInjection.ActivatorUtilities;

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
            world.Systems.Add<PhysicsSystem>();
            world.Systems.In<UpdateSystemGroup>()
                 .Add<TestGroup>()
                 .Add<PhysicsCleanupSystem>(SystemOrder.Last);
        }

        services.AddWorld(ConfigureWorld);             
    }
}

public static class ServiceCollectionExtensions
{
    private class PurrplingCore : IServiceModule
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAssetRegistry();
            services.AddWorld();
            services.AddStartup(GetServiceOrCreateInstance<VirtualFileSystemStartup>);
        }
    }

    public static IGameHostBuilder UsePurrplingCore(this IGameHostBuilder builder)
    {
        builder.Properties["PurrplingCore"] = true;
        builder.AddServiceModule<PurrplingCore>();
        builder.Services.AddVfs();
        builder.Services.Expose<ContentManager, IContentManagerProvider>(provider => provider.ContentManager)
                        .Expose<Resolution, GraphicsManager>(source => source.Resolution);

        return builder;
    }
}
