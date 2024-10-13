using System.Collections;

namespace OpenMcdf3;

internal class ChainIterator : IEnumerator<Sector>
{
    readonly IOContext ioContext;
    private uint nextId;

    public ChainIterator(IOContext ioContext)
    {
        this.ioContext = ioContext;
        this.nextId = ioContext.Header.FirstDifatSectorID;
        Current = default;
    }

    public Sector Current { get; private set; }

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (nextId == (uint)SectorType.EndOfChain)
            return false;

        Current = new Sector(nextId, ioContext.Header.SectorSize);
        long nextIdOffset = Current.EndOffset - sizeof(uint);
        ioContext.Reader.Seek(nextIdOffset);
        nextId = ioContext.Reader.ReadUInt32();
        return true;
    }

    public void Reset()
    {
        nextId = ioContext.Header.FirstDifatSectorID;
        Current = default;
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}

internal class ChainEnumerable<Sector> : IEnumerable<Sector>
{
    private readonly IOContext ioContext;

    public ChainEnumerable(IOContext ioContext)
    {
        this.ioContext = ioContext;
    }

    public IEnumerator<Sector> GetEnumerator() => (IEnumerator<Sector>)(new ChainIterator(ioContext));

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
