using Friflo.Engine.ECS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Particles;
using PurrplingCore.Ecs.WorldBuilding;
using PurrplingCore.Toolkit.Particles;
using Xunit;

namespace PurrplingCore.Toolkit.Tests.Ecs;

public class ParticleFeatureTests
{
    [Fact]
    public void ParticleFeatureScalesSimulationPerWorldMarkerAndDeduplicatesLegacyOptIn()
    {
        var services = CreateServices();
        services.AddWorld<HalfSpeedWorld>()
            .AddParticleModule()
            .AddFeature<ParticlesFeature>()
            .ConfigureOptions<ParticleOptions>(options => options.TimeScale = 0.5f);
        services.AddWorld<DoubleSpeedWorld>()
            .AddFeature<ParticlesFeature>()
            .ConfigureOptions<ParticleOptions>(options => options.TimeScale = 2f);
        services.AddWorld<DefaultSpeedWorld>().AddFeature<ParticlesFeature>();
        using var provider = BuildProvider(services);
        using var halfSpeed = CreateWorld<HalfSpeedWorld>(provider);
        using var doubleSpeed = CreateWorld<DoubleSpeedWorld>(provider);
        using var defaultSpeed = CreateWorld<DefaultSpeedWorld>(provider);
        AddMovingParticle(halfSpeed, lifetime: 10f);
        AddMovingParticle(doubleSpeed, lifetime: 10f);
        AddMovingParticle(defaultSpeed, lifetime: 10f);

        halfSpeed.Update(new UpdateTick(0.2f, 0.2f));
        doubleSpeed.Update(new UpdateTick(0.2f, 0.2f));
        defaultSpeed.Update(new UpdateTick(0.2f, 0.2f));

        var halfState = ReadSingleParticle(halfSpeed);
        var doubleState = ReadSingleParticle(doubleSpeed);
        Assert.Equal(0.1f, halfState.Age, 5);
        Assert.Equal(1f, halfState.Position.X, 5);
        Assert.Equal(0.4f, doubleState.Age, 5);
        Assert.Equal(4f, doubleState.Position.X, 5);
        var defaultState = ReadSingleParticle(defaultSpeed);
        Assert.Equal(0.2f, defaultState.Age, 5);
        Assert.Equal(2f, defaultState.Position.X, 5);
    }

    [Fact]
    public void ZeroTimeScalePausesMotionAndExpirationDeletion()
    {
        var services = CreateServices();
        services.AddWorld<PausedWorld>()
            .AddParticleModule()
            .ConfigureOptions<ParticleOptions>(options => options.TimeScale = 0f);
        using var provider = BuildProvider(services);
        using var world = CreateWorld<PausedWorld>(provider);
        var state = new ParticleState(new ParticleEmission
        {
            Position = new Vector2(3f, 4f),
            Velocity = new Vector2(10f, 0f),
            Lifetime = 1f,
        }) { Age = 1f };
        world.Store.CreateEntity(new Particle { State = state });

        world.Update(new UpdateTick(0.5f, 0.5f));

        Assert.Equal(1, CountParticles(world));
        var pausedState = ReadSingleParticle(world);
        Assert.Equal(1f, pausedState.Age);
        Assert.Equal(new Vector2(3f, 4f), pausedState.Position);
    }

    [Fact]
    public void ExpiredParticlesAreDeletedDuringScaledSimulation()
    {
        var services = CreateServices();
        services.AddWorld<ExpiredWorld>().AddFeature<ParticlesFeature>();
        using var provider = BuildProvider(services);
        using var world = CreateWorld<ExpiredWorld>(provider);
        AddMovingParticle(world, lifetime: 0.1f);

        world.Update(new UpdateTick(0.2f, 0.2f));

        Assert.Equal(0, CountParticles(world));
    }

    [Fact]
    public void ParticleOptionsRejectNegativeAndNonFiniteTimeScale()
    {
        var services = CreateServices();
        services.AddWorld<NegativeScaleWorld>()
            .AddParticleModule()
            .ConfigureOptions<ParticleOptions>(options => options.TimeScale = -1f);
        services.AddWorld<InvalidScaleWorld>()
            .AddParticleModule()
            .ConfigureOptions<ParticleOptions>(options => options.TimeScale = float.NaN);
        using var provider = BuildProvider(services);

        Assert.Throws<OptionsValidationException>(() => CreateWorld<NegativeScaleWorld>(provider));
        Assert.Throws<OptionsValidationException>(() => CreateWorld<InvalidScaleWorld>(provider));
    }

    [Fact]
    public void ScaledDeltaMustRemainFinite()
    {
        var services = CreateServices();
        services.AddWorld<OverflowWorld>()
            .AddParticleModule()
            .ConfigureOptions<ParticleOptions>(options => options.TimeScale = 2f);
        using var provider = BuildProvider(services);
        using var world = CreateWorld<OverflowWorld>(provider);
        AddMovingParticle(world, lifetime: float.MaxValue);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            world.Update(new UpdateTick(float.MaxValue, float.MaxValue)));
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }

    private static ServiceProvider BuildProvider(IServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

    private static ManagedWorld CreateWorld<TMarker>(IServiceProvider provider) where TMarker : IWorldMarker
    {
        var signature = WorldSignature.For<TMarker>();
        return (ManagedWorld)provider.GetRequiredKeyedService<IWorldFactory>(signature.MarkerType)
            .CreateWorld(typeof(TMarker).Name, signature);
    }

    private static void AddMovingParticle(World world, float lifetime)
    {
        world.Store.CreateEntity(new Particle(new ParticleEmission
        {
            Position = Vector2.Zero,
            Velocity = new Vector2(10f, 0f),
            Lifetime = lifetime,
        }));
    }

    private static ParticleState ReadSingleParticle(World world)
    {
        foreach (var (particles, _) in world.Store.Query<Particle>().Chunks)
        {
            if (particles.Length > 0)
                return particles[0].State;
        }

        throw new InvalidOperationException("Expected a particle entity.");
    }

    private static int CountParticles(World world)
    {
        var count = 0;
        foreach (var (particles, _) in world.Store.Query<Particle>().Chunks)
            count += particles.Length;
        return count;
    }

    public sealed class HalfSpeedWorld : IWorldMarker;
    public sealed class DoubleSpeedWorld : IWorldMarker;
    public sealed class PausedWorld : IWorldMarker;
    public sealed class ExpiredWorld : IWorldMarker;
    public sealed class DefaultSpeedWorld : IWorldMarker;
    public sealed class NegativeScaleWorld : IWorldMarker;
    public sealed class InvalidScaleWorld : IWorldMarker;
    public sealed class OverflowWorld : IWorldMarker;
}