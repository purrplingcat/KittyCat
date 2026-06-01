using Zio;

namespace PurrplingCore.Toolkit.Vfs.Comparers;

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