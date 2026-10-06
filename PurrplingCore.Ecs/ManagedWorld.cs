using Friflo.Engine.ECS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Ecs;

/// <summary>
/// A world that owns a dependency-injection scope for its services and systems.
/// </summary>
public class ManagedWorld : World
{
    private readonly IServiceScope _scope;
    private bool _disposed;

    /// <summary>
    /// Gets the service provider scoped to this world.
    /// </summary>
    public IServiceProvider Services => _scope.ServiceProvider;

    /// <summary>
    /// Initializes a managed world with its owning service scope.
    /// </summary>
    protected ManagedWorld(IServiceScope scope, ILogger? logger, string? name = null, WorldSignature? signature = null)
        : base(PidType.RandomPids, signature ?? WorldSignature.Default, logger)
    {
        ArgumentNullException.ThrowIfNull(scope);

        _scope = scope;
        Name = name ?? string.Empty;
    }

    /// <summary>
    /// Disposes the world and releases its service scope when requested.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        finally
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _scope.Dispose();
            }
        }
    }

    /// <summary>
    /// Creates a world with a new dependency-injection scope.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ManagedWorld Create(IServiceScopeFactory scopeFactory, string? name = null, WorldSignature? signature = null)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);

        var scope = scopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetService<ILogger<ManagedWorld>>()
                     ?? NullLogger<ManagedWorld>.Instance;
        var world = new ManagedWorld(scope, logger, name, signature);

        var context = scope.ServiceProvider.GetService<IWorldContext>();
        if (context is WorldContext worldContext)
        {
            worldContext.World = world;
        }

        return world;
    }
}
