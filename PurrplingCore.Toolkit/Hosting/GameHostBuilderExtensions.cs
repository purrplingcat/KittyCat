using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.DI.Configuration;

namespace PurrplingCore.Toolkit.Hosting;

public static class GameHostBuilderExtensions
{
    public static IGameHostBuilder AddServiceConfiguration(this IGameHostBuilder builder, IServicesConfiguration configuration)
    {
        builder.Services.AddSingleton(configuration);
        return builder;
    }

    public static IGameHostBuilder AddServiceConfiguration<T>(this IGameHostBuilder builder) where T : class, IServicesConfiguration
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IServicesConfiguration, T>());
        return builder;
    }

    public static IGameHostBuilder AddServiceConfiguration(this IGameHostBuilder builder, Action<IServiceCollection, IServiceProvider> configure)
    {
        builder.Services.AddTransient<IServicesConfiguration>(sp => new LambdaServices(sp, configure));
        return builder;
    }

    public static IGameHostBuilder UseDefaultConfiguration(this IGameHostBuilder builder)
    {
        // Configuration
        builder.Configuration
               .SetFileProvider(new TitleContainerFileProvider())
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
