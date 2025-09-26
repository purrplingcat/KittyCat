using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Messaging;

using System.Reflection;

namespace PurrplingCore.Toolkit.DI;

internal class GameHostBuilder : IGameHostBuilder
{
    private readonly List<Action<IServiceCollection, GameHostBuilderContext>> _serviceActions = [];
    private readonly List<Action<ILoggingBuilder>> _loggingActions = [];
    private IServiceProviderFactory _serviceProviderFactory = new DefaultServiceProviderFactory();

    protected IServiceProviderFactory ServiceProviderFactory => _serviceProviderFactory;

    private class GameHostConfiguration : IServiceConfiguration
    {
        private GameHost CreateGameHost(IServiceProvider provider)
        {
            return new GameHost(
                provider,
                game: provider.GetService<IGame>() ?? throw new InvalidOperationException($"No Game service of '{typeof(IGame)}' found"),
                logger: provider.GetRequiredService<ILogger<GameHost>>(),
                startupServices: [.. provider.GetServices<IStartupService>().OrderBy(s => s.Order)],
                cleanupServices: [.. provider.GetServices<ICleanupService>().OrderBy(s => s.Order)]
            );
        }

        public void Configure(IServiceCollection services)
        {
            services.TryAddSingleton(CreateGameHost);
            services.TryAddSingleton<IMessageBus, DefaultMessageBus>();
            services.TryAddTransient(typeof(Lazy<>), typeof(LazyService<>));
        }
    }

    private class LoggingBuilder(IServiceCollection services) : ILoggingBuilder
    {
        public IServiceCollection Services => services;
    }

    public IGameHostBuilder ConfigureServices(Action<IServiceCollection, GameHostBuilderContext> configureDelegate)
    {
        _serviceActions.Add(configureDelegate);
        return this;
    }

    public IGameHostBuilder ConfigureLogging(Action<ILoggingBuilder> configureDelegate)
    {
        _loggingActions.Add(configureDelegate);
        return this;
    }

    public IGameHostBuilder UseServiceProviderFactory(IServiceProviderFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory, nameof(factory));

        _serviceProviderFactory = factory;
        return this;
    }

    private void ConfigureLogging(ILoggingBuilder builder)
    {
        _loggingActions.ForEach(action => action(builder));
    }

    protected virtual void ConfigureServices(IServiceCollection services, GameHostBuilderContext context)
    {
        _serviceActions.ForEach(action => action(services, context));
        services.AddLogging(ConfigureLogging);
        services.AddConfiguration(new GameHostConfiguration());
    }

    protected ILoggerFactory CreateLoggerFactory() => LoggerFactory.Create(ConfigureLogging);

    private IServiceProvider BuildServiceProvider(ServiceCollection services, GameHostBuilderContext context)
    {
        ConfigureServices(services, context);

        return ServiceProviderFactory.CreateServiceProvider(services);
    }

    public GameHost Build()
    {
        using var loggerFactory = CreateLoggerFactory();
        var context = new GameHostBuilderContext(this, loggerFactory, Assembly.GetExecutingAssembly());
        var services = new ServiceCollection();
        var provider = BuildServiceProvider(services, context);

        return provider.GetRequiredService<GameHost>();
    }
}
