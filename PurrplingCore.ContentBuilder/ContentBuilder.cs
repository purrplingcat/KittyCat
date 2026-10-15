using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Content.Pipeline;
using MonoGame.Framework.Content.Pipeline.Builder;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.Pak;
using MGContentBuilder = MonoGame.Framework.Content.Pipeline.Builder.ContentBuilder;
using MGContentBuilderParams = MonoGame.Framework.Content.Pipeline.Builder.ContentBuilderParams;
using PipelineLogLevel = Microsoft.Xna.Framework.Content.Pipeline.LogLevel;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace PurrplingCore.ContentBuilder;

/// <summary>
/// Configures source, intermediate, output, platform, and packaging behavior for a content build.
/// </summary>
public record ContentBuilderOptions
{

    public TargetPlatform Platform { get; set; } = TargetPlatform.DesktopGL;
    public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();
    public string SourceDirectory { get; set; } = "Assets";
    public string IntermediateDirectory { get; set; } = "obj";
    public string OutputDirectory { get; set; } = "bin";
    public string RootedSourceDirectory => MakeRooted(SourceDirectory);
    public string RootedIntermediateDirectory => MakeRooted(IntermediateDirectory);
    public string RootedOutputDirectory => MakeRooted(OutputDirectory);
    public bool GenerateMetadata { get; set; }

    private string MakeRooted(string path)
    {
        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(WorkingDirectory, path);
        }

        return Path.GetFullPath(path);
    }
}

/// <summary>
/// Compiles MonoGame content and publishes it either as loose files or declared PAKs.
/// </summary>
/// <remarks>
/// Bundles can publish a semantic asset catalog from their source metadata. Overlay bundles can
/// omit the catalog while still publishing files that override an existing virtual path.
/// </remarks>
public class ContentBuilder
{
    private static readonly Dictionary<string, string[]> s_defaultPaks = new() { ["Content"] = ["."] };

    private readonly ILogger<ContentBuilder> _logger;
    private readonly BundleBuilder[] _builders = [];
    private readonly ContentCollection _collection;

    private class BundleBuilder(IAssetBundle bundle) : MGContentBuilder
    {
        private IContentCollection? _collection;

        public string Source => bundle.Source;
        public string Name => bundle.Name;
        public string Destination => bundle.Destination;

        public override IContentCollection GetContentCollection()
        {
            return _collection ??= bundle.GetContent();
        }

        public IAssetBundle GetBundle() => bundle;
    }

    public uint FailedToBuild => _builders.Aggregate(0u, (sum, b) => sum + b.FailedToBuild);
    public uint SucceededToBuild => _builders.Aggregate(0u, (sum, b) => sum + b.SucceededToBuild);

    public ContentBuilder(ILogger<ContentBuilder> logger, IEnumerable<IAssetBundle> bundles)
    {
        _logger = logger;
        _collection = new ContentCollection();
        _builders = [.. bundles.Select(b => new BundleBuilder(b) { Logger = new PipelineLogger(logger) })];

        _collection.IncludeCopy<WildcardRule>("*.meta.json");
    }

    private sealed class PipelineLogger(ILogger<ContentBuilder> logger) : ContentBuildLogger
    {
        public override void Log(PipelineLogLevel level, string message)
        {
            var logLevel = level switch
            {
                PipelineLogLevel.Error => LogLevel.Error,
                PipelineLogLevel.Warning => LogLevel.Warning,
                PipelineLogLevel.Debug => LogLevel.Debug,
                _ => LogLevel.Information
            };

            if (!logger.IsEnabled(logLevel))
            {
                return;
            }

            var asset = GetCurrentFilename();
            if (string.IsNullOrEmpty(asset))
            {
                logger.Log(logLevel, "{Message}", message);
            }
            else
            {
                logger.Log(logLevel, "{Asset}: {Message}", asset, message);
            }
        }
    }

    /// <summary>
    /// Executes the complete compile-and-publish pipeline.
    /// </summary>
    public void Run(ContentBuilderOptions parameters)
    {
        _logger.LogInformation("Building {Count} bundles", _builders.Length);
        
        foreach (var bundle in Build(parameters))
        {
            GenerateCatalog(parameters, bundle);
            Publish(parameters, bundle);
        }
    }

