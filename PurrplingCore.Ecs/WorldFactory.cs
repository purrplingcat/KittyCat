using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PurrplingCore.Ecs;

public interface IWorldFactory
{
    ManagedWorld CreateWorld(string? name = null, WorldTag? tag = null);
}

internal class WorldFactory(IServiceScopeFactory scopeFactory, ILogger<WorldFactory> logger) : IWorldFactory
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<WorldFactory> _logger = logger;

    public ManagedWorld CreateWorld(string? name = null, WorldTag? tag = null)
    {
        var scope = _scopeFactory.CreateScope();
        name ??= string.Empty;
        tag ??= WorldTag.Default;

        _logger.LogInformation("Creating {Tag} world named '{Name}'", tag.DebugName, name);
        return ManagedWorld.Create(scope, name, tag);
    }
}
