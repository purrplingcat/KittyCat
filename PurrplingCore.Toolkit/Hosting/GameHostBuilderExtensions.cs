using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Hosting.Services;

namespace PurrplingCore.Toolkit.Hosting;

/// <summary>
/// Provides extension methods for configuring game hosts.
/// </summary>
public static class GameHostBuilderExtensions
{
    private const string ConfiguredHostServiceModulesKey = "ConfiguredHostServiceModules";

    /// <summary>
    /// Sets a property value in the builder's property dictionary.
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="builder">The game host builder.</param>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value to set.</param>
    /// <returns>The current builder instance for fluent configuration.</returns>
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

    /// <summary>
    /// Gets a property value from the builder's property dictionary.
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="builder">The game host builder.</param>
    /// <param name="key">The property key.</param>
    /// <returns>The property value, or null if not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the property exists but is not of type TValue.</exception>
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
    /// <summary>
    /// Applies a service module immediately to the bootstrap service collection.
    /// Each module type is applied at most once.
    /// </summary>
    /// <remarks>
    /// Use this method to apply modules that must be configured before the game or other components.
    /// Repeated calls with the same module type are safely ignored.
    /// </remarks>
    /// <typeparam name="TModule">The service module type to apply.</typeparam>
    /// <param name="builder">The game host builder.</param>
    /// <returns>The current builder instance for fluent configuration.</returns>
    public static IGameHostBuilder ConfigureHostServices<TModule>(this IGameHostBuilder builder)
        where TModule : class, IServiceModule, new()
        => builder.ConfigureHostServices(typeof(TModule));

    /// <summary>
    /// Applies a service module immediately to the bootstrap service collection.
    /// </summary>
    /// <remarks>
    /// This internal method handles duplicate prevention using a HashSet of already-configured types.
    /// If initialization fails, the module type is removed from the configured set to allow retrying.
    /// </remarks>
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

    /// <summary>
    /// Registers a service module instance with the builder.
    /// </summary>
    /// <param name="builder">The game host builder.</param>
    /// <param name="module">The service module instance to register.</param>
    /// <returns>The current builder instance for fluent configuration.</returns>
    public static IGameHostBuilder AddServiceModule(this IGameHostBuilder builder, IServiceModule module)
    {
        builder.Services.AddSingleton(module);
        return builder;
    }

