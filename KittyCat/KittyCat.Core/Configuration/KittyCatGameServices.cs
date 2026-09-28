using KittyCat.Core;
using KittyCat.Ecs.Systems;
using KittyCat.Scenes;
using KittyCat.Services;
using KittyCat.Services.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Graphics;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Vfs;
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
            services.AddSingleton<ContentPackageManager>();
            services.AddStartup(GetServiceOrCreateInstance<VirtualFileSystemStartup>);
            services.AddStartup(provider => provider.GetRequiredService<ContentPackageManager>());
        }
    }

    /// <summary>
    /// Registers KittyCat runtime services with a provider that can enumerate bundled packages.
    /// </summary>
    /// <param name="packageFiles">
    /// Optional platform provider. Desktop defaults to the application base directory; packaged
    /// platforms should supply their own provider over application assets.
    /// </param>
    public static IGameHostBuilder UsePurrplingCore(
        this IGameHostBuilder builder,
        IFileProvider? packageFiles = null)
    {
        builder.Properties["PurrplingCore"] = true;
        builder.AddServiceModule<PurrplingCore>();
        packageFiles ??= new PhysicalFileProvider(AppContext.BaseDirectory);
        builder.Services.AddKeyedSingleton(ContentPackageManager.FileProviderKey, packageFiles);
        builder.Services.AddVfs();
        builder.Services.Expose<ContentManager, IContentManagerProvider>(provider => provider.ContentManager)
                        .Expose<Resolution, GraphicsManager>(source => source.Resolution);

        return builder;
    }
}
