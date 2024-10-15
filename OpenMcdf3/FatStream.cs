namespace OpenMcdf3;

public class FatStream : Stream
{
    readonly IOContext ioContext;
    readonly FatSectorChainEnumerator chain;
    long length;
    long position;

    internal FatStream(IOContext ioContext, DirectoryEntry directoryEntry)
    {
        this.ioContext = ioContext;
        DirectoryEntry = directoryEntry;
        length = directoryEntry.StreamLength;
        chain = new(ioContext, directoryEntry.StartSectorLocation);
    }

    internal DirectoryEntry DirectoryEntry { get; private set; }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => length;

    public override long Position
    {
        get => position;
        set => position = value;
    }

    protected override void Dispose(bool disposing)
    {
        chain.Dispose();

        base.Dispose(disposing);
    }

    public override void Flush()
    {
        //rootStorage.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        uint chainIndex = (uint)Math.DivRem(position, ioContext.Header.SectorSize, out long sectorOffset);
        if (!chain.MoveTo(chainIndex))
            return 0;

        int maxCount = (int)Math.Min(Math.Max(length - position, 0), int.MaxValue);
        int realCount = Math.Min(count, maxCount);
        int readCount = 0;
        int remaining = realCount;
        do
        {
            Sector sector = chain.Current;
            long readLength = Math.Min(remaining, sector.Length - sectorOffset);
            ioContext.Reader.Seek(sector.StartOffset + sectorOffset);
            int read = ioContext.Reader.Read(buffer, offset, (int)readLength);
            if (read == 0)
                return 0;
            position += read;
            readCount += read;
            if (readCount >= realCount)
                return readCount;
        } while (chain.MoveNext());

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
