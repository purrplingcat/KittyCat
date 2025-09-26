using KittyCat.Scenes;
using KittyCat.Scenes.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.DI;
using System;

namespace KittyCat.Extensions;

public static class ServiceCollectionExtensions
{
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
