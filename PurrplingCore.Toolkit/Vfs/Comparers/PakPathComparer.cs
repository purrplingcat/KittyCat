using Zio;

namespace PurrplingCore.Toolkit.Vfs.Comparers;

/// <summary>
/// Defines deterministic Unreal-style package discovery order from PAK filenames.
/// </summary>
/// <remarks>
/// Patch packages ending in <c>_P</c> sort before base packages; packages within the same group
/// sort by filename without regard to casing. Adding or removing a PAK therefore changes the
/// mounted set without maintaining a separate manifest.
/// </remarks>
public class PakPathComparer : IComparer<string>
{
    public static PakPathComparer Default { get; } = new PakPathComparer();

    private readonly struct PakInfo
    {
        public string NameWithoutExtension { get; }
        public bool IsPatch { get; }

        public PakInfo(UPath path)
        {
            NameWithoutExtension = path.GetNameWithoutExtension() ?? string.Empty;
            IsPatch = NameWithoutExtension.EndsWith("_P", StringComparison.OrdinalIgnoreCase);
        }
    }

    public int Compare(string? x, string? y)
    {
        var fileX = new PakInfo(new UPath(x ?? string.Empty));
        var fileY = new PakInfo(new UPath(y ?? string.Empty));

        if (fileX.IsPatch != fileY.IsPatch)
        {
            return fileX.IsPatch ? -1 : 1;
        }

        return string.Compare(
            fileX.NameWithoutExtension,
            fileY.NameWithoutExtension,
            StringComparison.OrdinalIgnoreCase
        );
    }
}