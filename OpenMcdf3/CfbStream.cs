namespace OpenMcdf3;

/// <summary>
/// Represents a stream in a compound file.
/// </summary>
public sealed class CfbStream : Stream
{
    private readonly IOContext ioContext;
    private readonly DirectoryEntry directoryEntry;
    private Stream stream;

    internal CfbStream(IOContext ioContext, DirectoryEntry directoryEntry)
    {
        this.ioContext = ioContext;
        this.directoryEntry = directoryEntry;
        stream = directoryEntry.StreamLength < Header.MiniStreamCutoffSize
            ? new MiniFatStream(ioContext, directoryEntry)
            : new FatStream(ioContext, directoryEntry);
    }

    protected override void Dispose(bool disposing)
    {
        stream.Dispose();

        base.Dispose(disposing);
    }

    public EntryInfo EntryInfo => directoryEntry.ToEntryInfo();

    public override bool CanRead => stream.CanRead;

    public override bool CanSeek => stream.CanSeek;

    public override bool CanWrite => stream.CanWrite;

    public override long Length => stream.Length;

    public override long Position { get => stream.Position; set => stream.Position = value; }

    public override void Flush() => stream.Flush();

    public override int Read(byte[] buffer, int offset, int count) => stream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);

    public override void SetLength(long value)
    {
        this.ThrowIfNotWritable();

        if (value >= Header.MiniStreamCutoffSize && stream is MiniFatStream miniStream)
        {
            long position = miniStream.Position;
            miniStream.Position = 0;

            DirectoryEntry newDirectoryEntry = directoryEntry.Clone();
            FatStream fatStream = new(ioContext, newDirectoryEntry);
            fatStream.SetLength(value); // Reserve enough space up front
            miniStream.CopyTo(fatStream);
            fatStream.Position = position;
            stream = fatStream;

            miniStream.SetLength(0);
            miniStream.Dispose();
        }
        else if (value < Header.MiniStreamCutoffSize && stream is FatStream fatStream)
        {
            long position = fatStream.Position;
            fatStream.Position = 0;

            DirectoryEntry newDirectoryEntry = directoryEntry.Clone();
            MiniFatStream miniFatStream = new(ioContext, newDirectoryEntry);
            fatStream.SetLength(value); // Truncate the stream
            fatStream.CopyTo(miniFatStream);
            miniFatStream.Position = position;
            stream = miniFatStream;

            fatStream.SetLength(0);
            fatStream.Dispose();
        }
        else
        {
            stream.SetLength(value);
        }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowHelper.ThrowIfStreamArgumentsAreInvalid(buffer, offset, count);

        this.ThrowIfNotWritable();

        long newPosition = Position + count;
        if (newPosition > stream.Length)
            SetLength(newPosition);

        stream.Write(buffer, offset, count);
    }

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)

    public override int Read(Span<byte> buffer) => stream.Read(buffer);

    public override int ReadByte() => this.ReadByteCore();

    public override void WriteByte(byte value) => this.WriteByteCore(value);

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        this.ThrowIfNotWritable();

        long newPosition = Position + buffer.Length;
        if (newPosition > stream.Length)
            SetLength(newPosition);

        stream.Write(buffer);
    }

#endif
}
