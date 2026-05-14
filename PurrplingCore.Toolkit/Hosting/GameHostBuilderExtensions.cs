using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.DI.Configuration;

namespace PurrplingCore.Toolkit.Hosting;

public static class GameHostBuilderExtensions
{
    public static IGameHostBuilder AddServiceConfiguration(this IGameHostBuilder builder, IServiceConfiguration configuration)
    {
        builder.Services.AddSingleton(configuration);
        return builder;
    }

    public static IGameHostBuilder AddServiceConfiguration<T>(this IGameHostBuilder builder) where T : class, IServiceConfiguration
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IServiceConfiguration, T>());
        return builder;
    }

    public static IGameHostBuilder AddServiceConfiguration(this IGameHostBuilder builder, Action<IServiceCollection, IServiceProvider> configure)
    {
        builder.Services.AddTransient<IServiceConfiguration>(sp => new LambdaServices(sp, configure));
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
               .AddFilter("Microsoft", LogLevel.Warning)
               .AddFilter("System", LogLevel.Warning)
               .AddFilter("PurrplingCore.Ecs", LogLevel.Debug);

        // Only for development purposes
        if (builder.Environment.IsDevelopment())
        {
            builder.Logging
                   .SetMinimumLevel(LogLevel.Debug)
                   .AddDebug();
        }

        // Apply user loging configuration
        builder.Logging
               .AddConfiguration(builder.Configuration.GetSection("Logging"));

        return builder;
    }
}
