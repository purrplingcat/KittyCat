using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Tests.Doubles;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Tests.Fixtures;

internal sealed class AssetManagerFixture : IDisposable
{
    private readonly ServiceProvider _services;

    public ContentPathResolver Resolver { get; } = new();
    public AssetRegistry Registry { get; } = new(NullLogger<AssetRegistry>.Instance);
    public MemoryFileSystem Files { get; } = new();
    public AssetCache<TestAsset> Cache { get; } = new();
    public RecordingAssetLoader Loader { get; } = new();
    public RecordingContentProvider ContentProvider { get; }
    public AssetCatalogIndexer Indexer { get; }
    public AssetManager Manager { get; }

    public AssetManagerFixture(
        string defaultRoot = "Content",
        bool registerLoader = true,
        bool registerCache = false)
    {
        var services = new ServiceCollection();
        if (registerCache)
        {
            services.AddSingleton(Cache);
        }

        if (registerLoader)
        {
            services.AddSingleton<IContentLoader<TestAsset>>(Loader);
        }

        _services = services.BuildServiceProvider();
        ContentProvider = new RecordingContentProvider(_services, defaultRoot);
        Indexer = new AssetCatalogIndexer(
            Registry,
            NullLogger<AssetCatalogIndexer>.Instance,
            Options.Create(new ContentManagerOptions { ContentRoot = defaultRoot }),
            new VirtualFileProvider(Files),
            Resolver);
        Manager = new AssetManager(ContentProvider, Registry, Indexer, Resolver);
    }

    public AssetId Register(string packageName, string objectPath)
    {
        var metadata = AssetMetadata.Create(packageName, objectPath);
        Registry.RegisterAsset(metadata);
        return metadata.Id;
    }

    public void Dispose()
    {
        ContentProvider.CreatedManager.Dispose();
        ContentProvider.ContentManager.Dispose();
        _services.Dispose();
        Files.Dispose();
    }
}