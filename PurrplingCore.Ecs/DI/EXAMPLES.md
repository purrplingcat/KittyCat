# World feature examples

Read [README.md](README.md) first for the concepts and precedence rules. The custom feature in the second half is exercised by `PurrplingCore.Toolkit.Tests/Ecs/WorldFeatureExampleTests.cs`.

## Use an existing feature

Particle simulation without any renderer:

```csharp
services.AddWorld<ArenaWorld>().AddParticleModule();
```

Simulation plus drawing. The world must already have a `RenderContext` whose renderer declares the particle layer:

```csharp
services.AddWorld()
    .AddRenderer<WorldRendererConfiguration>()
    .AddFeature<ParticleRenderingFeature>();
```

## Configure a feature

For one world marker only:

```csharp
services.AddWorld<ArenaWorld>()
    .AddFeature<ParticlesFeature>()
    .ConfigureOptions<ParticleOptions>(options => options.TimeScale = 0.5f);
```

For every world, with a marker override:

```csharp
services.ConfigureAll<ParticleOptions>(options => options.TimeScale = 0.8f);

services.AddWorld<MenuWorld>().AddFeature<ParticlesFeature>();   // 0.8
services.AddWorld<ArenaWorld>()
    .AddFeature<ParticlesFeature>()
    .ConfigureOptions<ParticleOptions>(options => options.TimeScale = 0f);   // paused
```

Pick another particle layer for one world:

```csharp
services.AddWorld<ArenaWorld>()
    .AddRenderer<WorldRendererConfiguration>()
    .AddFeature<ParticleRenderingFeature>()
    .ConfigureOptions<ParticleRenderingOptions>(options => options.Layer = MyLayers.Sparks);
```

Registration order does not matter between `ConfigureAll` and `ConfigureOptions`. The plain `services.Configure<ParticleOptions>(...)` is not applied to worlds.

## Write your own feature

This feature heals every entity with a `Health` component. All types are public here so the test project can reuse them.

```csharp
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Ecs.WorldBuilding;

public struct Health : IComponent
{
    public float Value;
}

// 1. Options with defaults. The feature works with no configuration.
public sealed class RegenerationOptions
{
    public float PerSecond { get; set; } = 1f;
}

// 2. The system reads its options once, through constructor injection.
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

// 3. The module puts the system into the world blueprint.
public sealed class RegenerationModule : IWorldModule
{
    public int Order => 0;

    public void Setup(IWorldBlueprint world) => world.AddSystem<RegenerationSystem>();
}

// 4. The feature is the unit a game opts into.
public sealed class RegenerationFeature : IWorldFeature
{
    public void ConfigureServices(IWorldServicesBuilder builder) => builder.AddModule<RegenerationModule>();
}

// 5. An extension makes it discoverable.
public static class RegenerationExtensions
{
    public static IWorldServicesBuilder AddRegeneration(this IWorldServicesBuilder builder) =>
        builder.AddFeature<RegenerationFeature>();
}
```

Using it, with and without configuration:

```csharp
services.AddWorld<NormalWorld>().AddRegeneration();
services.AddWorld<FastWorld>()
    .AddRegeneration()
    .ConfigureOptions<RegenerationOptions>(options => options.PerSecond = 4f);
```

### Validate options

Register a validator in the feature when invalid settings must stop world composition, as `ParticlesFeature` does:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

internal sealed class RegenerationOptionsValidator : IValidateOptions<RegenerationOptions>
{
    public ValidateOptionsResult Validate(string? name, RegenerationOptions options) =>
        float.IsFinite(options.PerSecond) && options.PerSecond >= 0f
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("PerSecond must be finite and nonnegative.");
}

public sealed class RegenerationFeature : IWorldFeature
{
    public void ConfigureServices(IWorldServicesBuilder builder)
    {
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<RegenerationOptions>, RegenerationOptionsValidator>());
        builder.AddModule<RegenerationModule>();
    }
}
```

### Depend on another feature, add a service and a lifecycle hook

A feature composes others through `AddFeature`; repeated requests are ignored. A module is created in the world scope, so it can inject the world's services, and it registers lifecycle callbacks in `Setup`:

```csharp
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
        .AddFeature<RegenerationFeature>()   // dependency
        .AddService<SpawnCounter>()          // one counter per world instance
        .AddModule<SpawnerModule>();
}
```

## World-owned services

```csharp
services.AddWorld<ArenaWorld>().AddService<ScoreBoard>();         // each world instance gets its own
services.AddWorld<MenuWorld>().AddService<IScoreBoard, MenuScoreBoard>();
```

An application-level registration of the same type overrides both:

```csharp
services.AddSingleton<IScoreBoard, GlobalScoreBoard>();
services.AddWorld<MenuWorld>().AddService<IScoreBoard, MenuScoreBoard>();   // GlobalScoreBoard is used
```

## Reuse a world's modules

```csharp
services.AddWorld<ArenaWorld>().AddRegeneration();
services.AddWorld<TrainingWorld>()
    .DeriveWorld<ArenaWorld>()   // inherits the modules, and with them their systems
    .ConfigureOptions<RegenerationOptions>(options => options.PerSecond = 2f);   // options belong to TrainingWorld
```

Services and options are resolved for the derived marker. Register any service an inherited module injects on `TrainingWorld` too.

## Test a feature

```csharp
var services = new ServiceCollection();
services.AddLogging();
services.AddWorld<TestWorld>().AddRegeneration();
using var provider = services.BuildServiceProvider(
    new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

var signature = WorldSignature.For<TestWorld>();
using var world = (ManagedWorld)provider.GetRequiredKeyedService<IWorldFactory>(signature.MarkerType)
    .CreateWorld("Test", signature);
world.Store.CreateEntity(new Health());
world.Update(new UpdateTick(0.5f, 0.5f));
```

`ValidateScopes` and `ValidateOnBuild` catch lifetime mistakes. Headless worlds need no graphics services unless a feature asks for a `RenderContext`.
