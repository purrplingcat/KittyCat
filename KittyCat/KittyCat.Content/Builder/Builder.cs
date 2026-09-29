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
var options = ContentBuilderOptions.Parse(args, contentCollectionArgs);
var builder = ApplicationHost.CreateBuilder();
builder.Services.AddSingleton(options);
builder.Services.AddSingleton<PakContentBuilder>();
builder.Services.AddSingleton<IHostedApplication, AssetBuildJob>();

using var host = builder.Build();
return host.Run();

internal sealed class AssetBuildJob(PakContentBuilder contentBuilder, ContentBuilderOptions options) : IHostedApplication
{
    public int Run()
    {
        contentBuilder.Collection.Include<WildcardRule>("Fonts/*.spritefont");
        var compiledDirectory = contentBuilder.Build(options);
        if (compiledDirectory != null)
        {
            AssetCatalogBuilder.Generate(options.RootedSourceDirectory, compiledDirectory);
            contentBuilder.Publish(options, compiledDirectory);
        }

        return contentBuilder.FailedToBuild > 0 ? -1 : 0;
    }
}
