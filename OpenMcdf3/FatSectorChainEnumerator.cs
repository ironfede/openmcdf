using System.Collections;

namespace OpenMcdf3;

internal sealed class FatSectorChainEnumerator : IEnumerator<Sector>
{
    private readonly FatSectorEnumerator fatEnumerator;
    private readonly IOContext ioContext;
    private readonly uint startId;
    private uint nextId;
    private Sector current;

    public FatSectorChainEnumerator(IOContext ioContext, uint startId)
    {
        fatEnumerator = new(ioContext);
        this.ioContext = ioContext;
        Index = SectorType.EndOfChain;
        this.startId = startId;
        this.nextId = SectorType.Free;
        this.current = new Sector(0, 0); // Initialize with a default value
    }

    public uint Index { get; private set; }

    public Sector Current => current;

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (nextId is SectorType.Free)
        {
            Index = 0;
            nextId = startId;
        }

        if (nextId is SectorType.EndOfChain)
        {
            Index = SectorType.EndOfChain;
            return false;
        }

        Index++;
        current = new Sector(nextId, ioContext.Header.SectorSize);
        nextId = fatEnumerator.GetNextFatSectorId(nextId);
        return true;
    }

    public void Reset()
    {
        Index = SectorType.EndOfChain;
        nextId = SectorType.Free;
        current = new Sector(0, 0); // Reset to default value
        fatEnumerator.Reset();
    }

    public void Dispose()
    {
        fatEnumerator.Dispose();
    }
}
