namespace OpenMcdf3;

/// <summary>
/// Provides a <see cref="Stream"/> for reading a <see cref="DirectoryEntry"/> from the mini FAT stream.
/// </summary>
internal sealed class MiniFatStream : Stream
{
    readonly IOContext ioContext;
    readonly MiniFatChainEnumerator miniChain;
    long position;
    bool disposed;

    internal MiniFatStream(IOContext ioContext, DirectoryEntry directoryEntry)
    {
        this.ioContext = ioContext;
        DirectoryEntry = directoryEntry;
        miniChain = new(ioContext, directoryEntry.StartSectorId);
    }

    internal DirectoryEntry DirectoryEntry { get; private set; }

    internal long ChainCapacity => ((Length + ioContext.MiniSectorSize - 1) / ioContext.MiniSectorSize) * ioContext.MiniSectorSize;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => ioContext.CanWrite;

    public override long Length => DirectoryEntry.StreamLength;

    public override long Position
    {
        get => position;
        set => Seek(value, SeekOrigin.Begin);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            miniChain.Dispose();
            disposed = true;
        }

        base.Dispose(disposing);
    }

    public override void Flush()
    {
        this.ThrowIfDisposed(disposed);

        ioContext.MiniStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowHelper.ThrowIfStreamArgumentsAreInvalid(buffer, offset, count);

        this.ThrowIfDisposed(disposed);

        if (count == 0)
            return 0;

        int maxCount = (int)Math.Min(Math.Max(Length - position, 0), int.MaxValue);
        if (maxCount == 0)
            return 0;

        uint chainIndex = (uint)Math.DivRem(position, ioContext.MiniSectorSize, out long sectorOffset);
        if (!miniChain.MoveTo(chainIndex))
            return 0;

        FatStream miniStream = ioContext.MiniStream;
        int realCount = Math.Min(count, maxCount);
        int readCount = 0;
        do
        {
            MiniSector miniSector = miniChain.CurrentSector;
            int remaining = realCount - readCount;
            long readLength = Math.Min(remaining, miniSector.Length - sectorOffset);
            miniStream.Position = miniSector.Position + sectorOffset;
            int localOffset = offset + readCount;
            int read = miniStream.Read(buffer, localOffset, (int)readLength);
            if (read == 0)
                return readCount;
            position += read;
            readCount += read;
            sectorOffset = 0;
            if (readCount >= realCount)
                return readCount;
        } while (miniChain.MoveNext());

        return readCount;
    }

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
                throw new ArgumentException(nameof(origin), "Invalid seek origin.");
        }

        return position;
    }

    public override void SetLength(long value)
    {
        if (value >= Header.MiniStreamCutoffSize)
            throw new ArgumentOutOfRangeException(nameof(value));

        this.ThrowIfDisposed(disposed);
        this.ThrowIfNotWritable();

        uint requiredChainLength = (uint)((value + ioContext.MiniSectorSize - 1) / ioContext.MiniSectorSize);
        if (value > ChainCapacity)
            miniChain.Extend(requiredChainLength);
        else if (value <= ChainCapacity - ioContext.MiniSectorSize)
            miniChain.Shrink(requiredChainLength);

        if (DirectoryEntry.StartSectorId != miniChain.StartId || DirectoryEntry.StreamLength != value)
        {
            DirectoryEntry.StartSectorId = miniChain.StartId;
            DirectoryEntry.StreamLength = value;
            ioContext.Write(DirectoryEntry);
        }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowHelper.ThrowIfStreamArgumentsAreInvalid(buffer, offset, count);

        this.ThrowIfDisposed(disposed);
        this.ThrowIfNotWritable();

        if (count == 0)
            return;

        if (position + count > ChainCapacity)
            SetLength(position + count);

        uint chainIndex = (uint)Math.DivRem(position, ioContext.MiniSectorSize, out long sectorOffset);
        if (!miniChain.MoveTo(chainIndex))
            throw new InvalidOperationException($"Failed to move to mini FAT chain index: {chainIndex}.");

        FatStream miniStream = ioContext.MiniStream;
        int writeCount = 0;
        do
        {
            MiniSector miniSector = miniChain.CurrentSector;
            long basePosition = miniSector.Position + sectorOffset;
            miniStream.Seek(basePosition, SeekOrigin.Begin);
            int remaining = count - writeCount;
            int localOffset = offset + writeCount;
            long writeLength = Math.Min(remaining, ioContext.MiniSectorSize - sectorOffset);
            miniStream.Write(buffer, localOffset, (int)writeLength);
            position += writeLength;
            writeCount += (int)writeLength;
            if (position > Length)
                DirectoryEntry.StreamLength = position;
            sectorOffset = 0;
            if (writeCount >= count)
                return;
        } while (miniChain.MoveNext());

        throw new InvalidOperationException($"End of mini FAT chain was reached.");
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER

    public override int ReadByte() => this.ReadByteCore();

    public override int Read(Span<byte> buffer)
    {
        this.ThrowIfDisposed(disposed);

        if (buffer.Length == 0)
            return 0;

        int maxCount = (int)Math.Min(Math.Max(Length - position, 0), int.MaxValue);
        if (maxCount == 0)
            return 0;

        uint chainIndex = (uint)Math.DivRem(position, ioContext.MiniSectorSize, out long sectorOffset);
        if (!miniChain.MoveTo(chainIndex))
            return 0;

        FatStream miniStream = ioContext.MiniStream;
        int realCount = Math.Min(buffer.Length, maxCount);
        int readCount = 0;
        do
        {
            MiniSector miniSector = miniChain.CurrentSector;
            int remaining = realCount - readCount;
            long readLength = Math.Min(remaining, miniSector.Length - sectorOffset);
            miniStream.Position = miniSector.Position + sectorOffset;
            int localOffset = readCount;
            Span<byte> slice = buffer.Slice(localOffset, (int)readLength);
            int read = miniStream.Read(slice);
            if (read == 0)
                return readCount;
            position += read;
            readCount += read;
            sectorOffset = 0;
            if (readCount >= realCount)
                return readCount;
        } while (miniChain.MoveNext());

        return readCount;
    }

    public override void WriteByte(byte value) => this.WriteByteCore(value);

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        this.ThrowIfDisposed(disposed);
        this.ThrowIfNotWritable();

        if (buffer.Length == 0)
            return;

        if (position + buffer.Length > ChainCapacity)
            SetLength(position + buffer.Length);

        uint chainIndex = (uint)Math.DivRem(position, ioContext.MiniSectorSize, out long sectorOffset);
        if (!miniChain.MoveTo(chainIndex))
            throw new InvalidOperationException($"Failed to move to mini FAT chain index: {chainIndex}.");

        FatStream miniStream = ioContext.MiniStream;
        int writeCount = 0;
        do
        {
            MiniSector miniSector = miniChain.CurrentSector;
            long basePosition = miniSector.Position + sectorOffset;
            miniStream.Seek(basePosition, SeekOrigin.Begin);
            int remaining = buffer.Length - writeCount;
            int localOffset = writeCount;
            long writeLength = Math.Min(remaining, ioContext.MiniSectorSize - sectorOffset);
            ReadOnlySpan<byte> slice = buffer.Slice(localOffset, (int)writeLength);
            miniStream.Write(slice);
            position += writeLength;
            writeCount += (int)writeLength;
            if (position > Length)
                DirectoryEntry.StreamLength = position;
            sectorOffset = 0;
            if (writeCount >= buffer.Length)
                return;
        } while (miniChain.MoveNext());

        throw new InvalidOperationException($"End of mini FAT chain was reached.");
    }

#endif
}
