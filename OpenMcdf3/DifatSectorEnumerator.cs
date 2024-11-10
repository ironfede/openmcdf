using System.Collections;
using System.Diagnostics;

namespace OpenMcdf3;

internal class DifatSectorEnumerator : IEnumerator<Sector>
{
    private readonly IOContext ioContext;
    public readonly uint DifatElementsPerSector;
    bool start = true;
    uint index = uint.MaxValue;
    Sector current = Sector.EndOfChain;
    private uint difatSectorId = SectorType.EndOfChain;

    public DifatSectorEnumerator(IOContext ioContext)
    {
        this.ioContext = ioContext;
        DifatElementsPerSector = (uint)((ioContext.SectorSize / sizeof(uint)) - 1);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // IOContext is owned by parent
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
            difatSectorId = ioContext.Header.FirstDifatSectorId;
        }

        uint nextIndex = index + 1;
        if (difatSectorId == SectorType.EndOfChain)
        {
            index = uint.MaxValue;
            current = Sector.EndOfChain;
            difatSectorId = SectorType.EndOfChain;
            return false;
        }

        current = new(difatSectorId, ioContext.SectorSize);
        index = nextIndex;
        ioContext.Reader.Position = current.EndPosition - sizeof(uint);
        difatSectorId = ioContext.Reader.ReadUInt32();
        return true;
    }

    public bool MoveTo(uint index)
    {
        if (index >= ioContext.Header.DifatSectorCount)
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
        Sector newDifatSector = new(ioContext.SectorCount, ioContext.SectorSize);

        Header header = ioContext.Header;
        CfbBinaryWriter writer = ioContext.Writer;
        if (header.FirstDifatSectorId == SectorType.EndOfChain)
        {
            header.FirstDifatSectorId = newDifatSector.Id;
        }
        else
        {
            bool ok = MoveTo(header.DifatSectorCount - 1);
            if (!ok)
                throw new InvalidOperationException("Failed to move to last DIFAT sector.");

            writer.Position = current.EndPosition - sizeof(uint);
            writer.Write(newDifatSector.Id);
        }

        writer.Position = newDifatSector.Position;
        writer.Write(SectorDataCache.GetFatEntryData(newDifatSector.Length));
        writer.Position = newDifatSector.EndPosition - sizeof(uint);
        writer.Write(SectorType.EndOfChain);

        ioContext.ExtendStreamLength(newDifatSector.EndPosition);
        header.DifatSectorCount++;

        ioContext.Fat[newDifatSector.Id] = SectorType.Difat;

        start = false;
        index = header.DifatSectorCount - 1;
        current = newDifatSector;
        difatSectorId = SectorType.EndOfChain;
    }
}