    /// <summary>
    /// Generates or removes the bundle's catalog before the bundle is published.
    /// </summary>
    public void GenerateCatalog(ContentBuilderOptions parameters, IAssetBundle bundle)
    {
        var compiledDirectory = Path.Combine(parameters.RootedIntermediateDirectory, bundle.Name);
        var sourceDirectory = Path.Combine(parameters.RootedSourceDirectory, bundle.Source);
        var catalogPath = Path.Combine(
            compiledDirectory,
            "Content",
            AssetCatalog.DirectoryName,
            $"{bundle.Name}.bin");

        if (!bundle.GenerateAssetCatalog)
        {
            if (File.Exists(catalogPath))
            {
                File.Delete(catalogPath);
            }

            return;
        }

        var contentRoots = BundleContentRootResolver.Resolve(bundle.GetContent(), sourceDirectory);
        var assetCount = AssetCatalogBuilder.Generate(
            sourceDirectory,
            compiledDirectory,
            bundle.PackageName,
            bundle.Name,
            catalogPath,
            contentRoots: contentRoots);
        _logger.LogInformation(
            "Generated asset catalog {CatalogPath} for bundle {BundleName} with {AssetCount} assets.",
            catalogPath,
            bundle.Name,
            assetCount);
    }

    /// <summary>
    /// Compiles configured source assets into platform-specific XNB files.
    /// </summary>
    /// <returns>The compiled output directory, or <see langword="null"/> when nothing succeeded.</returns>
    public IEnumerable<IAssetBundle> Build(ContentBuilderOptions parameters)
    {
        foreach (var builder in _builders)
        {
            var innerParams = new MGContentBuilderParams()
            {
                WorkingDirectory = parameters.WorkingDirectory,
                SourceDirectory = Path.Combine(parameters.SourceDirectory, builder.Source),
                IntermediateDirectory = Path.Combine(parameters.IntermediateDirectory, builder.Name),
                OutputDirectory = Path.Combine(parameters.IntermediateDirectory, builder.Name),
                Platform = parameters.Platform,
                Mode = ContentBuilderMode.Builder,
            };

            _logger.LogInformation("Starting build for {Bundle}", builder.Name);
            builder.Logger.LoggerLogLevel = innerParams.LogLevel;
            builder.Logger.LoggerRootDirectory = innerParams.WorkingDirectory;
            builder.Run(innerParams);

            if (builder.SucceededToBuild > 0)
            {
                yield return builder.GetBundle();
            }
        }
    }

    /// <summary>
    /// Publishes compiled XNB files as loose content or PAK archives according to the options.
    /// </summary>
    public void Publish(ContentBuilderOptions parameters, IAssetBundle bundle)
    {
        ArgumentException.ThrowIfNullOrEmpty(bundle.Destination);
        
        var compiledDirectory = Path.Combine(parameters.RootedIntermediateDirectory, bundle.Name);


        switch (bundle.Type)
        {
            case AssetBundleType.Loose:
                CopyFilesRecursively(
                    compiledDirectory,
                    Path.Combine(parameters.RootedOutputDirectory, bundle.Destination));
                return;
            case AssetBundleType.Pak:
                CreatePak(parameters, bundle, compiledDirectory);
                return;
            default:
                _logger.LogError("Unknown asset bundle type: {Type}", bundle.Type);
                return;
        }
    }

    private static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        Directory.CreateDirectory(targetPath);

        // Preserve the compiled directory tree before copying its files.
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        // Replace stale loose outputs with the newly compiled versions.
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }

    private void CreatePak(ContentBuilderOptions parameters, IAssetBundle bundle, string compiledDirectory)
    {
        var paker = new PakArchivePacker();
        var outputPak = Path.Combine(parameters.RootedOutputDirectory, bundle.Destination, $"{bundle.Name}.pak");

        try
        {
            foreach (string dirPath in Directory.GetDirectories(compiledDirectory))
            {
                var dirName = Path.GetFileName(dirPath);
                paker.AddDirectory(dirPath, dirName);
                _logger.LogDebug("PAK: Added directory {Directory} to PAK", dirName);
            }

            paker.Pack(outputPak);
            _logger.LogInformation("PAK: {Package} packed to file: {OutputPak}", bundle.Name, outputPak);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PAK: Error during packing {OutputPak}", outputPak);
        }
        finally
        {
            paker.Clear();
        }
    }
}
