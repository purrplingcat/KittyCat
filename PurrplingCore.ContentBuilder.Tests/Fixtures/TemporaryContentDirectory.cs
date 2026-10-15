namespace PurrplingCore.ContentBuilder.Tests.Fixtures;

internal sealed class TemporaryContentDirectory : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public TemporaryContentDirectory()
    {
        Directory.CreateDirectory(Root);
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);
}