using KittyCat.Core.Scenes;
using KittyCat.Core.Scenes.Factories;
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Toolkit.DI;
using System;

namespace KittyCat.Core.Services;

[Singleton]
public class SceneProvider (IServiceProvider provider)
{
    private readonly IServiceProvider _provider = provider;

    public TScene GetScene<TScene>() where TScene : Scene
    {
        if (!typeof(Scene).IsAssignableFrom(typeof(TScene)))
        {
            throw new ArgumentException($"Type '{typeof(TScene).FullName}' does not inherit from Scene.");
        }

        return _provider.GetRequiredService<ISceneProvider<TScene>>().GetScene();
    }

    public Scene GetScene(Type type)
    {
        if (!typeof(Scene).IsAssignableFrom(type))
        {
            throw new ArgumentException($"Type '{type.FullName}' does not inherit from Scene.", nameof(type));
        }

        var sceneProviderType = typeof(ISceneProvider<>).MakeGenericType(type);
        var sceneProvider = (ISceneFactory)_provider.GetRequiredService(sceneProviderType);

        return sceneProvider.GetScene();
    }
}
