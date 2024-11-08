using System.Collections;
using System.Diagnostics;

namespace OpenMcdf3;

/// <summary>
/// Enumerates the FAT sectors of a compound file.
/// </summary>
internal sealed class FatSectorEnumerator : IEnumerator<Sector>
{
    private readonly IOContext ioContext;
    private readonly uint DifatElementsPerSector;
    private bool start = true;
    private uint index = uint.MaxValue;
    private uint difatSectorId;
    private Sector current = Sector.EndOfChain;

    public FatSectorEnumerator(IOContext ioContext)
    {
        this.ioContext = ioContext;
        DifatElementsPerSector = (uint)((ioContext.SectorSize / sizeof(uint)) - 1);
        difatSectorId = ioContext.Header.FirstDifatSectorId;
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
            if (current.Id == SectorType.EndOfChain)
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
            start = false;
            index = uint.MaxValue;
        }

        uint nextIndex = index + 1;
        if (nextIndex < ioContext.Header.FatSectorCount && nextIndex < Header.DifatArrayLength) // Include the free entries
        {
            uint id = ioContext.Header.Difat[nextIndex];
            index = nextIndex;
            current = new Sector(id, ioContext.SectorSize);
            return true;
        }

        if (difatSectorId == SectorType.EndOfChain)
        {
            index = uint.MaxValue;
            current = Sector.EndOfChain;
            return false;
        }

        Sector difatSector = new(difatSectorId, ioContext.SectorSize);
        uint elementIndex = (nextIndex - Header.DifatArrayLength) % DifatElementsPerSector;
        ioContext.Reader.Position = difatSector.Position + elementIndex * sizeof(uint);
        uint id2 = ioContext.Reader.ReadUInt32();

        index = nextIndex;
        current = new Sector(id2, ioContext.SectorSize);

        if (elementIndex == DifatElementsPerSector - 1)
        {
            ioContext.Reader.Position = difatSector.EndPosition - sizeof(uint);
            difatSectorId = ioContext.Reader.ReadUInt32();
        }

        return true;
    }

    public bool IsAt(uint index) => !start && index == this.index;

    /// <summary>
    /// Moves the enumerator to the specified sector.
    /// </summary>
    public bool MoveTo(uint index)
    {
        ThrowHelper.ThrowIfSectorIdIsInvalid(index);

        start = false;

        if (index == this.index)
            return true;

        if (index >= ioContext.Header.FatSectorCount + ioContext.Header.DifatSectorCount)
        {
            this.index = uint.MaxValue;
            current = Sector.EndOfChain;
            return false;
        }

        if (this.index < Header.DifatArrayLength || index < this.index)
        {
            // Jump as close as possible
            this.index = Math.Min(index, Header.DifatArrayLength - 1);
            uint id = ioContext.Header.Difat[this.index];
            current = new(id, ioContext.SectorSize);
            difatSectorId = ioContext.Header.FirstDifatSectorId;
        }

        while (this.index < index)
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
        index = uint.MaxValue;
        difatSectorId = ioContext.Header.FirstDifatSectorId;
        current = Sector.EndOfChain;
    }

    (uint lastIndex, Sector lastSector) MoveToEnd()
    {
        Reset();

        uint lastIndex = uint.MaxValue;
        Sector lastSector = Sector.EndOfChain;
        while (MoveNext())
        {
            lastIndex = index;
            lastSector = current;
        }

        return (lastIndex, lastSector);
    }

    /// <summary>
    /// Extends the FAT by adding a new sector.
    /// </summary>
    /// <returns>The ID of the new sector that was added</returns>
    public uint Add()
    {
        // No FAT sectors are free, so add a new one
        Header header = ioContext.Header;
        uint nextIndex = ioContext.Header.FatSectorCount + ioContext.Header.DifatSectorCount;
        uint lastIndex = nextIndex - 1;
        Sector newSector = new(ioContext.SectorCount, ioContext.SectorSize);

        CfbBinaryWriter writer = ioContext.Writer;
        writer.Position = newSector.Position;
        writer.Write(SectorDataCache.GetFatEntryData(newSector.Length));

        uint sectorType;
        if (nextIndex < Header.DifatArrayLength)
        {
            index = nextIndex;
            current = newSector;
            sectorType = SectorType.Fat;

            header.Difat[nextIndex] = newSector.Id;
            header.FatSectorCount++;

            writer.Position = newSector.Position;
            writer.Write(SectorDataCache.GetFatEntryData(newSector.Length));
        }
        else
        {
            bool ok = MoveTo(lastIndex);
            Debug.Assert(ok);

            Sector lastSector = current;
            writer.Position = lastSector.EndPosition - sizeof(uint);
            writer.Write(newSector.Id);

            index = nextIndex;
            current = newSector;
            difatSectorId = newSector.Id;
            sectorType = SectorType.Difat;

            writer.Position = newSector.Position;
            writer.Write(SectorDataCache.GetFatEntryData(newSector.Length));

            writer.Position = newSector.EndPosition - sizeof(uint);
            writer.Write(SectorType.EndOfChain);

            // Chain the sector
            if (header.FirstDifatSectorId == SectorType.EndOfChain)
                header.FirstDifatSectorId = newSector.Id;
            header.DifatSectorCount++;
        }

        ioContext.Fat[newSector.Id] = sectorType;
        return newSector.Id;
    }
}
