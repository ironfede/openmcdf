using System.Collections;
using System.Diagnostics.SymbolStore;

namespace OpenMcdf3;

internal sealed class FatSectorEnumerator : IEnumerator<Sector>
{
    private readonly IOContext ioContext;
    private uint index = SectorType.EndOfChain;
    uint nextDifatSectorId = SectorType.EndOfChain;
    uint difatSectorElementIndex = SectorType.EndOfChain;

    public FatSectorEnumerator(IOContext ioContext)
    {
        this.ioContext = ioContext;
        this.index = SectorType.EndOfChain;
        this.nextDifatSectorId = ioContext.Header.FirstDifatSectorID;
        Current = Sector.EndOfChain;
    }

    public Sector Current { get; private set; }

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (index == SectorType.EndOfChain)
        {
            index = 0;
        }

        if (index < ioContext.Header.FatSectorCount && index < Header.DifatLength)
        {
            uint nextId = ioContext.Header.Difat[index];
            Current = new Sector(nextId, ioContext.Header.SectorSize);
            index++;
            return true;
        }

        if (nextDifatSectorId == SectorType.EndOfChain)
            return false;

        int difatElementCount = ioContext.Header.SectorSize / sizeof(uint) - 1;
        Sector difatSector = new(nextDifatSectorId, ioContext.Header.SectorSize);
        if (difatSectorElementIndex == difatElementCount)
        {
            long nextIdOffset = difatSector.EndOffset - sizeof(uint);
            ioContext.Reader.Seek(nextIdOffset);
            nextDifatSectorId = ioContext.Reader.ReadUInt32();
            difatSectorElementIndex = 0;
        }

        if (difatSectorElementIndex < difatElementCount)
        {
            long position = difatSector.StartOffset + difatSectorElementIndex * sizeof(uint);
            ioContext.Reader.Seek(position);
            uint nextId = ioContext.Reader.ReadUInt32();
            Current = new Sector(nextId, ioContext.Header.SectorSize);
            difatSectorElementIndex++;
            index++;
            return true;
        }

        return false;
    }

    public void Reset()
    {
        index = SectorType.EndOfChain;
        difatSectorElementIndex = SectorType.EndOfChain;
        Current = Sector.EndOfChain;
    }

    public uint GetNextFatSectorId(uint id)
    {
        int elementLength = ioContext.Header.SectorSize / sizeof(uint);
        int sectorId = (int)Math.DivRem(id, elementLength, out long sectorOffset);
        while (index == SectorType.EndOfChain || index - 1 < sectorId)
        {
            if (!MoveNext())
                return SectorType.EndOfChain;
        }

        long position = Current.StartOffset + sectorOffset * sizeof(uint);
        ioContext.Reader.Seek(position);
        uint nextId = ioContext.Reader.ReadUInt32();
        return nextId;
    }

    public void Dispose()
    {
    }
}
