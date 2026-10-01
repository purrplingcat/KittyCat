using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;

namespace PurrplingCore.Toolkit;

public enum PlatformType
{
    Unknown,
    Desktop,
    Browser,
    Mobile,
    Tv,
}

[GameServices<CoreGameServices>]
[Alias(typeof(Game), typeof(Microsoft.Xna.Framework.Game))]
public abstract class Game: Microsoft.Xna.Framework.Game, IGame
{
    private readonly IServiceProvider _provider;
    private readonly GraphicsDeviceManager _graphicsManager;
    private readonly GameClock _gameClock;
    private readonly ILogger _logger;
    private GameVersion? _version;
    private bool _isInitialized;
    private bool _isRunning;
    private bool _disposed;

    private sealed class CoreGameServices : IServiceModule
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .Expose<IGameClock, Game>(game => game.Clock)
                .Expose<GameServiceContainer, Game>(game => game.Services)
                .ExposeMonoGameService<IGraphicsDeviceService>()
                .ExposeMonoGameService<IGraphicsDeviceManager>()
                .ExposeMonoGameService<GraphicsDeviceManager>();
        }

    }

    public event EventHandler<EventArgs>? Exited;
    public event EventHandler<EventArgs>? Launched;
    public event EventHandler<EventArgs>? Initializing;
    public event EventHandler<EventArgs>? Initialized;

    public GraphicsDeviceManager GraphicsDeviceManager => _graphicsManager;

    /// <summary>
    /// The name of the game, used for display purposes.
    /// </summary>
    public virtual string Title { get; set; } = "PurrplingCore";

    /// <summary>
    /// Indicates if the game is initialized by <see cref="Initialize"/> method.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    protected ILogger Logger => _logger;

    /// <summary>
    /// Indicates if the game is running on a mobile platform.
    /// </summary>
    public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    /// <summary>
    /// Indicates if the game is running on a desktop platform.
    /// </summary>
    public readonly static bool IsDesktop = OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

    public static PlatformType PlatformType
    {
        get
        {
            if (OperatingSystem.IsTvOS()) return PlatformType.Tv;
            if (OperatingSystem.IsBrowser()) return PlatformType.Browser;
            if (IsMobile) return PlatformType.Mobile;
            if (IsDesktop) return PlatformType.Desktop;

            return PlatformType.Unknown;
        }
    }

    public bool IsRunning => _isRunning;

    internal IGameClock Clock => _gameClock;

    public Game(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _gameClock = new GameClock();
        _graphicsManager = new GraphicsDeviceManager(this);
        IsMouseVisible = true; // Default to showing the mouse cursor
        Content.RootDirectory = "Content";

        // Create game loggr
        var loggerFactory = provider.GetService<ILoggerFactory>();
        _logger = loggerFactory?.CreateLogger(GetType()) ?? NullLogger.Instance;

        // Resolve content manager
        var content = provider.GetService<IContentManagerProvider>();
        if (content is not null)
        {
            Content = content.ContentManager;
        }

        _logger.LogTrace("Game created: {Game}", this);
    }

    private event EventHandler<EventArgs>? _exiting;

    event EventHandler<EventArgs>? IGame.Exiting
    {
        add
        {
            _exiting += value;
        }

        remove
        {
            _exiting -= value;
        }
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        base.OnExiting(sender, args);

        if (!args.Cancel) 
        {
            _exiting?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Initializes the game, including setting up localization and adding the 
    /// initial screens to the ScreenManager.
    /// </summary>
    protected override void Initialize()
    {
        Initializing?.Invoke(this, EventArgs.Empty);

        foreach (var gameComponent in _provider.GetServices<IGameComponent>())
        {
            _logger.LogTrace("Adding game component: {GameComponent}", gameComponent);
            Components.Add(gameComponent);
        }

        Window.Title = Title;
        
        base.Initialize();
        _isInitialized = true;
        Initialized?.Invoke(this, EventArgs.Empty);
        _logger.LogTrace("Game initialized!");
    }

    protected override void Update(GameTime gameTime)
    {
        _gameClock.RecordUpdate(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _gameClock.RecordDraw(gameTime);
        base.Draw(gameTime);
    }

    protected override void BeginRun()
    {
        base.BeginRun();
        _isRunning = true;
        Launched?.Invoke(this, EventArgs.Empty);
        _logger.LogInformation("Game started!");
    }

    protected override void EndRun()
    {
        base.EndRun();
        _isRunning = false;
        Exited?.Invoke(this, EventArgs.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
            base.Dispose(disposing);
        }
    }

    protected T GetRequiredService<T>() where T : class
    {
        return _provider.GetRequiredService<T>();
    }

    protected T? GetService<T>() where T : class
    {
        return _provider.GetService<T>();
    }

    public override string ToString()
    {
        _version ??= _provider.GetService<GameVersion>() ?? new GameVersion(GetType());

        return $"{_version.Name} {_version.GetVersionString()}";
    }
}
