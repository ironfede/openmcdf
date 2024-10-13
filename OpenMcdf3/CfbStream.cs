namespace OpenMcdf3;

public class CfbStream : Stream
{
    readonly IOContext ioContext;
    readonly long sectorLength;
    readonly DirectoryEntry directoryEntry;
    readonly List<Sector> sectorChain;
    long length;
    long position;

    internal CfbStream(IOContext ioContext, long sectorLength, DirectoryEntry directoryEntry)
    {
        this.ioContext = ioContext;
        this.sectorLength = sectorLength;
        this.directoryEntry = directoryEntry;
        length = directoryEntry.StreamLength;
        sectorChain = ioContext.EnumerateFatSectorChain(directoryEntry.StartSectorLocation).ToList();
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => length;

    public override long Position
    {
        get => position;
        set => position = value;
    }

    public override void Flush()
    {
        //rootStorage.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int sectorSkipCount = (int)Math.DivRem(position, sectorLength, out long sectorOffset);
        int maxCount = (int)Math.Min(Math.Max(length - position, 0), int.MaxValue);
        int realCount = Math.Min(count, maxCount);
        int readCount = 0;
        int remaining = realCount;
        foreach (Sector sector in sectorChain.Skip(sectorSkipCount))
        {
            long readLength = Math.Min(remaining, sector.Length - sectorOffset);
            ioContext.Reader.Seek(sector.StartOffset + sectorOffset);
            int read = ioContext.Reader.Read(buffer, offset, (int)readLength);
            if (read == 0)
                return 0;
            position += read;
            readCount += read;
            if (readCount >= realCount)
                return readCount;
        }

        return readCount;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        position = offset;
        return position;
    }

    public override void SetLength(long value)
    {
        length = value;
    }

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
