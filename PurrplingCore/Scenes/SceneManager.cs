using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Graphics;
using PurrplingCore.Toolkit.Graphics;

namespace PurrplingCore.Scenes;

/// <summary>Activates, updates, renders, and disposes scenes owned by a game.</summary>
public class SceneManager(
    PurrplingCore.Toolkit.Game game,
    SceneProvider sceneProvider,
    GraphicsManager graphics,
    ILogger<SceneManager> logger,
    IOptions<SceneManagerOptions> options) : DrawableGameComponent(game)
{
    private readonly PurrplingCore.Toolkit.Game _game = game;
    private readonly SceneProvider _sceneProvider = sceneProvider;
    private readonly GraphicsManager _graphics = graphics;
    private readonly ILogger<SceneManager> _logger = logger;
    private readonly SceneManagerOptions _options = options.Value;
    private readonly List<Scene> _scenes = [];
    private readonly List<Scene> _scenesToUpdate = [];
    private readonly GameTime _slowTime = new(TimeSpan.Zero, TimeSpan.Zero, isRunningSlowly: true);
    private readonly TimeSpan _bgTickDelta = TimeSpan.FromMilliseconds(1000 / 10);
    private Scene? _currentScene;
    private bool _disposed;

    public List<Scene> Scenes => _scenes;

    public event Action<Scene?>? SceneSwitched;

    public void SwitchScene(Scene newScene)
    {
        _currentScene?.Deactivate();
        _currentScene = newScene ?? throw new ArgumentNullException(nameof(newScene));
        _currentScene.Activate();
        OnSceneSwitched();
    }

    public void SwitchScene<T>() where T : Scene => SwitchScene(_sceneProvider.GetScene<T>());

    public void SwitchScene(Type sceneType)
    {
        ArgumentNullException.ThrowIfNull(sceneType);

        if (!typeof(Scene).IsAssignableFrom(sceneType))
        {
            throw new ArgumentException($"Type {sceneType.Name} is not a valid Scene type.", nameof(sceneType));
        }

        SwitchScene(_sceneProvider.GetScene(sceneType));
    }

    protected virtual void OnSceneSwitched()
    {
        _game.Window.Title = _currentScene?.Title != null
            ? $"{_currentScene.Title} - {_game.Title}"
            : _game.Title;

        SceneSwitched?.Invoke(_currentScene);
        _logger.LogDebug("Switched to scene: {SceneName}", _currentScene?.Name ?? "<null>");
    }

    public override void Initialize()
    {
        _game.Window.Title = $"<NONE SCENE> {_game.Title}";

        if (_options.InitialSceneType != null)
        {
            SwitchScene(_sceneProvider.GetScene(_options.InitialSceneType));
        }
    }

    public override void Update(GameTime gameTime)
    {
        _slowTime.ElapsedGameTime += gameTime.ElapsedGameTime;
        _slowTime.TotalGameTime = gameTime.TotalGameTime;
        _currentScene?.Update(gameTime);

        if (_slowTime.ElapsedGameTime >= _bgTickDelta)
        {
            PrepareScenesToUpdate();
            foreach (var scene in _scenesToUpdate)
            {
                scene.Update(_slowTime);
            }

            _slowTime.ElapsedGameTime -= _bgTickDelta;
        }

        base.Update(gameTime);
    }

    private void PrepareScenesToUpdate()
    {
        _scenesToUpdate.Clear();
        foreach (var scene in _scenes)
        {
            if (scene != _currentScene)
            {
                _scenesToUpdate.Add(scene);
            }
        }
    }

    public override void Draw(GameTime gameTime)
    {
        if (_currentScene != null)
        {
            var scene = _currentScene.Render(gameTime);

            _graphics.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _graphics.SpriteBatch.Draw(scene, Color.White);
            _graphics.SpriteBatch.End();
        }

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_disposed) return;

        if (disposing)
        {
            foreach (var scene in _scenes)
            {
                scene.Dispose();
            }

            _scenes.Clear();
            _currentScene?.Dispose();
            _currentScene = null;
        }

        _disposed = true;
        _logger.LogDebug("SceneManager disposed.");
    }
}