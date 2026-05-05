using Friflo.Engine.ECS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.CompilerServices;
using static System.Formats.Asn1.AsnWriter;

namespace PurrplingCore.Ecs;

public class ManagedWorld : World
{
    private readonly IServiceScope _scope;
    private bool _disposed;

    public IServiceProvider Services => _scope.ServiceProvider;

    protected ManagedWorld(IServiceScope scope, ILogger? logger, string? name = null, WorldSignature? type = null) 
        : base(PidType.RandomPids, type ?? WorldSignature.Default, logger)
    {
        ArgumentNullException.ThrowIfNull(scope);

        _scope = scope;
        Name = name ?? string.Empty;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ManagedWorld Create(IServiceScopeFactory scopeFactory, string? name = null, WorldSignature? tag = null)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);

        var scope = scopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetService<ILogger<ManagedWorld>>()
                     ?? NullLogger<ManagedWorld>.Instance;
        var world = new ManagedWorld(scope, logger, name, tag);

        var context = scope.ServiceProvider.GetService<IWorldContext>();
        if (context is WorldContext worldContext)
        {
            worldContext.World = world;
        }

        return world;
    }
}
