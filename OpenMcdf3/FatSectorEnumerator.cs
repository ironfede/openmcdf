using System.Collections;

namespace OpenMcdf3;

/// <summary>
/// Enumerates the FAT sectors of a compound file.
/// </summary>
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

    /// <inheritdoc/>
    public void Dispose()
    {
        // IOContext is owned by a parent
    }

    /// <inheritdoc/>
    public Sector Current
    {
        get
        {
            if (current.IsEndOfChain)
                throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
            return current;
        }
    }

    /// <inheritdoc/>
    object IEnumerator.Current => Current;

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (start)
        {
            id = uint.MaxValue;
            start = false;
        }

        id++;

        if (id < ioContext.Header.FatSectorCount && id < Header.DifatArrayLength)
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
        long position = difatSector.Position + difatSectorElementIndex * sizeof(uint);
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

    /// <summary>
    /// Moves the enumerator to the specified sector.
    /// </summary>
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

    /// <inheritdoc/>
    public void Reset()
    {
        start = true;
        id = SectorType.EndOfChain;
        difatSectorId = ioContext.Header.FirstDifatSectorId;
        difatSectorElementIndex = 0;
        current = Sector.EndOfChain;
    }
}
