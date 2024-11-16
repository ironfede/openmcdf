namespace OpenMcdf;

/// <summary>
/// Represents a stream in a compound file.
/// </summary>
public sealed class CfbStream : Stream
{
    private readonly RootContextSite rootContextSite;
    private readonly DirectoryEntry directoryEntry;
    private Stream stream;
    private bool isDisposed;

    internal CfbStream(RootContextSite rootContextSite, DirectoryEntry directoryEntry, Storage parent)
    {
        this.rootContextSite = rootContextSite;
        this.directoryEntry = directoryEntry;
        Parent = parent;
        stream = directoryEntry.StreamLength < Header.MiniStreamCutoffSize
            ? new MiniFatStream(rootContextSite, directoryEntry)
            : new FatStream(rootContextSite, directoryEntry);
    }

    protected override void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            stream.Dispose();
            isDisposed = true;
        }

        base.Dispose(disposing);
    }

    public Storage Parent { get; }

    public EntryInfo EntryInfo
    {
        get
        {
            EntryInfo parentEntryInfo = Parent.EntryInfo;
            string path = $"{parentEntryInfo.Path}{parentEntryInfo.Name}";
            return directoryEntry.ToEntryInfo(path);
        }
    }

    public override bool CanRead => stream.CanRead;

    public override bool CanSeek => stream.CanSeek;

    public override bool CanWrite => stream.CanWrite;

    public override long Length => stream.Length;

    public override long Position { get => stream.Position; set => stream.Position = value; }

    public override void Flush()
    {
        this.ThrowIfDisposed(isDisposed);

        stream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        this.ThrowIfDisposed(isDisposed);

        return stream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        this.ThrowIfDisposed(isDisposed);

        return stream.Seek(offset, origin);
    }

    private void EnsureLengthToWrite(int count)
    {
        long newPosition = Position + count;
        if (newPosition > stream.Length)
            SetLength(newPosition);
    }

    public override void SetLength(long value)
    {
        this.ThrowIfDisposed(isDisposed);
        this.ThrowIfNotWritable();

        if (value >= Header.MiniStreamCutoffSize && stream is MiniFatStream miniStream)
        {
            long position = miniStream.Position;
            miniStream.Position = 0;

            DirectoryEntry newDirectoryEntry = directoryEntry.Clone();
            FatStream fatStream = new(rootContextSite, newDirectoryEntry);
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
            MiniFatStream miniFatStream = new(rootContextSite, newDirectoryEntry);
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

        this.ThrowIfDisposed(isDisposed);
        this.ThrowIfNotWritable();

        EnsureLengthToWrite(count);

        stream.Write(buffer, offset, count);
    }

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)

    public override int Read(Span<byte> buffer)
    {
        this.ThrowIfDisposed(isDisposed);

        return stream.Read(buffer);
    }

    public override int ReadByte() => this.ReadByteCore();

    public override void WriteByte(byte value) => this.WriteByteCore(value);

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        this.ThrowIfDisposed(isDisposed);
        this.ThrowIfNotWritable();

        EnsureLengthToWrite(buffer.Length);

        stream.Write(buffer);
    }

#endif
}
