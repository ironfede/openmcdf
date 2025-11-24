using System.Collections;

namespace OpenMcdf;

/// <summary>
/// Enumerates the <see cref="Sector"/>s in a DIFAT chain.
/// </summary>
internal class DifatSectorEnumerator : ContextBase, IEnumerator<Sector>
{
    bool start = true;
    uint index = uint.MaxValue;
    Sector current = Sector.EndOfChain;
    private uint difatSectorId = SectorType.EndOfChain;

    public DifatSectorEnumerator(RootContextSite rootContextSite)
        : base(rootContextSite)
    {
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public Sector Current => current.Id switch
    {
        SectorType.EndOfChain => throw new InvalidOperationException("Enumeration has not started. Call MoveNext."),
        _ => current,
    };

    /// <inheritdoc/>
    object IEnumerator.Current => Current;

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (start)
        {
            start = false;
            index = uint.MaxValue;
            difatSectorId = Context.Header.FirstDifatSectorId;
        }

        uint nextIndex = index + 1;
        if (difatSectorId == SectorType.EndOfChain)
        {
            index = uint.MaxValue;
            current = Sector.EndOfChain;
            difatSectorId = SectorType.EndOfChain;
            return false;
        }

        current = new(difatSectorId, Context.SectorSize);
        index = nextIndex;
        Context.Reader.Position = current.EndPosition - sizeof(uint);
        difatSectorId = Context.Reader.ReadUInt32();
        return true;
    }

    public bool MoveTo(uint index)
    {
        if (index >= Context.Header.DifatSectorCount)
            return false;

        if (start && !MoveNext())
            return false;

        if (index < this.index)
            Reset();

        while (start || this.index < index)
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
        current = Sector.EndOfChain;
        difatSectorId = SectorType.EndOfChain;
    }

    public void Add()
    {
        Sector newDifatSector = new(Context.SectorCount, Context.SectorSize);

        Header header = Context.Header;
        CfbBinaryWriter writer = Context.Writer;
        if (header.FirstDifatSectorId == SectorType.EndOfChain)
        {
            header.FirstDifatSectorId = newDifatSector.Id;
        }
        else
        {
            bool ok = MoveTo(header.DifatSectorCount - 1);
            if (!ok)
                throw new FileFormatException("The DIFAT sector count is invalid.");

            writer.Position = current.EndPosition - sizeof(uint);
            writer.Write(newDifatSector.Id);
        }

        writer.Position = newDifatSector.Position;
        writer.Write(SectorDataCache.GetFatEntryData(newDifatSector.Length));
        writer.Position = newDifatSector.EndPosition - sizeof(uint);
        writer.Write(SectorType.EndOfChain);

        Context.ExtendStreamLength(newDifatSector.EndPosition);
        header.DifatSectorCount++;

        Context.Fat[newDifatSector.Id] = SectorType.Difat;

        start = false;
        index = header.DifatSectorCount - 1;
        current = newDifatSector;
        difatSectorId = SectorType.EndOfChain;
    }
}
