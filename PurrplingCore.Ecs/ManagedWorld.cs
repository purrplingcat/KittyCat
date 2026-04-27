using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PurrplingCore.Toolkit.Extensions;

namespace PurrplingCore.Ecs;

public class ManagedWorld : World
{
    private readonly IServiceScope _scope;
    private bool _disposed;
    internal bool creating;

    public WorldTag Tag { get; }

    public IServiceProvider Services => _scope.ServiceProvider;

    public ManagedWorld(IServiceScope scope, ILogger? logger, string? name = null, WorldTag? tag = null) 
        : base(PidType.RandomPids, logger)
    {
        ArgumentNullException.ThrowIfNull(scope);

        _scope = scope;
        Name = name ?? string.Empty;
        Tag = tag ?? WorldTag.Default;
    }

    public T CreateSystem<T>() where T : BaseSystem
    {
        EnsureNotDisposed();
        return ActivatorUtilities.GetServiceOrCreateInstance<T>(Services);
    }

    public BaseSystem CreateSystem(Type type)
    {
        EnsureNotDisposed();
        return (BaseSystem)ActivatorUtilities.GetServiceOrCreateInstance(Services, type);
    }

    public SystemGroup CreateSystemGroup(string name, params BaseSystem[] systems)
    {
        EnsureNotDisposed();
        return new SystemGroup(name)
        {
            systems
        };
    }

    public SystemGroup CreateSystemGroup(string name, params Type[] systems)
    {
        EnsureNotDisposed();
        return new SystemGroup(name)
        {
            systems.Select(s => (BaseSystem)ActivatorUtilities.GetServiceOrCreateInstance(Services, s))
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _scope.Dispose();
            }

            base.Dispose(disposing);
            _disposed = true;
        }
    }

    public static ManagedWorld Create(IServiceScope scope, string? name = null, WorldTag? tag = null) 
    { 
        var logger = scope.ServiceProvider.GetService<ILogger<ManagedWorld>>() 
                     ?? NullLogger<ManagedWorld>.Instance;
        
        return Create(scope, logger, name, tag);
    }

    public static ManagedWorld Create(IServiceScope scope, ILogger logger, string? name = null, WorldTag? tag = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var context = scope.ServiceProvider.GetService<WorldContext>();
        var world = new ManagedWorld(scope, logger, name, tag);

        if (context != null)
        {
            context.World = world;
        }

        return world;
    }

    public static ManagedWorld Create(IServiceProvider services, string? name = null, WorldTag? tag = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var scope = services.CreateScope();
        return Create(scope, name, tag);
    }
}
