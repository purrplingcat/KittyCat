using Friflo.Engine.ECS.Systems;
using KittyCat.Core.Ecs;
using KittyCat.Core.Ecs.Systems;
using KittyCat.Core.Extensions;
using KittyCat.Core.Scenes;
using KittyCat.Core.Services;
using KittyCat.Core.Services.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.DI.Configuration;
using PurrplingCore.Toolkit.Systems;
using PurrplingCore.Toolkit.Graphics;
using PurrplingCore.Toolkit.Rendering;
using System;
using System.Reflection;

namespace KittyCat.Core.Configuration;

public class KittyCatGameServices : IServiceConfiguration
{
    public Assembly Assembly => GetType().Assembly;

    protected virtual ContentManager GetDefaultContentManager(IServiceProvider provider)
    {
        return provider.GetRequiredService<ContentManagerProvider>().Default;
    }

    protected virtual void ConfigureSystems(ISystemBuilder builder)
    {
        builder.AddPhysicsSystems();
    }

    public void Configure(IServiceCollection services)
    {
        // Register world and their systems
        services.AddSingleton<World>();
        services.AddSystemRoot(ConfigureSystems);
        services.AddRenderPipeline<World>(builder => { });

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
