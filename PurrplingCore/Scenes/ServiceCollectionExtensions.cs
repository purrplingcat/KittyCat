using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Xna.Framework;
using System;

namespace PurrplingCore.Scenes;

public static class ServiceCollectionExtensions
{
    /// <summary>Opts a game into scene management and registers its drawable game component.</summary>
    public static IServiceCollection AddSceneManager(this IServiceCollection services)
    {
        services.AddOptions<SceneManagerOptions>();
        services.TryAddSingleton<SceneProvider>();
        services.TryAddSingleton<SceneManager>();
        services.AddSingleton<IGameComponent>(provider => provider.GetRequiredService<SceneManager>());
        return services;
    }

    public static IServiceCollection AddScene<TScene>(this IServiceCollection services) where TScene : Scene
    {
        services.TryAddSingleton<ISceneProvider<TScene>, SceneFactory<TScene>>();

        return services;
    }

    public static IServiceCollection AddPersistentScene<TScene>(this IServiceCollection services) where TScene : Scene
    {
        services.TryAddSingleton<ISceneProvider<TScene>, PersistentSceneFactory<TScene>>();

        return services;
    }

    public static ISceneProvider<TScene> GetSceneFactory<TScene>(this IServiceProvider provider) where TScene : Scene
    {
        return provider.GetRequiredService<ISceneProvider<TScene>>();
    }
}
