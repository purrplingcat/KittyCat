using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.Messaging;

namespace PurrplingCore.Toolkit.DI;

public sealed class CoreServices : IServiceConfiguration
{
    public void Configure(IServiceCollection services)
    {
        services.AddOptions<ContentManagerOptions>();
        services.TryAddSingleton<IContentManagerProvider, DefaultContentManagerProvider>();
        services.TryAddSingleton<IMessageBus, DefaultMessageBus>();
    }
}
