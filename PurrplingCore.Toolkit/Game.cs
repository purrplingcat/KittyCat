using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Extensions;
using PurrplingCore.Toolkit.Messages;
using PurrplingCore.Toolkit.Messaging;

namespace PurrplingCore.Toolkit;

public enum PlatformType
{
    Unknown,
    Desktop,
    Browser,
    Mobile,
    Tv,
}
public abstract class Game: Microsoft.Xna.Framework.Game, IGame
{
    private readonly IServiceProvider _provider;
    private readonly IMessageBus? _bus;
    private readonly GraphicsDeviceManager _graphicsManager;
    private readonly ILogger _logger;
    private GameVersion? _version;
    private bool _isInitialized;
    private bool _isRunning;

    public event EventHandler<EventArgs>? Exited;

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

    public Game(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _graphicsManager = new GraphicsDeviceManager(this);
        _bus = provider.GetService<IMessageBus>();
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

    /// <summary>
    /// Initializes the game, including setting up localization and adding the 
    /// initial screens to the ScreenManager.
    /// </summary>
    protected override void Initialize()
    {
        var services = _provider.GetServices<IGameService>();
        services.ForEach(service => service.Initialize());

        foreach (var gameComponent in _provider.GetServices<IGameComponent>())
        {
            Components.Add(gameComponent);
        }

        _isInitialized = true;
        Window.Title = Title;
        
        base.Initialize();
        _bus?.Publish(new GameMessage(this, GameMessages.Initialized));
        _logger.LogTrace("Game initialized!");
    }

    /// <summary>
    /// Loads game content, such as textures and particle systems.
    /// </summary>
    protected override void LoadContent()
    {
        var services = _provider.GetServices<IGameService>();
        services.ForEach(service => service.LoadContent());
        base.LoadContent();
    }

    protected override void BeginRun()
    {
        base.BeginRun();
        _isRunning = true;
        _bus?.Publish(new GameMessage(this, GameMessages.Lanuched));
        _logger.LogInformation("Game started!");
    }

    protected override void EndRun()
    {
        base.EndRun();
        _isRunning = false;
        _bus?.Publish(new GameMessage(this, GameMessages.Exit));
        Exited?.Invoke(this, EventArgs.Empty);
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
