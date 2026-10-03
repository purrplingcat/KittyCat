using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Toolkit.Rendering;

namespace PurrplingCore.Rendering;

/// <summary>
/// Resolves renderer-owned contexts by the same keys used to register their renderers.
/// </summary>
public sealed class RenderContextManager(IServiceProvider services)
{
    /// <summary>
    /// Returns the registered renderer's shared context. Resolve once when composing a draw system.
    /// </summary>
    public RenderContext Get(object rendererKey)
        => services.GetRequiredKeyedService<RenderContext>(rendererKey);
}