using System.Diagnostics;

namespace OpenMcdf;

internal class TransactedStream : Stream
{
    readonly RootContext ioContext;
    readonly Stream originalStream;
    readonly Dictionary<uint, long> dirtySectorPositions = new();
    readonly byte[] buffer;

    public TransactedStream(RootContext ioContext, Stream originalStream, Stream overlayStream)
    {
        this.ioContext = ioContext;
        this.originalStream = originalStream;
        OverlayStream = overlayStream;
        buffer = new byte[ioContext.SectorSize];
    }

    protected override void Dispose(bool disposing)
    {
        // Original stream might be owned by the caller
        OverlayStream.Dispose();

        base.Dispose(disposing);
    }

    public Stream OverlayStream { get; }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => true;

    public override long Length => OverlayStream.Length;

    public override long Position { get => originalStream.Position; set => originalStream.Position = value; }

    public override void Flush() => OverlayStream.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowHelper.ThrowIfStreamArgumentsAreInvalid(buffer, offset, count);

        int read;
        int totalRead = 0;
        do
        {
            uint sectorId = (uint)Math.DivRem(originalStream.Position, ioContext.SectorSize, out long sectorOffset);
            int remainingFromSector = ioContext.SectorSize - (int)sectorOffset;
            int localCount = Math.Min(count - totalRead, remainingFromSector);

            if (dirtySectorPositions.TryGetValue(sectorId, out long overlayPosition))
            {
                OverlayStream.Position = overlayPosition + sectorOffset;
                read = OverlayStream.Read(buffer, offset + totalRead, localCount);
                originalStream.Seek(read, SeekOrigin.Current);
            }
            else
            {
                read = originalStream.Read(buffer, offset + totalRead, localCount);
            }

            if (read == 0)
                break;

            totalRead += read;
        } while (totalRead < count);

        return totalRead;
    }

    public override long Seek(long offset, SeekOrigin origin) => originalStream.Seek(offset, origin);

    public override void SetLength(long value) => throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowHelper.ThrowIfStreamArgumentsAreInvalid(buffer, offset, count);

        uint sectorId = (uint)Math.DivRem(originalStream.Position, ioContext.SectorSize, out long sectorOffset);
        int remainingFromSector = ioContext.SectorSize - (int)sectorOffset;
        int localCount = Math.Min(count, remainingFromSector);
        Debug.Assert(localCount == count);
        // TODO: Loop through the buffer and write to the overlay stream

        bool added = false;
        if (!dirtySectorPositions.TryGetValue(sectorId, out long overlayPosition))
        {
            overlayPosition = OverlayStream.Length;
            dirtySectorPositions.Add(sectorId, overlayPosition);
            added = true;
        }

        long originalPosition = originalStream.Position;
        if (added && localCount != ioContext.SectorSize && originalPosition < originalStream.Length)
        {
            // Copy the existing sector data
            originalStream.Position = originalPosition - sectorOffset;
            originalStream.ReadExactly(this.buffer);

            OverlayStream.Position = overlayPosition;
            OverlayStream.Write(this.buffer, 0, this.buffer.Length);
        }

        OverlayStream.Position = overlayPosition + sectorOffset;
        OverlayStream.Write(buffer, offset, localCount);
        if (OverlayStream.Length < overlayPosition + ioContext.SectorSize)
            OverlayStream.SetLength(overlayPosition + ioContext.SectorSize);
        originalStream.Position = originalPosition + localCount;
    }

    public void Commit()
    {
        foreach (KeyValuePair<uint, long> entry in dirtySectorPositions)
        {
            OverlayStream.Position = entry.Value;
            OverlayStream.ReadExactly(buffer);

            originalStream.Position = entry.Key * ioContext.SectorSize;
            originalStream.Write(buffer, 0, buffer.Length);
        }

        originalStream.Flush();
        dirtySectorPositions.Clear();
    }

    public void Revert()
    {
        dirtySectorPositions.Clear();
    }

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)

    public override int ReadByte() => this.ReadByteCore();

    public override int Read(Span<byte> buffer)
    {
        uint sectorId = (uint)Math.DivRem(originalStream.Position, ioContext.SectorSize, out long sectorOffset);
        int remainingFromSector = ioContext.SectorSize - (int)sectorOffset;
        int localCount = Math.Min(buffer.Length, remainingFromSector);
        Debug.Assert(localCount == buffer.Length);

        Span<byte> slice = buffer[..localCount];
        int read;
        if (dirtySectorPositions.TryGetValue(sectorId, out long overlayPosition))
        {
            OverlayStream.Position = overlayPosition + sectorOffset;
            read = OverlayStream.Read(slice);
            originalStream.Seek(read, SeekOrigin.Current);
        }
        else
        {
            read = originalStream.Read(slice);
        }

        return read;
    }

    public override void WriteByte(byte value) => this.WriteByteCore(value);

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        uint sectorId = (uint)Math.DivRem(originalStream.Position, ioContext.SectorSize, out long sectorOffset);
        int remainingFromSector = ioContext.SectorSize - (int)sectorOffset;
        int localCount = Math.Min(buffer.Length, remainingFromSector);
        Debug.Assert(localCount == buffer.Length);
        // TODO: Loop through the buffer and write to the overlay stream

        bool added = false;
        if (!dirtySectorPositions.TryGetValue(sectorId, out long overlayPosition))
        {
            overlayPosition = OverlayStream.Length;
            dirtySectorPositions.Add(sectorId, overlayPosition);
            added = true;
        }

        long originalPosition = originalStream.Position;
        if (added && localCount != ioContext.SectorSize && originalPosition < originalStream.Length)
        {
            // Copy the existing sector data
            originalStream.Position = originalPosition - sectorOffset;
            originalStream.ReadExactly(this.buffer);

            OverlayStream.Position = overlayPosition;
            OverlayStream.Write(this.buffer, 0, this.buffer.Length);
        }

        OverlayStream.Position = overlayPosition + sectorOffset;
        OverlayStream.Write(buffer);
        if (OverlayStream.Length < overlayPosition + ioContext.SectorSize)
            OverlayStream.SetLength(overlayPosition + ioContext.SectorSize);
        originalStream.Position = originalPosition + localCount;
    }

#endif
}