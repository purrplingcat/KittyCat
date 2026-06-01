using System.Text;


namespace PurrplingCore.Toolkit.Pak;

internal sealed class ThrowHelper
{
    public static void ThrowIfInvalidPakVersion(int version)
    {
        if (version != PakArchive.SUPPORTED_VERSION)
        {
            throw new InvalidDataException($"Unsupported Pak version: {version}");
        }
    }

    public static void ThrowIfInvalidMagic(int magic)
    {
            if (magic != PakArchive.MAGIC)
            {
                throw new InvalidDataException($"Invalid Pak file: incorrect magic number.");
            }
    }

    public static void ThrowIfFileNotFound(string path, string mountPoint)
    {
        throw new FileNotFoundException($"File '{path}' not found in PCAT archive '{mountPoint}'.");
    }

    public static void ThrowIfInvalidSubStreamPosition(long position, long length)
    {
        if (position < 0 || position > length)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Position must be within the bounds of the substream.");
        }
    }

    public static void ThrowIfMountpointTooLong(string mountPoint)
    {
        if (Encoding.UTF8.GetByteCount(mountPoint) > 64)
        {
            throw new ArgumentException(
                "Mount point cannot exceed 64 bytes when UTF-8 encoded.",
                nameof(mountPoint)
            );
        }
    }
}

