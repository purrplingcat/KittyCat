using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PurrplingCore.Ecs;

public interface IWorldFactory
{
    ManagedWorld CreateWorld(string? name = null, WorldType? tag = null);
}

internal class WorldFactory(IServiceScopeFactory scopeFactory, ILogger<WorldFactory> logger) : IWorldFactory
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<WorldFactory> _logger = logger;

    public ManagedWorld CreateWorld(string? name = null, WorldType? type = null)
    {
        var scope = _scopeFactory.CreateScope();
        name ??= string.Empty;
        type ??= WorldType.Default;

        _logger.LogInformation("Creating {Tag} world named '{Name}'", type.Name, name);
        return ManagedWorld.Create(scope, name, type);
    }
}
