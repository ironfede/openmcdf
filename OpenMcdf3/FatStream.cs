namespace OpenMcdf3;

/// <summary>
/// Provides a <inheritdoc cref="Stream"/> for a stream object in a compound file./>
/// </summary>
internal class FatStream : Stream
{
    readonly IOContext ioContext;
    readonly FatChainEnumerator chain;
    long position;
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
            chain.Dispose();
            disposed = true;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    public override void Flush()
    {
        this.ThrowIfDisposed(disposed);
        ioContext.Writer!.Flush(); // TODO: Check validity
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
            chain.Extend(requiredChainLength);
        else if (value <= ChainCapacity - ioContext.SectorSize)
            chain.Shrink(requiredChainLength);

        if (DirectoryEntry.StartSectorId != chain.StartId || DirectoryEntry.StreamLength != value)
        {
            DirectoryEntry.StartSectorId = chain.StartId;
            DirectoryEntry.StreamLength = value;
            ioContext.Write(DirectoryEntry);
        }
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowHelper.ThrowIfStreamArgumentsAreInvalid(buffer, offset, count);

        this.ThrowIfNotWritable();

        if (count == 0)
            return;

        if (position + count > ChainCapacity)
            SetLength(position + count);

        uint chainIndex = (uint)Math.DivRem(position, ioContext.SectorSize, out long sectorOffset);
        if (!chain.MoveTo(chainIndex))
            throw new InvalidOperationException($"Failed to move to FAT chain index: {chainIndex}");

        CfbBinaryWriter writer = ioContext.Writer!;
        int writeCount = 0;
        do
        {
            Sector sector = chain.CurrentSector;
            writer.Position = sector.Position + sectorOffset;
            int remaining = count - writeCount;
            int localOffset = offset + writeCount;
            long writeLength = Math.Min(remaining, sector.Length - sectorOffset);
            writer.Write(buffer, localOffset, (int)writeLength);
            position += writeLength;
            writeCount += (int)writeLength;
            if (position > Length)
                DirectoryEntry.StreamLength = position;
            sectorOffset = 0;
            if (writeCount >= count)
                return;
        } while (chain.MoveNext());

        throw new InvalidOperationException($"End of FAT chain was reached");
    }
}
