using System.Collections;

namespace OpenMcdf3;

internal sealed class MiniFatSectorEnumerator : IEnumerator<MiniSector>
{
    private readonly IOContext ioContext;
    private readonly FatSectorChainEnumerator miniFatChain;
    bool start = true;
    MiniSector current = MiniSector.EndOfChain;

    public MiniFatSectorEnumerator(IOContext ioContext)
    {
        this.ioContext = ioContext;
        miniFatChain = new(ioContext, ioContext.Header.FirstMiniFatSectorId);
    }

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
            current = new(ioContext.Header.FirstMiniFatSectorId);
            start = false;
        }
        else if (!current.IsEndOfChain)
        {
            uint sectorId = GetNextMiniFatSectorId(current.Id);
            current = new(sectorId);
        }

        return !current.IsEndOfChain;
    }

    public bool MoveTo(uint id)
    {
        if (id == SectorType.EndOfChain)
            return false;

        while (start || current.Id < id)
        {
            if (!MoveNext())
                return false;
        }

        return true;
    }

    public void Reset()
    {
        start = true;
        current = MiniSector.EndOfChain;
    }

    public uint GetNextMiniFatSectorId(uint id)
    {
        if (id > SectorType.Maximum)
            throw new ArgumentException("Invalid sector ID");

        int elementLength = ioContext.Header.SectorSize / sizeof(uint);
        uint sectorId = (uint)Math.DivRem(id, elementLength, out long sectorOffset);
        if (!miniFatChain.MoveTo(sectorId))
            throw new ArgumentException("Invalid sector ID");

        long position = miniFatChain.Current.StartOffset + sectorOffset * sizeof(uint);
        ioContext.Reader.Seek(position);
        uint nextId = ioContext.Reader.ReadUInt32();
        return nextId;
    }

    public void Dispose()
    {
    }
}
