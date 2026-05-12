
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PurrplingCore.Toolkit.DI;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Content;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVfs(this IServiceCollection services)
    {
        services.TryAddAlias<IFileSystem, IAggregateFileSystem>();
        services.TryAddSingleton<IAggregateFileSystem, FileSystemManager>();

        return services;
    }

    public static IServiceCollection AddPhysicalVfs(this IServiceCollection services, string path, int order = 0)
    {
        return services.AddVfsLayer(order, sp =>
        {
            var rootFs = new PhysicalFileSystem();
            return new SubFileSystem(rootFs, rootFs.ConvertPathFromInternal(path));
        });
    }

    public static IServiceCollection AddVfsLayer(this IServiceCollection services, int order, Func<IServiceProvider, IFileSystem> factory)
    {
        services.AddVfs();
        return services.AddSingleton(sp => new FileSystemLayer(factory(sp), order));
    }
}
