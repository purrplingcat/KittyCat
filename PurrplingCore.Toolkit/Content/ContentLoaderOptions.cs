namespace PurrplingCore.Toolkit.Content;

public class ContentLoaderOptions
{
    public Dictionary<string, Type> ExtensionMappings { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void MapExtension<TLoader>(string extension) where TLoader : IContentLoader
    {
        ExtensionMappings[extension] = typeof(TLoader);
    }
}
