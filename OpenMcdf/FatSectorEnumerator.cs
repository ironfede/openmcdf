using System.Collections;

namespace OpenMcdf;

/// <summary>
/// Enumerates the FAT sectors of a compound file.
/// </summary>
internal sealed class FatSectorEnumerator : ContextBase, IEnumerator<Sector>
{
    private readonly DifatSectorEnumerator difatSectorEnumerator;
    private bool start = true;
    private uint index = uint.MaxValue;
    private Sector current = Sector.EndOfChain;

    public FatSectorEnumerator(RootContextSite rootContextSite)
        : base(rootContextSite)
    {
        difatSectorEnumerator = new(rootContextSite);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Context is owned by a parent
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

        if (index >= Context.Header.FatSectorCount)
        {
            this.index = uint.MaxValue;
            current = Sector.EndOfChain;
            return false;
        }

        if (index < Header.DifatArrayLength)
        {
            this.index = index;
            uint difatId = Context.Header.Difat[this.index];
            current = new(difatId, Context.SectorSize);
            return true;
        }

        if (index < this.index)
        {
            this.index = Header.DifatArrayLength;
        }

        uint difatChainIndex = GetDifatChainIndexAndDifatEntryIndex(index, out long difatElementIndex);
        if (!difatSectorEnumerator.MoveTo(difatChainIndex))
        {
            this.index = uint.MaxValue;
            current = Sector.EndOfChain;
            return false;
        }

        Sector difatSector = difatSectorEnumerator.Current;
        Context.Reader.Position = difatSector.Position + (difatElementIndex * sizeof(uint));
        uint id = Context.Reader.ReadUInt32();
        this.index = index;
        current = new Sector(id, Context.SectorSize);
        return true;
    }

    private uint GetDifatChainIndexAndDifatEntryIndex(uint index, out long difatElementIndex)
        => (uint)Math.DivRem(index - Header.DifatArrayLength, Context.DifatEntriesPerSector, out difatElementIndex);

    /// <inheritdoc/>
    public void Reset()
    {
        start = true;
        index = uint.MaxValue;
        current = Sector.EndOfChain;
        difatSectorEnumerator.Reset();
    }

    /// <summary>
    /// Extends the FAT by adding a new sector.
    /// </summary>
    /// <returns>The ID of the new sector that was added</returns>
    public uint Add()
    {
        // No FAT sectors are free, so add a new one
        Header header = Context.Header;
        uint nextIndex = Context.Header.FatSectorCount;
        Sector newFatSector = new(Context.SectorCount, Context.SectorSize);

        CfbBinaryWriter writer = Context.Writer;
        writer.Position = newFatSector.Position;
        writer.Write(SectorDataCache.GetFatEntryData(newFatSector.Length));
        Context.ExtendStreamLength(newFatSector.EndPosition);

        header.FatSectorCount++;

        index = nextIndex;
        current = newFatSector;

        if (nextIndex < Header.DifatArrayLength)
        {
            header.Difat[nextIndex] = newFatSector.Id;
        }
        else
        {
            uint difatSectorIndex = GetDifatChainIndexAndDifatEntryIndex(nextIndex, out long difatElementIndex);
            if (!difatSectorEnumerator.MoveTo(difatSectorIndex))
                difatSectorEnumerator.Add();

            Sector difatSector = difatSectorEnumerator.Current;
            writer.Position = difatSector.Position + difatElementIndex * sizeof(uint);
            writer.Write(newFatSector.Id);
        }

        Context.Fat[newFatSector.Id] = SectorType.Fat;
        return newFatSector.Id;
    }
}
