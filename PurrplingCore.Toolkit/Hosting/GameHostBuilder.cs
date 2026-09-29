using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting.Services;
using System.Diagnostics;
using System.Reflection;


namespace PurrplingCore.Toolkit.Hosting;

public class GameHostBuilder : ApplicationHostBuilder, IGameHostBuilder
{
    public const string GameTypePropertyKey = "GameType";

    private bool _hasGame;
    private readonly GameFeatureCollection _gameFeatures;
    private GameVersion _gameVersion = GameVersion.Empty;

    public IGameFeatureCollection GameFeatures => _gameFeatures;

    public GameHostBuilder(string[]? args = null) : base(args)
    {
        _gameFeatures = new GameFeatureCollection();
        if (!GameCrashLogger.IsRegistered)
        {
            GameCrashLogger.Register(Environment);
        }
    }

    /// <inheritdoc />
    public IGameHostBuilder AddGame<TGame>() where TGame : class, IGame
    {
        if (_hasGame)
        {
            GameHostBuildException.ThrowGameAlreadyAddedException();
        }

        _hasGame = true;
        _gameVersion = GameVersion.Of<TGame>();
        Environment.ApplicationName = _gameVersion.Name;
        Services.AddServiceModule(new AssemblyServices(typeof(TGame).Assembly));
        Services.AddServiceModule<GameServices<TGame>>();

        ConfigureGameHostServices<TGame>();
        this.SetProperty(GameTypePropertyKey, typeof(TGame));

        return this;
    }

    private void ConfigureGameHostServices<TGame>() where TGame : class, IGame
    {
        foreach (var attribute in typeof(TGame).GetCustomAttributes<GameHostServicesAttribute>())
        {
            this.ConfigureHostServices(attribute.ConfigurationType);
        }
    }

    /// <inheritdoc />
    public new IGameHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        where TContainerBuilder : notnull
    {
        base.UseServiceProviderFactory(factory);
        return this;
    }

    protected override void AddCommonServices(IServiceCollection services)
    {
        services.AddSingleton(_gameVersion);
        services.AddSingleton<IGameFeatureCollection>(_gameFeatures);

        base.AddCommonServices(services);
    }

    /// <summary>
    /// Build the <see cref="GameHost"/> instance with hosted game and services.
    /// This method can only be called once.
    /// </summary>
    /// <returns>The <see cref="GameHost"/> instance</returns>
    public override GameHost Build()
    {
        if (!_hasGame)
            GameHostBuildException.ThrowNoGameAddedException();

        return (GameHost)base.Build();
    }

    protected override ApplicationHost CreateHost(IServiceProvider provider, IServiceCollection applicationServices)
    {
        return new GameHost(provider, applicationServices, Environment, _gameVersion, Properties);
    }

    protected override void OnHostCreated(ApplicationHost host)
    {
        base.OnHostCreated(host);
        Properties[typeof(GameHost)] = host;
        _gameFeatures.Freeze();
    }
}

public sealed class GameHostBuildException : InvalidOperationException
{
    public GameHostBuildException() : base()
    {
    }
    
    public GameHostBuildException(string message) : base(message)
    {
    }

    public GameHostBuildException(string message, Exception innerException) : base(message, innerException) 
    {
    }

    [StackTraceHidden]
    public static void ThrowNoGameAddedException()
    {
        throw new GameHostBuildException(
            "No game has added to the host! " +
            "Did you call IGameHostBuilder.AddGame properly?"
        );
    }

    [StackTraceHidden]
    public static void ThrowGameAlreadyAddedException()
    {
        throw new InvalidOperationException("Game is already added to the host!");
    }
}

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection Clone(this IServiceCollection serviceCollection)
    {
        IServiceCollection clone = new ServiceCollection();
        foreach (var service in serviceCollection)
        {
            clone.Add(service);
        }
        return clone;
    }
}

public static class AssemblyExtensions
{
    public static bool IsDebugBuild(this Assembly assembly)
    {
        // Vytáhneme atribut z metadata assembly
        var attributes = assembly.GetCustomAttributes(typeof(DebuggableAttribute), false);

        if (attributes.Length == 0)
            return false;

        var debuggable = (DebuggableAttribute)attributes[0];

        // Zásadní rozdíl: Debug build má vždy zakázané kompilátorové optimalizace, 
        // aby se dalo krokovat řádek po řádku. Release je má povolené (tento flag chybí).
        return debuggable.DebuggingFlags.HasFlag(DebuggableAttribute.DebuggingModes.DisableOptimizations);
    }
}

public static class HostDefaults
{
    /// <summary>
    /// The configuration key used to set <see cref="IHostEnvironment.ApplicationName"/>.
    /// </summary>
    public static readonly string ApplicationKey = "applicationName";

    /// <summary>
    /// The configuration key used to set <see cref="IHostEnvironment.EnvironmentName"/>.
    /// </summary>
    public static readonly string EnvironmentKey = "environment";

    /// <summary>
    /// The configuration key used to set <see cref="IHostEnvironment.ContentRootPath"/>
    /// and <see cref="IHostEnvironment.ContentRootFileProvider"/>.
    /// </summary>
    public static readonly string ContentRootKey = "contentRoot";
}
