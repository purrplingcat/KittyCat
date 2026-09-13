/// <summary>
/// Entry point for the Content Builder project,
/// which when executed will build content according to the "Content Collection Strategy" defined in the Builder class.
/// </summary>
/// <remarks>
/// Make sure to validate the directory paths in the "ContentBuilderParams" for your specific project.
/// For more details regarding the Content Builder, see the MonoGame documentation: <tbc.>
/// </remarks>

using KittyCat.Content.Tools;
using Microsoft.Xna.Framework.Content.Pipeline;
using MonoGame.Framework.Content.Pipeline.Builder;

var contentCollectionArgs = new ContentBuilderOptions
{
    WorkingDirectory = $"{AppContext.BaseDirectory}../../", // path to where your content folder can be located
    SourceDirectory = "Assets", // Not actually needed as this is the default, but added for reference
    Platform = TargetPlatform.DesktopGL,
    CreatePak = true,
};
var builder = new PakContentBuilder();

builder.Collection.Include<WildcardRule>("Fonts/*.spritefont");
var options = ContentBuilderOptions.Parse(args, contentCollectionArgs);
var compiledDirectory = builder.Build(options);
if (compiledDirectory != null)
{
    AssetCatalogBuilder.Generate(options.RootedSourceDirectory, compiledDirectory);
    builder.Publish(options, compiledDirectory);
}

/*if (args != null && args.Length > 0)
{
    builder.Run(MonoGame.Framework.Content.Pipeline.Builder.ContentBuilderParams.Parse(args));
}
else
{
    builder.Run(contentCollectionArgs);
}*/

return builder.FailedToBuild > 0 ? -1 : 0;
