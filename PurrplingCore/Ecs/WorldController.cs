using Friflo.Engine.ECS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit.Rendering;

namespace PurrplingCore.Ecs;

/// <summary>
/// Configures the ECS world owned by a MonoGame game component.
/// </summary>
public sealed class WorldControllerOptions
{
    public string Name { get; set; } = "MainWorld";
    public WorldSignature Signature { get; set; } = WorldSignature.Default;
}

/// <summary>
/// Bridges the MonoGame update and draw loop to an ECS world.
/// </summary>
public sealed class WorldController : DrawableGameComponent
{
    private readonly World _world;
    private bool _disposed;

    public WorldController(Game game, IWorldFactory worldFactory, IOptions<WorldControllerOptions> options)
        : base(game)
    {
        ArgumentNullException.ThrowIfNull(worldFactory);
        var worldOptions = options.Value;
        _world = worldFactory.CreateWorld(worldOptions.Name, worldOptions.Signature);
    }

    public override void Initialize()
    {
        _world.Initialize();
        base.Initialize();
    }

    public override void Update(GameTime gameTime)
    {
        _world.Update(gameTime.ToUpdateTick());
        UpdateEntityScripts(_world.Store);
    }

    private static void UpdateEntityScripts(EntityStore store)
    {
        var scripts = store.EntityScripts;
        for (var index = 0; index < scripts.Length; index++)
        {
            foreach (var script in scripts[index])
            {
                script.Update();
            }
        }
    }

    public override void Draw(GameTime gameTime) => _world.Draw(gameTime.ToUpdateTick());

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
            _world.Dispose();
            base.Dispose(disposing);
        }
    }
}

/// <summary>
/// Opt-in registration for running an ECS world as a MonoGame component.
/// </summary>
public static class WorldControllerServiceCollectionExtensions
{
    public static IServiceCollection AddWorldController(this IServiceCollection services)
    {
        services.AddOptions<WorldControllerOptions>();
        services.TryAddSingleton<WorldController>();
        services.AddSingleton<IGameComponent>(provider => provider.GetRequiredService<WorldController>());
        return services;
    }
}
