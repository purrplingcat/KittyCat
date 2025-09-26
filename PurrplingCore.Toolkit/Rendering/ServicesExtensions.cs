using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PurrplingCore.Toolkit.DI;

namespace PurrplingCore.Toolkit.Rendering;

public static class ServicesExtensions
{
    public static IServiceCollection AddRenderPipeline<TKey>(this IServiceCollection services, Action<IRenderPipelineBuilder> configure)
    {
        var key = typeof(TKey);

        services.TryAddKeyedSingleton<IRenderPipelineFactory>(key, (provider, _) => new RenderPipelineFactory(
            provider, provider.GetRequiredKeyedService<ISetup<IRenderPipelineBuilder>>(key)
        ));
        services.TryAddKeyedScoped(key, (provider, _) => provider.GetRequiredKeyedService<IRenderPipelineFactory>(key).Create());
        services.AddKeyedSetup(configure, key);

        return services;
    }

    public static IServiceCollection AddRenderPipeline(this IServiceCollection services, Action<IRenderPipelineBuilder> configure)
    {
        services.TryAddSingleton<IRenderPipelineFactory>(provider => new RenderPipelineFactory(
            provider, provider.GetRequiredService<ISetup<IRenderPipelineBuilder>>()
        ));
        services.TryAddScoped((provider) => provider.GetRequiredService<IRenderPipelineFactory>().Create());
        services.AddSetup(configure);

        return services;
    }
}
