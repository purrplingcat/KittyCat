using K4os.Compression.LZ4.Streams;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace PurrplingCore.Toolkit.Pak;

internal static class PakHelper
{
    public static Stream CreateCompressionStream(Stream destination, CompressionMethod method) => method switch
    {
        CompressionMethod.None => new ShieldStream(destination, leaveOpen: true),
        CompressionMethod.Deflate => new DeflateStream(destination, CompressionLevel.Optimal, leaveOpen: true),
        CompressionMethod.LZ4 => LZ4Stream.Encode(destination, leaveOpen: true),
        //CompressionMethod.Zstd => new CompressionStream(destination, level: 3, leaveOpen: true),
        CompressionMethod.Brotli => new BrotliStream(destination, CompressionLevel.Optimal, leaveOpen: true),
        _ => throw new NotSupportedException()
    };

    public static Stream CreateDecompressionStream(Stream source, CompressionMethod method) => method switch
    {
        CompressionMethod.None => new ShieldStream(source),
        CompressionMethod.Deflate => new DeflateStream(source, CompressionMode.Decompress),
        CompressionMethod.LZ4 => LZ4Stream.Decode(source, leaveOpen: true),
        //CompressionMethod.Zstd => new DecompressionStream(source, leaveOpen: true),
        CompressionMethod.Brotli => new BrotliStream(source, CompressionMode.Decompress),
        _ => throw new NotSupportedException()
    };

    public static Stream CreateDecryptionStream(Stream source, byte[] key, long offset)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;

        byte[] iv = new byte[16];
        BinaryPrimitives.WriteInt64BigEndian(iv, offset);

        var decryptor = aes.CreateDecryptor(aes.Key, iv);
        return new CryptoStream(source, decryptor, CryptoStreamMode.Read);
    }

    public static CryptoStream CreateEncryptionStream(Stream destination, byte[] key, long offset)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;

        byte[] iv = new byte[16];
        BinaryPrimitives.WriteInt64BigEndian(iv, offset);

        var encryptor = aes.CreateEncryptor(aes.Key, iv);
        return new CryptoStream(destination, encryptor, CryptoStreamMode.Write, leaveOpen: true);
    }

    public static byte[] EncryptBytes(byte[] data, byte[] key, long iv)
    {
        using var ms = new MemoryStream();
        using var cryptoStream = CreateEncryptionStream(ms, key, iv);
        cryptoStream.Write(data);
        cryptoStream.FlushFinalBlock();

        return ms.ToArray();
    }

    public static byte[] GetKeySignature(byte[] key)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes("KittyCat-Engine-Key"));
    }
}

internal class ShieldStream(Stream baseStream, bool leaveOpen = false) : Stream
{
    private readonly Stream _baseStream = baseStream;
    private readonly bool _leaveOpen = leaveOpen;
    private bool _disposed;

    public override bool CanRead => !_disposed && _baseStream.CanRead;
    public override bool CanSeek => !_disposed && _baseStream.CanSeek;
    public override bool CanWrite => !_disposed && _baseStream.CanWrite;

    public override long Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(ShieldStream));
            return _baseStream.Length;
        }
    }
    public override long Position
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(ShieldStream));
            return _baseStream.Position;
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(ShieldStream));
            _baseStream.Position = value;
        }
    }

    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShieldStream));
        _baseStream.Flush();
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShieldStream));
        return _baseStream.Read(buffer, offset, count);
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShieldStream));
        _baseStream.Write(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShieldStream));
        return _baseStream.Seek(offset, origin);
    }
    public override void SetLength(long value)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ShieldStream));
        _baseStream.SetLength(value);
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing && !_leaveOpen)
        {
            _baseStream.Dispose();
        }

        _disposed = true;
    }
}
