using Microsoft.Xna.Framework.Content.Pipeline;
using MonoGame.Framework.Content.Pipeline.Builder;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.Pak;
using System.CommandLine;
using MGContentBuilder = MonoGame.Framework.Content.Pipeline.Builder.ContentBuilder;
using MGContentBuilderParams = MonoGame.Framework.Content.Pipeline.Builder.ContentBuilderParams;

namespace KittyCat.Content.Tools;

/// <summary>
/// Configures source, intermediate, output, platform, and packaging behavior for a content build.
/// </summary>
public record ContentBuilderOptions
{

    public TargetPlatform Platform { get; set; } = TargetPlatform.DesktopGL;
    public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();
    public string SourceDirectory { get; set; } = "Assets";
    public string IntermediateDirectory { get; set; } = "obj/Content";
    public string OutputDirectory { get; set; } = "bin/Content";
    public string RootedSourceDirectory => MakeRooted(SourceDirectory);
    public string RootedIntermediateDirectory => MakeRooted(IntermediateDirectory);
    public string RootedOutputDirectory => MakeRooted(OutputDirectory);
    public bool GenerateMetadata { get; set; }
    public bool CreatePak { get; set; }

    /// <summary>
    /// Applies supported command-line arguments to the supplied defaults.
    /// </summary>
    public static ContentBuilderOptions Parse(string[] args, ContentBuilderOptions? defaults = null)
    {
        var ret = defaults ?? new ContentBuilderOptions();
        args ??= []; // Ensure args is not null

        var sourceDirOption = new Option<string>(
            aliases: ["--source-dir", "-s"],
            getDefaultValue: () => "Assets",
            description: "Složka se zdrojovými assety ke kompilaci.");

        var platformOption = new Option<TargetPlatform>(
            aliases: ["--platform", "-p"],
            getDefaultValue: () => TargetPlatform.DesktopGL,
            description: "Cílová platforma pro MonoGame kompilaci.");

        var pakOption = new Option<bool>(
            aliases: ["--pak"],
            getDefaultValue: () => false,
            description: "Zabalí zkompilovaný obsah do PAK archivu.");

        var outputDirOpt = new Option<string>(["--output-dir", "-o"], () => ret.OutputDirectory, "Výstupní složka.");
        var intermediateDirOpt = new Option<string>(["--intermediate-dir", "-i"], () => ret.IntermediateDirectory, "Výstupní složka.");

        var rootCommand = new RootCommand("PurrplingCore Content Builder")
        {
            sourceDirOption,
            outputDirOpt,
            intermediateDirOpt,
            platformOption,
            pakOption
        };

        rootCommand.SetHandler(context =>
        {
            ret.SourceDirectory = context.ParseResult.GetValueForOption(sourceDirOption)!;
            ret.IntermediateDirectory = context.ParseResult.GetValueForOption(intermediateDirOpt) ?? ret.IntermediateDirectory;
            ret.OutputDirectory = context.ParseResult.GetValueForOption(outputDirOpt) ?? ret.OutputDirectory;
            ret.Platform = context.ParseResult.GetValueForOption(platformOption);
            ret.CreatePak = context.ParseResult.GetValueForOption(pakOption);
        });

        rootCommand.Invoke(args);

        return ret;
    }

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
/// Compiles MonoGame content and publishes it either as loose files or directory-grouped PAKs.
/// </summary>
/// <remarks>
/// PAK mode also creates one semantic asset catalog per package. Runtime catalog discovery can
/// therefore identify assets without understanding the PAK container format.
/// </remarks>
public class PakContentBuilder
{
    private readonly InnerBuilder _inner;
    private readonly ContentCollection _collection;

    public ContentCollection Collection => _collection;

    private class InnerBuilder(PakContentBuilder parent) : MGContentBuilder
    {
        public override IContentCollection GetContentCollection()
        {
            return parent._collection;
        }
    }

