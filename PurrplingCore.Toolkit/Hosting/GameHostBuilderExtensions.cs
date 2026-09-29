using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Hosting.Services;

namespace PurrplingCore.Toolkit.Hosting;

public static class GameHostBuilderExtensions
{
    private const string ConfiguredHostServiceModulesKey = "ConfiguredHostServiceModules";

    public static IGameHostBuilder SetProperty<TValue>(
        this IGameHostBuilder builder,
        object key,
        TValue value)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(key);

        builder.Properties[key] = value;
        return builder;
    }

    public static TValue? GetProperty<TValue>(this IGameHostBuilder builder, object key)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(key);

        if (!builder.Properties.TryGetValue(key, out var value))
        {
            return default;
        }

        if (value is TValue property)
        {
            return property;
        }

        throw new InvalidOperationException(
            $"Property '{key}' contains '{value.GetType()}', not '{typeof(TValue)}'.");
    }

    /// <summary>
    /// Applies a module immediately to the bootstrap service collection.
    /// Each module type is applied at most once.
    /// </summary>
    public static IGameHostBuilder ConfigureHostServices<TModule>(this IGameHostBuilder builder)
        where TModule : class, IServiceModule, new()
        => builder.ConfigureHostServices(typeof(TModule));

    internal static IGameHostBuilder ConfigureHostServices(
        this IGameHostBuilder builder,
        Type moduleType)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(moduleType);

        var configuredModules = builder.GetProperty<HashSet<Type>>(ConfiguredHostServiceModulesKey);
        if (configuredModules is null)
        {
            configuredModules = [];
            builder.SetProperty(ConfiguredHostServiceModulesKey, configuredModules);
        }

        if (!configuredModules.Add(moduleType))
        {
            return builder;
        }

        try
        {
            var module = (IServiceModule)Activator.CreateInstance(moduleType)!;
            module.ConfigureServices(builder.Services);
            return builder;
        }
        catch
        {
            configuredModules.Remove(moduleType);
            throw;
        }
    }

    public static IGameHostBuilder AddServiceModule(this IGameHostBuilder builder, IServiceModule module)
    {
        builder.Services.AddSingleton(module);
        return builder;
    }

    public static IGameHostBuilder AddServiceModule<T>(this IGameHostBuilder builder) where T : class, IServiceModule
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IServiceModule, T>());
        return builder;
    }

    public static IGameHostBuilder AddServiceModule(this IGameHostBuilder builder, Action<IServiceCollection, IServiceProvider> configure)
    {
        builder.Services.AddTransient<IServiceModule>(sp => new LambdaServices(sp, configure));
        return builder;
    }

    public static IGameHostBuilder AddServiceModule<T>(this IGameHostBuilder builder, Func<IServiceProvider, T> factory) where T : class, IServiceModule
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IServiceModule, T>(factory));
        return builder;
    }

    public static IGameHostBuilder UseDefaultConfiguration(this IGameHostBuilder builder)
    {
        ConfigureDefault(builder.Configuration, builder.Logging, builder.Environment);
        return builder;
    }

    internal static void ConfigureApplicationDefaults(ApplicationHostBuilder builder)
    {
        ConfigureDefault(builder.Configuration, builder.Logging, builder.Environment);
    }

    private static void ConfigureDefault(IConfigurationManager configuration, ILoggingBuilder logging, IHostEnvironment environment)
    {
        // Configuration
        configuration
               .SetFileProvider(new VirtualFileProvider(environment))
               .AddJsonFile("appsettings.json", optional: true)
               .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true);
               

        // Default logging config
        logging
               .SetMinimumLevel(LogLevel.Information)
               .AddDefaultConfiguration(environment)
               .AddConfiguration(configuration.GetSection("Logging"));
    }

    private static ILoggingBuilder AddDefaultConfiguration(this ILoggingBuilder builder, IHostEnvironment env)
    {
        // Common settings
        builder.AddFilter("Microsoft", LogLevel.Warning)
               .AddFilter("System", LogLevel.Warning)
               .AddFilter("PurrplingCore.Ecs", LogLevel.Debug);

        // Development-only settings
        if (env.IsDevelopment())
        {
            // Enable debug logging for development
            builder.SetMinimumLevel(LogLevel.Debug)
                   .AddDebug();
        }

        return builder;
    }
}

public static partial class ServicesExtensions
{
    public static IServiceCollection AddServiceModule<T>(this IServiceCollection services, T instance) where T : class, IServiceModule
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IServiceModule>(instance));
        return services;
    }

    public static IServiceCollection AddServiceModule<T>(this IServiceCollection services) where T : class, IServiceModule
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IServiceModule, T>());
        return services;
    }

    public static IServiceCollection AddServiceModule<T>(this IServiceCollection services, Func<IServiceProvider, T> factory) where T : class, IServiceModule
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IServiceModule, T>(factory));
        return services;
    }

    public static IServiceCollection AddServiceModule(this IServiceCollection services, Func<IServiceProvider, IServiceModule> factory)
    {
        services.AddTransient(factory);
        return services;
    }
}
