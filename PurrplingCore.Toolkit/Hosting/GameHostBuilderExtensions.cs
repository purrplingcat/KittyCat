using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Hosting.Services;

namespace PurrplingCore.Toolkit.Hosting;

public static class GameHostBuilderExtensions
{
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
        // Configuration
        builder.Configuration
               .SetFileProvider(new VirtualFileProvider(builder.Environment))
               .AddJsonFile("appsettings.json", optional: true)
               .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
               

        // Default logging config
        builder.Logging
               .SetMinimumLevel(LogLevel.Information)
               .AddDefaultConfiguration(builder.Environment)
               .AddConfiguration(builder.Configuration.GetSection("Logging"));

        return builder;
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
