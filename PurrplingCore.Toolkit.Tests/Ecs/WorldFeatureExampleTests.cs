using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Ecs.WorldBuilding;
using Xunit;

namespace PurrplingCore.Toolkit.Tests.Ecs;

// The feature types below are the examples documented in PurrplingCore.Ecs/DI/EXAMPLES.md.
public class WorldFeatureExampleTests
{
    [Fact]
    public void CustomFeatureUsesDefaultsAndPerWorldConfiguration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorld<NormalWorld>().AddRegeneration();
        services.AddWorld<FastWorld>()
            .AddRegeneration()
            .ConfigureOptions<RegenerationOptions>(options => options.PerSecond = 4f);
        using var provider = BuildProvider(services);
        using var normal = CreateWorld<NormalWorld>(provider);
        using var fast = CreateWorld<FastWorld>(provider);
        normal.Store.CreateEntity(new Health());
        fast.Store.CreateEntity(new Health());

        normal.Update(new UpdateTick(0.5f, 0.5f));
        fast.Update(new UpdateTick(0.5f, 0.5f));

        Assert.Equal(0.5f, ReadHealth(normal), 5);
        Assert.Equal(2f, ReadHealth(fast), 5);
    }

    [Fact]
    public void FeatureCanComposeDependenciesServicesAndLifecycleHooks()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorld<SpawnerWorld>().AddFeature<SpawnerFeature>();
        using var provider = BuildProvider(services);
        using var world = CreateWorld<SpawnerWorld>(provider);

        world.Initialize();

        Assert.Equal(1, world.Services.GetRequiredService<SpawnCounter>().Spawned);
        Assert.Equal(1, CountHealth(world));
        Assert.Single(world.Services.GetKeyedServices<IWorldModule>(typeof(SpawnerWorld))
            .OfType<RegenerationModule>());
    }

    [Fact]
    public void DerivedWorldInheritsModulesButUsesItsOwnOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorld<NormalWorld>().AddRegeneration();
        services.AddWorld<TrainingWorld>()
            .DeriveWorld<NormalWorld>()
            .ConfigureOptions<RegenerationOptions>(options => options.PerSecond = 2f);
        using var provider = BuildProvider(services);
        using var training = CreateWorld<TrainingWorld>(provider);
        training.Store.CreateEntity(new Health());

        training.Update(new UpdateTick(0.5f, 0.5f));

        Assert.Equal(1f, ReadHealth(training), 5);
    }

    private static ServiceProvider BuildProvider(IServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

    private static ManagedWorld CreateWorld<TMarker>(IServiceProvider provider) where TMarker : IWorldMarker
    {
        var signature = WorldSignature.For<TMarker>();
        return (ManagedWorld)provider.GetRequiredKeyedService<IWorldFactory>(signature.MarkerType)
            .CreateWorld(typeof(TMarker).Name, signature);
    }

    private static float ReadHealth(World world)
    {
        foreach (var (health, _) in world.Store.Query<Health>().Chunks)
        {
            if (health.Length > 0)
                return health[0].Value;
        }

        throw new InvalidOperationException("Expected a Health entity.");
    }

    private static int CountHealth(World world)
    {
        var count = 0;
        foreach (var (health, _) in world.Store.Query<Health>().Chunks)
            count += health.Length;
        return count;
    }

    public sealed class NormalWorld : IWorldMarker;
    public sealed class FastWorld : IWorldMarker;
    public sealed class SpawnerWorld : IWorldMarker;
    public sealed class TrainingWorld : IWorldMarker;
}

public struct Health : IComponent
{
    public float Value;
}

public sealed class RegenerationOptions
{
    public float PerSecond { get; set; } = 1f;
}

[System<UpdateSystemGroup>]
public sealed class RegenerationSystem : QuerySystem<Health>
{
    private readonly float _perSecond;

    public RegenerationSystem(IWorldOptions<RegenerationOptions> options)
    {
        _perSecond = options.Value.PerSecond;
    }

    protected override void OnUpdate()
    {
        var amount = _perSecond * Tick.deltaTime;
        foreach (var (health, _) in Query.Chunks)
        {
            for (var index = 0; index < health.Length; index++)
                health[index].Value += amount;
        }
    }
}

public sealed class RegenerationModule : IWorldModule
{
    public int Order => 0;

    public void Setup(IWorldBlueprint world) => world.AddSystem<RegenerationSystem>();
}

public sealed class RegenerationFeature : IWorldFeature
{
    public void ConfigureServices(IWorldServicesBuilder builder) => builder.AddModule<RegenerationModule>();
}

public static class RegenerationExtensions
{
    public static IWorldServicesBuilder AddRegeneration(this IWorldServicesBuilder builder) =>
        builder.AddFeature<RegenerationFeature>();
}

public sealed class SpawnCounter
{
    public int Spawned { get; set; }
}

public sealed class SpawnerModule(SpawnCounter counter) : IWorldModule
{
    public int Order => 0;

    public void Setup(IWorldBlueprint world) => world.OnInitialized(context =>
    {
        context.World.Store.CreateEntity(new Health());
        counter.Spawned++;
    });
}

public sealed class SpawnerFeature : IWorldFeature
{
    public void ConfigureServices(IWorldServicesBuilder builder) => builder
        .AddFeature<RegenerationFeature>()
        .AddService<SpawnCounter>()
        .AddModule<SpawnerModule>();
}