    /// <summary>
    /// Registers a service module type with the builder using transient lifetime.
    /// </summary>
    /// <typeparam name="T">The service module type implementing IServiceModule.</typeparam>
    /// <param name="builder">The game host builder.</param>
    /// <returns>The current builder instance for fluent configuration.</returns>
    public static IGameHostBuilder AddServiceModule<T>(this IGameHostBuilder builder) where T : class, IServiceModule
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IServiceModule, T>());
        return builder;
    }

    /// <summary>
    /// Registers a service module using a delegate for dynamic configuration.
    /// </summary>
    /// <param name="builder">The game host builder.</param>
    /// <param name="configure">A delegate that configures services using the service collection.</param>
    /// <returns>The current builder instance for fluent configuration.</returns>
    public static IGameHostBuilder AddServiceModule(this IGameHostBuilder builder, Action<IServiceCollection, IServiceProvider> configure)
    {
        builder.Services.AddTransient<IServiceModule>(sp => new LambdaServices(sp, configure));
        return builder;
    }

    /// <summary>
    /// Registers a service module type with a factory delegate.
    /// </summary>
    /// <typeparam name="T">The service module type implementing IServiceModule.</typeparam>
    /// <param name="builder">The game host builder.</param>
    /// <param name="factory">A factory delegate to create the service module instance.</param>
    /// <returns>The current builder instance for fluent configuration.</returns>
    public static IGameHostBuilder AddServiceModule<T>(this IGameHostBuilder builder, Func<IServiceProvider, T> factory) where T : class, IServiceModule
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IServiceModule, T>(factory));
        return builder;
    }

    /// <summary>
    /// Applies default configuration for logging and configuration files.
    /// </summary>
    /// <remarks>
    /// This includes loading appsettings.json and environment-specific configuration,
    /// as well as setting up default logging filters.
    /// </remarks>
    /// <param name="builder">The game host builder.</param>
    /// <returns>The current builder instance for fluent configuration.</returns>
    public static IGameHostBuilder UseDefaultConfiguration(this IGameHostBuilder builder)
    {
        ConfigureDefault(builder.Configuration, builder.Logging, builder.Environment);
        return builder;
    }

    /// <summary>
    /// Configures default settings for configuration and logging.
    /// </summary>
    /// <remarks>
    /// Internal helper method that sets up JSON configuration files and logging defaults.
    /// </remarks>
    internal static void ConfigureApplicationDefaults(ApplicationHostBuilder builder)
    {
        ConfigureDefault(builder.Configuration, builder.Logging, builder.Environment);
    }

    /// <summary>
    /// Applies default configuration and logging settings to the builder.
    /// </summary>
    /// <remarks>
    /// Sets up JSON configuration files (appsettings.json and environment-specific files),
    /// default logging levels, and development-mode debug logging.
    /// </remarks>
    private static void ConfigureDefault(IConfigurationManager configuration, ILoggingBuilder logging, IHostEnvironment environment)
    {
        // Load configuration files in order: base, then environment-specific
        configuration
               .SetFileProvider(new VirtualFileProvider(environment))
               .AddJsonFile("appsettings.json", optional: true)
               .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true);

        // Configure default logging
        logging
               .SetMinimumLevel(LogLevel.Information)
               .AddDefaultConfiguration(environment)
               .AddConfiguration(configuration.GetSection("Logging"));
    }

    /// <summary>
    /// Applies default logging filters and development-specific logging configuration.
    /// </summary>
    /// <remarks>
    /// Suppresses verbose logging from Microsoft and System namespaces, enables ECS debug logging,
    /// and enables debug-level logging in development environments.
    /// </remarks>
    private static ILoggingBuilder AddDefaultConfiguration(this ILoggingBuilder builder, IHostEnvironment env)
    {
        // Apply common filters to reduce noise from framework libraries
        builder.AddFilter("Microsoft", LogLevel.Warning)
               .AddFilter("System", LogLevel.Warning)
               .AddFilter("PurrplingCore.Ecs", LogLevel.Debug);

        // Enable debug logging only in development
        if (env.IsDevelopment())
        {
            builder.SetMinimumLevel(LogLevel.Debug)
                   .AddDebug();
        }

        return builder;
    }
}

/// <summary>
/// Extension methods for service collection operations.
/// </summary>
public static partial class ServicesExtensions
{
    /// <summary>
    /// Registers a service module instance with the service collection.
    /// </summary>
    /// <typeparam name="T">The service module type implementing IServiceModule.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="instance">The service module instance to register.</param>
    /// <returns>The service collection for fluent configuration.</returns>
    public static IServiceCollection AddServiceModule<T>(this IServiceCollection services, T instance) where T : class, IServiceModule
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IServiceModule>(instance));
        return services;
    }

    /// <summary>
    /// Registers a service module type with transient lifetime.
    /// </summary>
    /// <typeparam name="T">The service module type implementing IServiceModule.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for fluent configuration.</returns>
    public static IServiceCollection AddServiceModule<T>(this IServiceCollection services) where T : class, IServiceModule
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IServiceModule, T>());
        return services;
    }

    /// <summary>
    /// Registers a service module type with a factory delegate.
    /// </summary>
    /// <typeparam name="T">The service module type implementing IServiceModule.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">A factory delegate to create the service module instance.</param>
    /// <returns>The service collection for fluent configuration.</returns>
    public static IServiceCollection AddServiceModule<T>(this IServiceCollection services, Func<IServiceProvider, T> factory) where T : class, IServiceModule
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IServiceModule, T>(factory));
        return services;
    }

    /// <summary>
    /// Registers a service module using a factory delegate.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">A factory delegate to create the service module instance.</param>
    /// <returns>The service collection for fluent configuration.</returns>
    public static IServiceCollection AddServiceModule(this IServiceCollection services, Func<IServiceProvider, IServiceModule> factory)
    {
        services.AddTransient(factory);
        return services;
    }
}
