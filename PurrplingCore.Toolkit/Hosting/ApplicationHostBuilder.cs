using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Messaging;
using System.Diagnostics.CodeAnalysis;

namespace PurrplingCore.Toolkit.Hosting;

public class ApplicationHostBuilder : IApplicationHostBuilder
{
    private readonly ServiceCollection _services = [];
    private readonly ConfigurationManager _config = new();
    private readonly HostEnvironment _env = new();
    private IServiceFactoryAdapter _serviceProviderFactory;
    private bool _hostBuilt;

    public IServiceCollection Services => _services;
    public IConfigurationManager Configuration => _config;
    public IHostEnvironment Environment => _env;
    public ILoggingBuilder Logging { get; }
    public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

    public ApplicationHostBuilder(string[]? args = null)
    {
        Logging = new LoggingBuilder(_services);
        _config.AddEnvironmentVariables(prefix: "DOTNET_");
        _config.AddCommandLine(args ?? []);
        _env.EnvironmentName = _config[HostDefaults.EnvironmentKey] ?? _env.EnvironmentName;

        var options = _env.IsDevelopment()
            ? new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }
            : new ServiceProviderOptions();
        UseServiceProviderFactory(new DefaultServiceProviderFactory(options));
    }

    [MemberNotNull(nameof(_serviceProviderFactory))]
    public IApplicationHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        where TContainerBuilder : notnull
    {
        ArgumentNullException.ThrowIfNull(factory);

        _serviceProviderFactory = new ServiceProviderFactoryAdapter<TContainerBuilder>(factory);
        return this;
    }

    protected virtual void AddCommonServices(IServiceCollection services)
    {
        services.AddLogging();
        services.TryAddSingleton(_serviceProviderFactory);
        services.TryAddSingleton<IConfiguration>(_config);
        services.TryAddSingleton<IHostEnvironment>(_env);
        services.TryAddSingleton<IMessageBus, NullMessageBus>();
        services.TryAddTransient(typeof(Lazy<>), typeof(LazyService<>));
    }

    public virtual ApplicationHost Build()
    {
        if (_hostBuilt)
        {
            throw new InvalidOperationException($"{GetType().Name} can only build once");
        }
        _hostBuilt = true;

        AddCommonServices(_services);
        _services.MakeReadOnly();

        var applicationServices = _services.Clone();
        var provider = _serviceProviderFactory.CreateServiceProvider(_services);
        var host = CreateHost(provider, applicationServices);

        host.Initialize();
        OnHostCreated(host);

        return host;
    }

    protected virtual ApplicationHost CreateHost(IServiceProvider provider, IServiceCollection applicationServices)
    {
        return new ApplicationHost(provider, applicationServices, Properties);
    }

    protected virtual void OnHostCreated(ApplicationHost host)
    {
        Properties[typeof(ApplicationHost)] = host;
    }

    private sealed class LoggingBuilder(IServiceCollection services) : ILoggingBuilder
    {
        public IServiceCollection Services => services;
    }
}