using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.WorldBuilding;
using Xunit;

namespace PurrplingCore.Toolkit.Tests.Ecs;

public class WorldServiceTests
{
    [Fact]
    public void ServicesSelectTheWorldImplementationAndAreDisposedExactlyOnce()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorld<FirstWorld>()
            .AddService<IWorldService, FirstService>()
            .AddService<Consumer>();
        services.AddWorld<SecondWorld>()
            .AddService<IWorldService, SecondService>()
            .AddService<Consumer>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
        using var first = CreateWorld<FirstWorld>(provider);
        using var another = CreateWorld<FirstWorld>(provider);
        using var second = CreateWorld<SecondWorld>(provider);

        var firstService = Assert.IsType<FirstService>(first.Services.GetRequiredService<Consumer>().Service);
        var anotherService = Assert.IsType<FirstService>(another.Services.GetRequiredService<Consumer>().Service);
        var secondService = Assert.IsType<SecondService>(second.Services.GetRequiredService<Consumer>().Service);

        Assert.Same(firstService, first.Services.GetRequiredService<IWorldService>());
        Assert.NotSame(firstService, anotherService);
        first.Dispose();

        Assert.Equal(1, firstService.DisposeCount);
        Assert.Equal(0, anotherService.DisposeCount);
        Assert.Equal(0, secondService.DisposeCount);
        first.Dispose();
        Assert.Equal(1, firstService.DisposeCount);
    }

    [Fact]
    public void WorldOptionsUseNamedValuesAndAreScopedPerWorld()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfigureOptions<WorldTestOptions>, SharedWorldTestOptions>();
        services.AddSingleton<IPostConfigureOptions<WorldTestOptions>, FirstWorldTestOptions>();
        services.AddWorld<FirstWorld>();
        services.AddWorld<SecondWorld>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
        using var first = CreateWorld<FirstWorld>(provider);
        using var anotherFirst = CreateWorld<FirstWorld>(provider);
        using var second = CreateWorld<SecondWorld>(provider);

        var firstOptions = first.Services.GetRequiredService<IWorldOptions<WorldTestOptions>>();
        var anotherFirstOptions = anotherFirst.Services.GetRequiredService<IWorldOptions<WorldTestOptions>>();
        var secondOptions = second.Services.GetRequiredService<IWorldOptions<WorldTestOptions>>();

        Assert.Equal(2, firstOptions.Value.Value);
        Assert.Equal(2, anotherFirstOptions.Value.Value);
        Assert.Equal(10, secondOptions.Value.Value);
        Assert.NotSame(firstOptions, anotherFirstOptions);
        Assert.NotSame(firstOptions.Value, anotherFirstOptions.Value);
        Assert.Same(firstOptions.Value, first.Services.GetRequiredService<IWorldOptions<WorldTestOptions>>().Value);
        Assert.Equal(typeof(FirstWorld).AssemblyQualifiedName, WorldOptions.GetName(WorldSignature.For<FirstWorld>()));
        Assert.NotEqual(WorldOptions.GetName(WorldSignature.For<FirstWorld>()),
            WorldOptions.GetName(WorldSignature.For<SecondWorld>()));
    }

    [Fact]
    public void ConfigureOptionsAppliesOnlyToTheConfiguredWorldMarker()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorld<FirstWorld>().ConfigureOptions<WorldTestOptions>(options => options.Value = 2);
        services.AddWorld<SecondWorld>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
        using var first = CreateWorld<FirstWorld>(provider);
        using var second = CreateWorld<SecondWorld>(provider);

        Assert.Equal(2, first.Services.GetRequiredService<IWorldOptions<WorldTestOptions>>().Value.Value);
        Assert.Equal(0, second.Services.GetRequiredService<IWorldOptions<WorldTestOptions>>().Value.Value);
    }

    [Fact]
    public void MarkerOptionsOverrideSharedDefaultsAndMergeOtherValues()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorld<FirstWorld>().ConfigureOptions<WorldTestOptions>(options => options.Value = 2);
        services.ConfigureAll<WorldTestOptions>(options =>
        {
            options.Value = 10;
            options.Other = 5;
        });
        services.Configure<WorldTestOptions>(options => options.Other = 99);
        services.AddWorld<SecondWorld>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
        using var first = CreateWorld<FirstWorld>(provider);
        using var second = CreateWorld<SecondWorld>(provider);

        var firstOptions = first.Services.GetRequiredService<IWorldOptions<WorldTestOptions>>().Value;
        var secondOptions = second.Services.GetRequiredService<IWorldOptions<WorldTestOptions>>().Value;
        Assert.Equal(2, firstOptions.Value);
        Assert.Equal(5, firstOptions.Other);
        Assert.Equal(10, secondOptions.Value);
        Assert.Equal(5, secondOptions.Other);
    }

    [Fact]
    public void FeaturesApplyImmediatelyAndDeduplicateDependenciesPerMarker()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorld<FirstWorld>().AddFeature<ConsumerFeature>().AddFeature<ServiceFeature>();
        services.AddWorld<SecondWorld>().AddFeature<ServiceFeature>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
        using var first = CreateWorld<FirstWorld>(provider);
        using var another = CreateWorld<FirstWorld>(provider);

        Assert.Equal(2, provider.GetServices<FeatureProbe>().Count());
        Assert.IsType<FirstService>(first.Services.GetRequiredService<Consumer>().Service);
        var firstModule = Assert.IsType<ServiceModule>(
            Assert.Single(first.Services.GetKeyedServices<IWorldModule>(typeof(FirstWorld))));
        var anotherModule = Assert.IsType<ServiceModule>(
            Assert.Single(another.Services.GetKeyedServices<IWorldModule>(typeof(FirstWorld))));
        Assert.Equal(1, firstModule.SetupCount);
        Assert.NotSame(firstModule, anotherModule);
        Assert.Same(first.Services.GetRequiredService<IWorldService>(), firstModule.Service);
    }

    [Fact]
    public void ServicesAreUnavailableToUnconfiguredWorldsAndTheRootProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorld<FirstWorld>().AddService<IWorldService, FirstService>();
        services.AddWorld<SecondWorld>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var second = CreateWorld<SecondWorld>(provider);

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IWorldService>());
        Assert.Throws<InvalidOperationException>(() => second.Services.GetRequiredService<IWorldService>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WorldModulesCanUseApplicationServiceOverrides(bool registerGlobalFirst)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (registerGlobalFirst)
            services.AddSingleton<IWorldService, FirstService>();

        services.AddWorld<FirstWorld>()
            .AddService<IWorldService, SecondService>()
            .AddService<Consumer>()
            .AddModule<ServiceModule>();

        if (!registerGlobalFirst)
            services.AddSingleton<IWorldService, FirstService>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
        using var first = CreateWorld<FirstWorld>(provider);
        using var second = CreateWorld<FirstWorld>(provider);
        var globalService = Assert.IsType<FirstService>(provider.GetRequiredService<IWorldService>());
        var firstModule = Assert.IsType<ServiceModule>(
            Assert.Single(first.Services.GetKeyedServices<IWorldModule>(typeof(FirstWorld))));
        var secondModule = Assert.IsType<ServiceModule>(
            Assert.Single(second.Services.GetKeyedServices<IWorldModule>(typeof(FirstWorld))));

        Assert.Same(globalService, first.Services.GetRequiredService<Consumer>().Service);
        Assert.Same(globalService, second.Services.GetRequiredService<Consumer>().Service);
        Assert.Same(globalService, firstModule.Service);
        Assert.Same(globalService, secondModule.Service);
        Assert.Equal(1, firstModule.SetupCount);
        Assert.Equal(1, secondModule.SetupCount);

        first.Dispose();
        second.Dispose();
        Assert.Equal(0, globalService.DisposeCount);

        provider.Dispose();
        Assert.Equal(1, globalService.DisposeCount);
    }

    [Fact]
    public void DerivedBlueprintsResolveInheritedModulesInTheActualWorld()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorld<FirstWorld>().AddFeature<ConsumerFeature>();
        services.AddWorld<SecondWorld>().AddFeature<ServiceFeature>().DeriveWorld<FirstWorld>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
        using var first = CreateWorld<FirstWorld>(provider);
        using var second = CreateWorld<SecondWorld>(provider);
        var firstModule = Assert.IsType<ServiceModule>(
            Assert.Single(first.Services.GetKeyedServices<IWorldModule>(typeof(FirstWorld))));
        var inheritedModule = Assert.IsType<ServiceModule>(
            Assert.Single(second.Services.GetKeyedServices<IWorldModule>(typeof(FirstWorld))));

        Assert.NotSame(firstModule, inheritedModule);
        Assert.Equal(1, inheritedModule.SetupCount);
        Assert.Same(second.Services.GetRequiredService<IWorldService>(), inheritedModule.Service);
        Assert.NotSame(firstModule.Service, inheritedModule.Service);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AllModuleRegistrationsUseWorldScopeAndDisposeOnce(bool useFactory)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddWorld<FirstWorld>()
            .AddService<IWorldService, FirstService>()
            .Configure(static _ => { });

        if (useFactory)
            builder.AddModule(CreateServiceModule).AddModule(CreateServiceModule);
        else
            builder.AddModule<ServiceModule>().AddModule<ServiceModule>();

        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == typeof(IWorldModule))
                Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
        using var first = CreateWorld<FirstWorld>(provider);
        using var second = CreateWorld<FirstWorld>(provider);
        var firstModules = first.Services.GetKeyedServices<IWorldModule>(typeof(FirstWorld)).ToArray();
        var secondModules = second.Services.GetKeyedServices<IWorldModule>(typeof(FirstWorld)).ToArray();
        var firstModule = Assert.Single(firstModules.OfType<ServiceModule>());
        var secondModule = Assert.Single(secondModules.OfType<ServiceModule>());

        Assert.Equal(2, firstModules.Length);
        Assert.Equal(2, secondModules.Length);
        for (var index = 0; index < firstModules.Length; index++)
            Assert.NotSame(firstModules[index], secondModules[index]);

        Assert.Equal(1, firstModule.SetupCount);
        Assert.Equal(1, secondModule.SetupCount);
        Assert.Same(first.Services.GetRequiredService<IWorldService>(), firstModule.Service);
        Assert.Same(second.Services.GetRequiredService<IWorldService>(), secondModule.Service);
        first.Dispose();
        first.Dispose();
        Assert.Equal(1, firstModule.DisposeCount);
        Assert.Equal(0, secondModule.DisposeCount);

        second.Dispose();
        Assert.Equal(1, secondModule.DisposeCount);
    }

    private static ServiceModule CreateServiceModule(IServiceProvider provider) =>
        new(provider.GetRequiredService<IWorldService>());

    private static ManagedWorld CreateWorld<TMarker>(IServiceProvider provider) where TMarker : IWorldMarker
    {
        var signature = WorldSignature.For<TMarker>();
        return (ManagedWorld)provider.GetRequiredKeyedService<IWorldFactory>(signature.MarkerType)
            .CreateWorld(typeof(TMarker).Name, signature);
    }

    public sealed class FirstWorld : IWorldMarker;
    public sealed class SecondWorld : IWorldMarker;
    public interface IWorldService;

    public sealed class FirstService : IWorldService, IDisposable
    {
        public int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
    }

    public sealed class SecondService : IWorldService, IDisposable
    {
        public int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
    }

    public sealed class Consumer(IWorldService service)
    {
        public IWorldService Service { get; } = service;
    }

    public sealed class FeatureProbe;

    public sealed class WorldTestOptions
    {
        public int Value { get; set; }
        public int Other { get; set; }
    }

    private sealed class SharedWorldTestOptions : IConfigureNamedOptions<WorldTestOptions>
    {
        public void Configure(WorldTestOptions options) => options.Value = 10;

        public void Configure(string? name, WorldTestOptions options) => Configure(options);
    }

    private sealed class FirstWorldTestOptions : IPostConfigureOptions<WorldTestOptions>
    {
        public void PostConfigure(string? name, WorldTestOptions options)
        {
            if (name == WorldOptions.GetName(WorldSignature.For<FirstWorld>()))
                options.Value = 2;
        }
    }

    public sealed class ServiceFeature : IWorldFeature
    {
        public void ConfigureServices(IWorldServicesBuilder builder)
        {
            builder.AddService<IWorldService, FirstService>();
            builder.Services.AddSingleton<FeatureProbe>();
        }
    }

    public sealed class ConsumerFeature : IWorldFeature
    {
        public void ConfigureServices(IWorldServicesBuilder builder)
        {
            builder.AddFeature<ServiceFeature>()
                .AddService<Consumer>()
                .AddModule<ServiceModule>()
                .AddModule<ServiceModule>();
        }
    }

    public sealed class ServiceModule(IWorldService service) : IWorldModule, IDisposable
    {
        public IWorldService Service { get; } = service;
        public int Order => 0;
        public int SetupCount { get; private set; }
        public int DisposeCount { get; private set; }
        public void Setup(IWorldBlueprint builder) => SetupCount++;
        public void Dispose() => DisposeCount++;
    }
}