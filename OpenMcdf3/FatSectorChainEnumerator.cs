using System.Collections;

namespace OpenMcdf3;

internal sealed class FatSectorChainEnumerator : IEnumerator<Sector>
{
    private readonly IOContext ioContext;
    private readonly FatSectorEnumerator fatEnumerator;
    private readonly uint startId;
    private bool start = true;
    private Sector current = Sector.EndOfChain;

    public FatSectorChainEnumerator(IOContext ioContext, uint startSectorId)
    {
        this.ioContext = ioContext;
        if (startSectorId is SectorType.EndOfChain)
            throw new ArgumentException("Invalid start sector ID", nameof(startSectorId));
        this.startId = startSectorId;
        fatEnumerator = new(ioContext);
    }

    public uint Index { get; private set; } = uint.MaxValue;

    public Sector Current
    {
        get
        {
            if (current.IsEndOfChain)
                throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
            return current;
        }
    }

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (start)
        {
            current = new(startId, ioContext.Header.SectorSize);
            Index = 0;
            start = false;
        }
        else if (!current.IsEndOfChain)
        {
            uint sectorId = fatEnumerator.GetNextFatSectorId(current.Id);
            current = new(sectorId, ioContext.Header.SectorSize);
            Index++;
        }

        if (current.IsEndOfChain)
        {
            current = Sector.EndOfChain;
            Index = uint.MaxValue;
            return false;
        }

        return true;
    }

    public bool MoveTo(uint index)
    {
        if (index < Index)
            Reset();

        while (start || Index < index)
        {
            if (!MoveNext())
                return false;
        }

        return true;
    }

    public void Reset()
    {
        start = true;
        fatEnumerator.Reset();
        current = Sector.EndOfChain;
        Index = uint.MaxValue;
    }

    public void Dispose()
    {
        fatEnumerator.Dispose();
    }
}
