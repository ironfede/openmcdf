namespace OpenMcdf3;

/// <summary>
/// Provides a <inheritdoc cref="Stream"/> for a stream object in a compound file./>
/// </summary>
internal class FatStream : Stream
{
    readonly IOContext ioContext;
    readonly FatChainEnumerator chain;
    long position;
    bool isDirty;
    bool disposed;

    internal FatStream(IOContext ioContext, DirectoryEntry directoryEntry)
    {
        this.ioContext = ioContext;
        DirectoryEntry = directoryEntry;
        chain = new(ioContext, directoryEntry.StartSectorId);
    }

    /// <inheritdoc/>
    internal DirectoryEntry DirectoryEntry { get; private set; }

    internal long ChainCapacity => ((Length + ioContext.SectorSize - 1) / ioContext.SectorSize) * ioContext.SectorSize;

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => true;

    /// <inheritdoc/>
    public override bool CanWrite => ioContext.CanWrite;

    /// <inheritdoc/>
    public override long Length => DirectoryEntry.StreamLength;

    /// <inheritdoc/>
    public override long Position
    {
        get => position;
        set => Seek(value, SeekOrigin.Begin);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            Flush();

            chain.Dispose();
            disposed = true;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    public override void Flush()
    {
        this.ThrowIfDisposed(disposed);

        if (isDirty)
        {
            ioContext.DirectoryEntries.Write(DirectoryEntry);
            isDirty = false;
        }

        if (CanWrite)
            ioContext.Writer!.Flush();
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowHelper.ThrowIfStreamArgumentsAreInvalid(buffer, offset, count);

        this.ThrowIfDisposed(disposed);

        if (count == 0)
            return 0;

        int maxCount = (int)Math.Min(Math.Max(Length - position, 0), int.MaxValue);
        if (maxCount == 0)
            return 0;

        uint chainIndex = (uint)Math.DivRem(position, ioContext.SectorSize, out long sectorOffset);
        if (!chain.MoveTo(chainIndex))
            return 0;

        int realCount = Math.Min(count, maxCount);
        int readCount = 0;
        do
        {
            Sector sector = chain.CurrentSector;
            int remaining = realCount - readCount;
            long readLength = Math.Min(remaining, sector.Length - sectorOffset);
            ioContext.Reader.Position = sector.Position + sectorOffset;
            int localOffset = offset + readCount;
            int read = ioContext.Reader.Read(buffer, localOffset, (int)readLength);
            if (read == 0)
                return readCount;
            position += read;
            readCount += read;
            sectorOffset = 0;
            if (readCount >= realCount)
                return readCount;
        } while (chain.MoveNext());

        return readCount;
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        this.ThrowIfDisposed(disposed);

        switch (origin)
        {
            case SeekOrigin.Begin:
                if (offset < 0)
                    ThrowHelper.ThrowSeekBeforeOrigin();
                position = offset;
                break;

            case SeekOrigin.Current:
                if (position + offset < 0)
                    ThrowHelper.ThrowSeekBeforeOrigin();
                position += offset;
                break;

            case SeekOrigin.End:
                if (Length - offset < 0)
                    ThrowHelper.ThrowSeekBeforeOrigin();
                position = Length - offset;
                break;

            default:
                throw new ArgumentException(nameof(origin), "Invalid seek origin");
        }

        return position;
    }

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        this.ThrowIfNotWritable();

        uint requiredChainLength = (uint)((value + ioContext.SectorSize - 1) / ioContext.SectorSize);
        if (value > ChainCapacity)
            DirectoryEntry.StartSectorId = chain.Extend(requiredChainLength);
        else if (value <= ChainCapacity - ioContext.SectorSize)
            DirectoryEntry.StartSectorId = chain.Shrink(requiredChainLength);

        DirectoryEntry.StreamLength = value;
        isDirty = true;
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowHelper.ThrowIfStreamArgumentsAreInvalid(buffer, offset, count);

        this.ThrowIfDisposed(disposed);
        this.ThrowIfNotWritable();

        if (count == 0)
            return;

        uint chainIndex = (uint)Math.DivRem(position, ioContext.SectorSize, out long sectorOffset);

        CfbBinaryWriter writer = ioContext.Writer;
        int writeCount = 0;
        uint lastIndex = 0;
        for (; ; )
        {
            if (!chain.MoveTo(chainIndex))
                lastIndex = chain.ExtendFrom(lastIndex);

            Sector sector = chain.CurrentSector;
            writer.Position = sector.Position + sectorOffset;
            int remaining = count - writeCount;
            int localOffset = offset + writeCount;
            long writeLength = Math.Min(remaining, sector.Length - sectorOffset);
            writer.Write(buffer, localOffset, (int)writeLength);
            ioContext.ExtendStreamLength(sector.EndPosition);
            position += writeLength;
            writeCount += (int)writeLength;
            if (position > Length)
            {
                DirectoryEntry.StreamLength = position;
                isDirty = true;
            }
            sectorOffset = 0;
            if (writeCount >= count)
                return;

            chainIndex++;
        }

        throw new InvalidOperationException($"End of FAT chain was reached");
    }

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)

    public override int ReadByte() => this.ReadByteCore();

    public override int Read(Span<byte> buffer)
    {
        this.ThrowIfDisposed(disposed);

        if (buffer.Length == 0)
            return 0;

        int maxCount = (int)Math.Min(Math.Max(Length - position, 0), int.MaxValue);
        if (maxCount == 0)
            return 0;

        uint chainIndex = (uint)Math.DivRem(position, ioContext.SectorSize, out long sectorOffset);
        if (!chain.MoveTo(chainIndex))
            return 0;

        int realCount = Math.Min(buffer.Length, maxCount);
        int readCount = 0;
        do
        {
            Sector sector = chain.CurrentSector;
            int remaining = realCount - readCount;
            long readLength = Math.Min(remaining, sector.Length - sectorOffset);
            ioContext.Reader.Position = sector.Position + sectorOffset;
            int localOffset = readCount;
            Span<byte> slice = buffer.Slice(localOffset, (int)readLength);
            int read = ioContext.Reader.Read(slice);
            if (read == 0)
                return readCount;
            position += read;
            readCount += read;
            sectorOffset = 0;
            if (readCount >= realCount)
                return readCount;
        } while (chain.MoveNext());

        return readCount;
    }

    public override void WriteByte(byte value) => this.WriteByteCore(value);

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        this.ThrowIfDisposed(disposed);
        this.ThrowIfNotWritable();

        if (buffer.Length == 0)
            return;

        uint chainIndex = (uint)Math.DivRem(position, ioContext.SectorSize, out long sectorOffset);

        CfbBinaryWriter writer = ioContext.Writer;
        int writeCount = 0;
        uint lastIndex = 0;
        for (; ; )
        {
            if (!chain.MoveTo(chainIndex))
                lastIndex = chain.ExtendFrom(lastIndex);

            Sector sector = chain.CurrentSector;
            writer.Position = sector.Position + sectorOffset;
            int remaining = buffer.Length - writeCount;
            int localOffset = writeCount;
            long writeLength = Math.Min(remaining, sector.Length - sectorOffset);
            ReadOnlySpan<byte> slice = buffer.Slice(localOffset, (int)writeLength);
            writer.Write(slice);
            ioContext.ExtendStreamLength(sector.EndPosition);
            position += writeLength;
            writeCount += (int)writeLength;
            if (position > Length)
                DirectoryEntry.StreamLength = position;
            sectorOffset = 0;
            if (writeCount >= buffer.Length)
                return;

            chainIndex++;
        }

        throw new InvalidOperationException($"End of FAT chain was reached");
    }

#endif
}
