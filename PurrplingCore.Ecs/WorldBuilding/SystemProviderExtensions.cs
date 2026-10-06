using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Provides typed access to systems created by a system provider.
/// </summary>
public static class SystemProviderExtensions
{
    /// <summary>
    /// Gets or creates a system instance of the specified type.
    /// </summary>
    public static TSystem Provide<TSystem>(this SystemProvider provider) where TSystem : BaseSystem
    {
        return (TSystem)provider.Provide(typeof(TSystem));
    }
}