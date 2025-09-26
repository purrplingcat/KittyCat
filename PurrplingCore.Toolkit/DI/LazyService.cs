using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Toolkit.DI;

internal sealed class LazyService<TService>(IServiceProvider provider) : Lazy<TService>(provider.GetRequiredService<TService>)
    where TService : class
{
    public override string ToString()
    {
        return $"LazyService<{typeof(TService).Name}>";
    }
}
