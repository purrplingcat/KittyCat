using System.IO.Hashing;
using System.Text;


namespace PurrplingCore.Toolkit.Pak;

internal static class PathHasher
{
    public static ulong GetHash(ReadOnlySpan<char> path)
    {
        if (path.IsEmpty) return 0;

        Span<char> normalizedPath = stackalloc char[path.Length];
        NormalizePath(path, normalizedPath);

        int maxBytes = Encoding.UTF8.GetMaxByteCount(normalizedPath.Length);
        Span<byte> pathBytes = stackalloc byte[maxBytes];
        int bytesWritten = Encoding.UTF8.GetBytes(normalizedPath, pathBytes);

        return XxHash64.HashToUInt64(pathBytes[..bytesWritten]);
    }

    private static void NormalizePath(ReadOnlySpan<char> source, Span<char> destination)
    {
        source.ToLowerInvariant(destination);
        destination.Replace('\\', '/');
        destination.TrimStart('/');
    }
}

