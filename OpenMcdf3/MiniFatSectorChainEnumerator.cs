using System.Collections;

namespace OpenMcdf3;

internal sealed class MiniFatSectorChainEnumerator : IEnumerator<MiniSector>
{
    private readonly MiniFatSectorEnumerator miniFatEnumerator;
    private readonly uint startId;
    private bool start = true;
    private MiniSector current = MiniSector.EndOfChain;

    public MiniFatSectorChainEnumerator(IOContext ioContext, uint startSectorId)
    {
        this.startId = startSectorId;
        miniFatEnumerator = new(ioContext);
    }

    public uint Index { get; private set; } = uint.MaxValue;

    public MiniSector Current
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
            current = new(startId);
            Index = 0;
            start = false;
        }
        else if (!current.IsEndOfChain)
        {
            uint sectorId = miniFatEnumerator.GetNextMiniFatSectorId(current.Id);
            current = new(sectorId);
            Index++;
        }

        if (current.IsEndOfChain)
        {
            current = MiniSector.EndOfChain;
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
        miniFatEnumerator.Reset();
        current = MiniSector.EndOfChain;
        Index = uint.MaxValue;
    }

    public void Dispose()
    {
        miniFatEnumerator.Dispose();
    }
}
