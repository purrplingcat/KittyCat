using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace KittyCat.Scenes.Factories;

public interface ISceneProvider<TScene> : ISceneFactory where TScene : Scene
{
    event Action<Scene> SceneCreated;

    new TScene GetScene();
}

public interface ISceneFactory
{
    Scene GetScene();
}

internal class SceneFactory<TScene>(IServiceProvider provider) : ISceneProvider<TScene> where TScene : Scene
{
    private readonly IServiceProvider _provider = provider;
    private readonly ILogger _logger = provider.GetRequiredService<ILogger<SceneFactory<TScene>>>();

    public event Action<Scene>? SceneCreated;

    public virtual TScene GetScene()
    {
        var scope = _provider.CreateScope();
        var scene = ActivatorUtilities.CreateInstance<TScene>(scope.ServiceProvider);

        scene.Disposed += scope.Dispose;
        _logger.LogDebug("Created scene: {SceneName} ({SceneType})", scene.Name, scene.GetType());

        SceneCreated?.Invoke(scene);
        return scene;
    }

    Scene ISceneFactory.GetScene()
    {
        return GetScene();
    }
}

internal class PersistentSceneFactory<TScene>(IServiceProvider provider) : SceneFactory<TScene>(provider) where TScene : Scene
{
    private TScene? _scene;
    private readonly object _lock = new();

    public override TScene GetScene()
    {
        lock (_lock)
        {

            if (_scene is null)
            {
                _scene = base.GetScene();
                _scene.Disposed += () => _scene = null;
            }

            return _scene;
        }
    }
}