    public uint FailedToBuild => _inner.FailedToBuild;
    public uint SucceededToBuild => _inner.SucceededToBuild;

    public PakContentBuilder()
    {
        _collection = new ContentCollection();
        _inner = new InnerBuilder(this);

        _collection.IncludeCopy<WildcardRule>("*.meta.json");
    }

    /// <summary>
    /// Executes the complete compile-and-publish pipeline.
    /// </summary>
    public void Run(ContentBuilderOptions parameters)
    {
        var compiledDirectory = Build(parameters);
        if (compiledDirectory != null)
        {
            Publish(parameters, compiledDirectory);
        }
    }

    /// <summary>
    /// Compiles configured source assets into platform-specific XNB files.
    /// </summary>
    /// <returns>The compiled output directory, or <see langword="null"/> when nothing succeeded.</returns>
    public string? Build(ContentBuilderOptions parameters)
    {
        var innerParams = new MGContentBuilderParams()
        {
            WorkingDirectory = parameters.WorkingDirectory,
            SourceDirectory = parameters.SourceDirectory,
            IntermediateDirectory = parameters.IntermediateDirectory,
            OutputDirectory = Path.Combine(parameters.IntermediateDirectory, "Content"),
            Platform = parameters.Platform,
            Mode = ContentBuilderMode.Builder,
        };

        _inner.Logger.LoggerLogLevel = innerParams.LogLevel;
        _inner.Logger.LoggerRootDirectory = innerParams.WorkingDirectory;
        _inner.Logger.Log($"Output directory: {parameters.OutputDirectory}");
        _inner.Logger.Log($"Intermediate directory: {parameters.IntermediateDirectory}");
        _inner.Run(innerParams);

        return _inner.SucceededToBuild > 0
            ? innerParams.RootedOutputDirectory
            : null;
    }

    /// <summary>
    /// Publishes compiled XNB files as loose content or PAK archives according to the options.
    /// </summary>
    public void Publish(ContentBuilderOptions parameters, string compiledDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(compiledDirectory);

        if (parameters.CreatePak)
        {
            CreatePak(parameters, compiledDirectory);
        }
        else
        {
            CopyFilesRecursively(compiledDirectory, parameters.RootedOutputDirectory);
        }
    }

    private static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
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

    private void CreatePak(ContentBuilderOptions parameters, string compiledDirectory)
    {
        var paker = new PakArchivePacker();
        var dirs = Directory.GetDirectories(compiledDirectory, "*", SearchOption.TopDirectoryOnly);

        // Each top-level content directory is an independently mountable package.
        foreach (string dirPath in dirs)
        {
            var dirName = Path.GetFileName(dirPath);
            var outputPak = Path.Combine(parameters.RootedOutputDirectory, "Content/Paks", Path.ChangeExtension(dirName, ".pak"));
            var catalogPath = Path.Combine(
                parameters.RootedIntermediateDirectory,
                AssetCatalog.DirectoryName,
                Path.ChangeExtension(dirName, ".bin"));

            try
            {
                paker.AddDirectory(dirPath, $"Content/{dirName}");
                // The unique sidecar path lets an aggregate provider expose every package catalog.
                if (AssetCatalogBuilder.Generate(
                    parameters.RootedSourceDirectory,
                    compiledDirectory,
                    packageId: dirName,
                    outputPath: catalogPath,
                    contentPathPrefix: dirName) > 0)
                {
                    paker.AddFile(catalogPath, $"Content/{AssetCatalog.DirectoryName}/{dirName}.bin");
                }

                paker.Pack(outputPak);
                _inner.Logger.Log($"PAK: {dirName} packed to file: {outputPak}");
            }
            catch (Exception ex)
            {
                _inner.Logger.Log(LogLevel.Error, $"PAK: Error during packing {outputPak}: {ex}");
            }
            finally
            {
                paker.Clear();
            }
        }
    }
}
