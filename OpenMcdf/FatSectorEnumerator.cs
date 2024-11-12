using System.Collections;
using System.Diagnostics;

namespace OpenMcdf;

/// <summary>
/// Enumerates the FAT sectors of a compound file.
/// </summary>
internal sealed class FatSectorEnumerator : IEnumerator<Sector>
{
    private readonly IOContext ioContext;
    private readonly DifatSectorEnumerator difatSectorEnumerator;
    private bool start = true;
    private uint index = uint.MaxValue;
    private Sector current = Sector.EndOfChain;

    public FatSectorEnumerator(IOContext ioContext)
    {
        this.ioContext = ioContext;
        difatSectorEnumerator = new(ioContext);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // IOContext is owned by a parent
        difatSectorEnumerator.Dispose();
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
        return MoveTo(nextIndex);
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

        if (index >= ioContext.Header.FatSectorCount)
        {
            this.index = uint.MaxValue;
            current = Sector.EndOfChain;
            return false;
        }

        if (index < Header.DifatArrayLength)
        {
            this.index = index;
            uint difatId = ioContext.Header.Difat[this.index];
            current = new(difatId, ioContext.SectorSize);
            return true;
        }

        if (index < this.index)
        {
            this.index = Header.DifatArrayLength;
        }

        uint difatSectorIndex = (uint)Math.DivRem(index - Header.DifatArrayLength, difatSectorEnumerator.DifatElementsPerSector, out long difatElementIndex);
        if (!difatSectorEnumerator.MoveTo(difatSectorIndex))
        {
            this.index = uint.MaxValue;
            current = Sector.EndOfChain;
            return false;
        }

        Sector difatSector = difatSectorEnumerator.Current;
        ioContext.Reader.Position = difatSector.Position + (difatElementIndex * sizeof(uint));
        uint id = ioContext.Reader.ReadUInt32();
        this.index = index;
        current = new Sector(id, ioContext.SectorSize);
        return true;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        start = true;
        index = uint.MaxValue;
        current = Sector.EndOfChain;
        difatSectorEnumerator.Reset();
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
        uint nextIndex = ioContext.Header.FatSectorCount;
        Sector newFatSector = new(ioContext.SectorCount, ioContext.SectorSize);

        CfbBinaryWriter writer = ioContext.Writer;
        writer.Position = newFatSector.Position;
        writer.Write(SectorDataCache.GetFatEntryData(newFatSector.Length));
        ioContext.ExtendStreamLength(newFatSector.EndPosition);

        header.FatSectorCount++;

        index = nextIndex;
        current = newFatSector;

        if (nextIndex < Header.DifatArrayLength)
        {
            header.Difat[nextIndex] = newFatSector.Id;
        }
        else
        {
            uint difatSectorIndex = (uint)Math.DivRem(nextIndex - Header.DifatArrayLength, difatSectorEnumerator.DifatElementsPerSector, out long difatElementIndex);
            if (!difatSectorEnumerator.MoveTo(difatSectorIndex))
                difatSectorEnumerator.Add();

            Sector difatSector = difatSectorEnumerator.Current;
            writer.Position = difatSector.Position + difatElementIndex * sizeof(uint);
            writer.Write(newFatSector.Id);
        }

        ioContext.Fat[newFatSector.Id] = SectorType.Fat;
        return newFatSector.Id;
    }
}
