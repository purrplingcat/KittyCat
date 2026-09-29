/// <summary>
/// Entry point for the Content Builder project,
/// which when executed will build content according to the "Content Collection Strategy" defined in the Builder class.
/// </summary>
/// <remarks>
/// Make sure to validate the directory paths in the "ContentBuilderParams" for your specific project.
/// For more details regarding the Content Builder, see the MonoGame documentation: <tbc.>
/// </remarks>

using KittyCat.Content.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework.Content.Pipeline;
using MonoGame.Framework.Content.Pipeline.Builder;
using PurrplingCore.Toolkit.Hosting;

var contentCollectionArgs = new ContentBuilderOptions
{
    WorkingDirectory = $"{AppContext.BaseDirectory}../../", // path to where your content folder can be located
    SourceDirectory = "Assets", // Not actually needed as this is the default, but added for reference
    Platform = TargetPlatform.DesktopGL,
    CreatePak = true,
};
// Parse command-line arguments to override defaults
var options = ContentBuilderOptions.Parse(args, contentCollectionArgs);
var builder = ApplicationHost.CreateBuilder();
builder.Services.AddSingleton(options);
builder.Services.AddSingleton<PakContentBuilder>();
builder.Services.AddSingleton<IHostedApplication, AssetBuildJob>();

using var host = builder.Build();
return host.Run();

/// <summary>
/// Handles the actual asset building and packaging operation.
/// </summary>
/// <remarks>
/// This internal hosted application is responsible for including asset rules,
/// building the content, generating the asset catalog, and publishing the final PAK.
/// </remarks>
internal sealed class AssetBuildJob(PakContentBuilder contentBuilder, ContentBuilderOptions options) : IHostedApplication
{
    /// <summary>
    /// Runs the asset build process including content collection, compilation, and packaging.
    /// </summary>
    /// <remarks>
    /// Process:
    /// 1. Adds wildcard rules for spritefonts
    /// 2. Builds all content
    /// 3. Generates an asset catalog
    /// 4. Publishes the compiled content to a PAK file
    /// </remarks>
    /// <returns>0 on successful build, -1 if any assets failed to build.</returns>
    public int Run()
    {
        // Include all spritefont files in the build
        contentBuilder.Collection.Include<WildcardRule>("Fonts/*.spritefont");
        
        // Build all content and get the output directory
        var compiledDirectory = contentBuilder.Build(options);
        if (compiledDirectory != null)
        {
            // Generate a catalog of all assets for runtime lookup
            AssetCatalogBuilder.Generate(options.RootedSourceDirectory, compiledDirectory);
            
            // Publish the compiled content as a PAK file
            contentBuilder.Publish(options, compiledDirectory);
        }

        // Return exit code based on build success
        return contentBuilder.FailedToBuild > 0 ? -1 : 0;
    }
}
