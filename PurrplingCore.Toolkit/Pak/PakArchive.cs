using System.Security.Cryptography;
using System.Text;

namespace PurrplingCore.Toolkit.Pak;

public enum CompressionMethod : byte
{
    None = 0,    // Žádná komprese (nejrychlejší, ale největší soubory)
    Deflate = 1, // Standardní .NET (Zlib)
    LZ4 = 2,     // Ultra-rychlé pro hry
    Zstd = 3,    // Moderní standard (vysoká komprese i rychlost)
    Brotli = 4   // Vyvážená volba pro web a obecné použití
}

public class PakArchive : IDisposable
{
    public const int MAGIC = 0x54414350; // PCAT in little-endian
    public const int SUPPORTED_VERSION = 0x00_01_00; // Minor.Major.Patch (0.1.0)
    public const byte HEADER_SIZE = 64;

    private readonly Stream _stream;
    private readonly Dictionary<ulong, PakEntry> _entries = [];
    private readonly byte[] _encryptionKey;
    private bool _disposed;

    public int Version { get; private set; }
    public CompressionMethod Compression { get; private set; }
    public DateTime PackedDate { get; private set; }
    public bool Encrypted { get; private set; }

    public PakArchive(Stream stream, byte[]? encryptionKey = null)
    {
        if (!stream.CanRead) 
            throw new ArgumentException("Cannot read from passed stream!", nameof(stream));
        if (!stream.CanSeek) 
            throw new ArgumentException("Passed stream is not seekable!", nameof(stream));
        if (stream.Length <= HEADER_SIZE)
            throw new ArgumentException("Stream is empty or corrupted!");

        _stream = stream;
        _encryptionKey = encryptionKey ?? [];
        ReadMetadata();
    }

    public PakArchive(string filePath, byte[]? encryptionKey = null)
        : this(File.OpenRead(filePath), encryptionKey)
    {
    }

    public bool FileExists(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        return _entries.ContainsKey(PathHasher.GetHash(path));
    }

    public bool TryGetEntry(string path, out PakEntry entry)
    {
        if (string.IsNullOrEmpty(path))
        {
            entry = default;
            return false;
        }
        return _entries.TryGetValue(PathHasher.GetHash(path), out entry);
    }

    public PakEntry GetEntry(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        if (!_entries.TryGetValue(PathHasher.GetHash(path), out var entry))
        {
            throw new FileNotFoundException($"File '{path}' not found in PCAT archive.");
        }
        return entry;
    }

    public Stream OpenFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));
        
        ulong hash = PathHasher.GetHash(path);
        if (hash == 0 || !_entries.TryGetValue(hash, out var entry))
            throw new FileNotFoundException($"File '{path}' not found in PCAT archive.");

        Stream stream = new SubReadStream(_stream, entry.Offset, entry.CompressedLength);

        if (Encrypted)
        {
            if (_encryptionKey == null || _encryptionKey.Length == 0)
                throw new InvalidOperationException("Archive is encrypted but no encryption key was provided.");
            stream = PakHelper.CreateDecryptionStream(stream, _encryptionKey, entry.Offset);
        }

        return PakHelper.CreateDecompressionStream(stream, Compression);
    }

    public IEnumerable<PakEntry> GetAllEntries() => _entries.Values;

    private void ReadMetadata()
    {
        using var reader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);

        // MAGIC (4 bytes)
        ThrowHelper.ThrowIfInvalidMagic(reader.ReadInt32());

        Version = reader.ReadInt32(); // Version (4 bytes)
        Compression = (CompressionMethod)reader.ReadByte(); // Compression method (1 byte)
        PackedDate = DateTime.FromBinary(reader.ReadInt64()); // Packed date (8 bytes)
        Encrypted = reader.ReadBoolean();
        byte[] expectedSignature = reader.ReadBytes(32);

        if (Encrypted) VerifyKey(expectedSignature);

        ThrowHelper.ThrowIfInvalidPakVersion(Version);
        ReadToc(_stream);
    }

    private void VerifyKey(byte[] expectedSignature)
    {
        if (_encryptionKey.Length == 0)
        {
            throw new UnauthorizedAccessException("Archive is encrypted but no encryption key was provided.");
        }

        byte[] actualSignature = PakHelper.GetKeySignature(_encryptionKey);
        bool isKeyValid = CryptographicOperations.FixedTimeEquals(expectedSignature, actualSignature);

        if (!isKeyValid)
        {
            throw new CryptographicException("Invalid encryption key was provided.");
        }
    }

    private void ReadToc(Stream stream)
    {
        // 1. Přečteme 12bajtovou patičku z úplného konce
        stream.Seek(-12, SeekOrigin.End);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        int tocSize = reader.ReadInt32();
        long tocOffset = reader.ReadInt64();

        // 2. Skočíme na začátek TOC a načteme CELÝ blok dat do paměti
        stream.Seek(tocOffset, SeekOrigin.Begin);
        byte[] tocData = reader.ReadBytes(tocSize);

        // 3. Rozšifrujeme data (pokud máme klíč)
        if (Encrypted)
        {
            tocData = DecryptBytes(tocData, tocOffset);
        }

        // 4. Ze surových čistých bajtů postavíme naši tabulku
        DeserializeToc(tocData);
    }

    private void DeserializeToc(byte[] rawData)
    {
        using var ms = new MemoryStream(rawData);
        using var reader = new BinaryReader(ms, Encoding.UTF8);
        int count = reader.ReadUInt16();

        for (int i = 0; i < count; i++)
        {
            var entry = new PakEntry(
                hash: reader.ReadUInt64(),
                path: reader.ReadString(),
                offset: reader.ReadInt64(),
                size: reader.ReadInt32(),
                compressedLength: reader.ReadInt64()
            );

            _entries[entry.Hash] = entry;
        }
    }

    private byte[] DecryptBytes(byte[] encryptedData, long iv)
    {
        using var msIn = new MemoryStream(encryptedData);
        using var cryptoStream = PakHelper.CreateDecryptionStream(msIn, _encryptionKey!, iv);
        using var msOut = new MemoryStream();

        cryptoStream.CopyTo(msOut);
        
        return msOut.ToArray();
    }

    public readonly struct PakEntry(ulong hash, string path, long offset, int size, long compressedLength)
    {
        public readonly ulong Hash = hash;
        public readonly string Path = path;
        public readonly long Offset = offset;
        public readonly int UncompressedSize = size;
        public readonly long CompressedLength = compressedLength;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _entries.Clear();
            }

            _stream.Dispose();
            _disposed = true;
        }
    }

    ~PakArchive()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Neměňte tento kód. Kód pro vyčištění vložte do metody Dispose(bool disposing).
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
