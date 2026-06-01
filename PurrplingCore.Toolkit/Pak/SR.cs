namespace PurrplingCore.Toolkit.Pak;

internal static class SR
{
    public const string LocalFileHeaderCorrupt = "The local header is corrupt.";
    public const string SeekingNotSupported = "The stream does not support seeking.";
    public const string ReadingNotSupported = "The stream does not support reading.";
    public const string WritingNotSupported = "The stream does not support writing.";
    public const string HiddenStreamName = "Hidden stream";
    public const string SetLengthRequiresSeekingAndWriting = "SetLength requires seeking and writing capabilities.";
    public const string IO_SeekBeforeBegin = "Attempted to seek before the beginning of the stream.";
    public const string PakArchiveReadOnly = "PCAT archive is read-only!";
}

