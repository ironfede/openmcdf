namespace OpenMcdf3;

public class CfbStream : Stream
{
    readonly RootStorage rootStorage;
    readonly long sectorLength;
    private readonly DirectoryEntry directoryEntry;
    long length;
    long position;

    internal CfbStream(RootStorage rootStorage, long sectorLength, DirectoryEntry directoryEntry)
    {
        this.rootStorage = rootStorage;
        this.sectorLength = sectorLength;
        this.directoryEntry = directoryEntry;
        length = directoryEntry.StreamLength;
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
        foreach (Sector sector in rootStorage.EnumerateFatSectorChain(directoryEntry.StartSectorLocation).Skip(sectorSkipCount))
        {
            long readLength = Math.Min(remaining, sector.Length - sectorOffset);
            rootStorage.Reader.Seek(sector.StartOffset + sectorOffset);
            int read = rootStorage.Reader.Read(buffer, offset, (int)readLength);
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
