using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PurrplingCore.ContentBuilder.Xml;
using PurrplingCore.Toolkit.Hosting;

namespace PurrplingCore.ContentBuilder;

/// <summary>
/// Composes content compilation, catalog generation, and PAK or loose publishing through the host.
/// </summary>
public static class ContentBuilderHostExtensions
{
    /// <summary>
    /// Adds the content application configured through the host's ContentBuilder section.
    /// </summary>
    public static IApplicationHostBuilder UseContentBuilder(this IApplicationHostBuilder builder)
    {
        builder.Services.AddServiceModule<ContentBuilderModule>();
        return builder;
    }

    public static IApplicationHostBuilder AddBundle<TBundle>(this IApplicationHostBuilder builder) where TBundle : class, IAssetBundle
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAssetBundle, TBundle>());
        return builder;
    }
}

public enum AssetBundleType
{
    Loose,
    Pak
}

internal sealed class ContentBuilderModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions<ContentBuilderOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            configuration.GetSection("ContentBuilder").Bind(options);
            // A relative working directory is resolved against the tool binaries, not the shell.
            options.WorkingDirectory = Path.GetFullPath(options.WorkingDirectory, AppContext.BaseDirectory);
        });
        services.AddSingleton<ContentBuilder>();
        services.AddSingleton<IHostedApplication, AssetBuildJob>();
    }
}

internal sealed class AssetBuildJob(ContentBuilder contentBuilder, IOptions<ContentBuilderOptions> options) : IHostedApplication
{
    public int Run(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        contentBuilder.Run(options.Value);
        return contentBuilder.FailedToBuild > 0 ? -1 : 0;
    }
}