using System.Collections;

namespace OpenMcdf3;

internal sealed class FatSectorEnumerator : IEnumerator<Sector>
{
    private readonly IOContext ioContext;
    private bool start = true;
    private uint id = SectorType.EndOfChain;
    private uint difatSectorId;
    private uint difatSectorElementIndex = 0;
    private Sector current = Sector.EndOfChain;

    public FatSectorEnumerator(IOContext ioContext)
    {
        this.ioContext = ioContext;
        this.difatSectorId = ioContext.Header.FirstDifatSectorId;
    }

    public void Dispose()
    {
        // IOContext is owned by a parent
    }

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
            id = uint.MaxValue;
            start = false;
        }

        id++;

        if (id < ioContext.Header.FatSectorCount && id < Header.DifatLength)
        {
            uint id = ioContext.Header.Difat[this.id];
            current = new Sector(id, ioContext.Header.SectorSize);
            return true;
        }

        if (difatSectorId == SectorType.EndOfChain)
        {
            current = Sector.EndOfChain;
            id = SectorType.EndOfChain;
            return false;
        }

        int difatElementCount = ioContext.Header.SectorSize / sizeof(uint) - 1;
        Sector difatSector = new(difatSectorId, ioContext.Header.SectorSize);
        long position = difatSector.StartOffset + difatSectorElementIndex * sizeof(uint);
        ioContext.Reader.Seek(position);
        uint sectorId = ioContext.Reader.ReadUInt32();
        current = new Sector(sectorId, ioContext.Header.SectorSize);
        difatSectorElementIndex++;
        id++;

        if (difatSectorElementIndex == difatElementCount)
        {
            difatSectorId = ioContext.Reader.ReadUInt32();
            difatSectorElementIndex = 0;
        }

        return true;
    }

    public bool MoveTo(uint sectorId)
    {
        if (sectorId < id)
            Reset();

        while (start || id < sectorId)
        {
            if (!MoveNext())
                return false;
        }

        return true;
    }

    public void Reset()
    {
        start = true;
        id = SectorType.EndOfChain;
        difatSectorId = ioContext.Header.FirstDifatSectorId;
        difatSectorElementIndex = 0;
        current = Sector.EndOfChain;
    }

    public uint GetNextFatSectorId(uint id)
    {
        if (id > SectorType.Maximum)
            throw new ArgumentException("Invalid sector ID");

        int elementCount = ioContext.Header.SectorSize / sizeof(uint);
        uint sectorId = (uint)Math.DivRem(id, elementCount, out long sectorOffset);
        if (!MoveTo(sectorId))
            throw new ArgumentException("Invalid sector ID");

        long position = Current.StartOffset + sectorOffset * sizeof(uint);
        ioContext.Reader.Seek(position);
        uint nextId = ioContext.Reader.ReadUInt32();
        return nextId;
    }
}
