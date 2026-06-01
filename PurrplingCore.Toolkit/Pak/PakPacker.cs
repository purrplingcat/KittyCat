using System.Security.Cryptography;
using System.Text;

namespace PurrplingCore.Toolkit.Pak;

public class PakPacker()
{
    private readonly List<PendingFile> _files = [];
    private readonly byte[]? _encryptionKey;

    public CompressionMethod Compression { get; set; } = CompressionMethod.None;

    public PakPacker(byte[] encryptionKey) : this()
    {
        _encryptionKey = encryptionKey;
    }

    private struct PendingFile
    {
        public ulong Hash;
        public string VirtualPath;
        public string PhysicalPath;
    }

    private struct TocEntry
    {
        public ulong Hash;
        public string Path;
        public long Offset;
        public int UncompressedSize;
        public long CompressedLength;
    }

    private void WriteHeader(BinaryWriter writer)
    {
        byte[] keySignature = _encryptionKey != null 
            ? PakHelper.GetKeySignature(_encryptionKey!) 
            : new byte[32];
        
        // Header structure (18 bytes + variable length mount point)
        writer.Write(PakArchive.MAGIC); // 4 bytes
        writer.Write(PakArchive.SUPPORTED_VERSION); // 4 bytes
        writer.Write((byte)Compression); // 1 byte
        writer.Write(DateTime.UtcNow.ToBinary()); // Packed date (8 bytes)
        writer.Write(_encryptionKey != null); // Encrypted flag (1 byte)
        writer.Write(keySignature); // 32 bytes (key signature or zeros)
        writer.Write(stackalloc byte[14]); // Padding (Header 64 bytes total)
        writer.Flush();
    }

    public void Pack(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (_files.Count == 0)
            throw new InvalidOperationException("No files to pack.");

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        
        WriteHeader(writer); // Header
        WriteFiles(writer, out TocEntry[] toc); // Files
        WriteToc(writer, toc); // TOC

        writer.Flush();
        stream.Flush();
    }

    private Stream CreateTocBuffer(Stream stream)
    {
        if (_encryptionKey != null)
        {
            return PakHelper.CreateEncryptionStream(stream, _encryptionKey!, 0);
        }

        return new BufferedStream(stream);
    }

    public void Pack(string outputPath)
    {
        using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        Pack(fs);
    }

    private void WriteToc(BinaryWriter writer, TocEntry[] toc)
    {
        writer.Flush();
        long tocStartPos = writer.BaseStream.Position;
        byte[] tocBytes = SerializeToc(toc);

        if (_encryptionKey != null)
        {
            tocBytes = PakHelper.EncryptBytes(tocBytes, _encryptionKey!, tocStartPos);
        }

        writer.Write(tocBytes);
        writer.Write(tocBytes.Length); // Skutečná velikost na disku!
        writer.Write(tocStartPos);
        writer.Flush();
    }

    private byte[] SerializeToc(TocEntry[] toc)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8);

        writer.Write((ushort)toc.Length);
        for (int i = 0; i < toc.Length; i++)
        {
            TocEntry entry = toc[i];
            writer.Write(entry.Hash);
            writer.Write(entry.Path);
            writer.Write(entry.Offset);
            writer.Write(entry.UncompressedSize);
            writer.Write(entry.CompressedLength);
        }
        ms.Flush();

        return ms.ToArray();
    }

    private void WriteFiles(BinaryWriter writer, out TocEntry[] toc)
    {
        toc = new TocEntry[_files.Count];

        for (int i = 0; i < _files.Count; i++)
        {
            PendingFile entry = _files[i];
            long startPos = writer.BaseStream.Position;
            using FileStream source = File.OpenRead(entry.PhysicalPath);

            if (source.Length > int.MaxValue)
                throw new NotSupportedException($"File '{entry.VirtualPath}' is over 2GB.");

            if (_encryptionKey != null)
                WriteEncryptedFile(source, writer.BaseStream, startPos);
            else
                WriteFile(source, writer.BaseStream);

            // Record TOC entry
            toc[i] = new TocEntry
            {
                Hash = entry.Hash,
                Path = entry.VirtualPath,
                Offset = startPos,
                CompressedLength = writer.BaseStream.Position - startPos,
                UncompressedSize = (int)source.Length
            };
        }

        writer.Flush();
    }

    private TocEntry CreateTocEntry(PendingFile file, long startPos, long endPos, long sourceLength)
    {
        return new TocEntry
        {
            Hash = file.Hash,
            Path = file.VirtualPath,
            Offset = startPos,
            CompressedLength = (int)(endPos - startPos),
            UncompressedSize = (int)sourceLength
        };
    }

    public void WriteFile(Stream input, Stream output)
    {
        using Stream compressionStream = PakHelper.CreateCompressionStream(output, Compression);
        input.CopyTo(compressionStream);
    }

    public void WriteEncryptedFile(Stream input, Stream output, long iv)
    {
        using CryptoStream encryptionStream = PakHelper.CreateEncryptionStream(output, _encryptionKey!, iv);
        using Stream compressionStream = PakHelper.CreateCompressionStream(encryptionStream, Compression);
        input.CopyTo(compressionStream);
    }

    public void AddFile(string physicalPath, string virtualPath)
    {
        if (!File.Exists(physicalPath))
            throw new FileNotFoundException($"File not found: {physicalPath}");

        _files.Add(new PendingFile
        {
            Hash = PathHasher.GetHash(virtualPath),
            VirtualPath = NormalizeVirtualPath(virtualPath),
            PhysicalPath = physicalPath
        });
    }

    private static string NormalizeVirtualPath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    public void AddDirectory(string physicalDir, string virtualPrefix = "")
    {
        if (!Directory.Exists(physicalDir))
            throw new DirectoryNotFoundException($"Directory not found: {physicalDir}");

        foreach (string file in Directory.GetFiles(physicalDir, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(physicalDir, file).Replace('\\', '/');
            string vPath = string.IsNullOrEmpty(virtualPrefix) ? relative : $"{virtualPrefix}/{relative}";

            AddFile(file, vPath);
        }
    }
}
