using System.Collections;

namespace OpenMcdf3;

/// <summary>
/// Enumerates the <see cref="Sector"/>s in a FAT sector chain.
/// </summary>
internal sealed class FatChainEnumerator : IEnumerator<Sector>
{
    private readonly IOContext ioContext;
    private readonly FatSectorEnumerator fatEnumerator;
    private readonly uint startId;
    private bool start = true;
    private Sector current = Sector.EndOfChain;

    public FatChainEnumerator(IOContext ioContext, uint startSectorId)
    {
        this.ioContext = ioContext;
        this.startId = startSectorId;
        fatEnumerator = new(ioContext);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        fatEnumerator.Dispose();
    }

    /// <summary>
    /// The index within the FAT sector chain, or <see cref="uint.MaxValue"/> if the enumeration has not started.
    /// </summary>
    public uint Index { get; private set; } = uint.MaxValue;

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
            current = new(startId, ioContext.Header.SectorSize);
            Index = 0;
            start = false;
        }
        else if (!current.IsEndOfChain)
        {
            uint sectorId = GetNextFatSectorId(current.Id);
            current = new(sectorId, ioContext.Header.SectorSize);
            Index++;
        }

        if (current.IsEndOfChain)
        {
            current = Sector.EndOfChain;
            Index = uint.MaxValue;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Moves to the specified index within the FAT sector chain.
    /// </summary>
    /// <param name="index"></param>
    /// <returns>true if the enumerator was successfully advanced to the given index</returns>
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

    /// <inheritdoc/>
    public void Reset()
    {
        fatEnumerator.Reset();
        start = true;
        current = Sector.EndOfChain;
        Index = uint.MaxValue;
    }

    /// <summary>
    /// Gets the next sector ID in the FAT chain.
    /// </summary>
    uint GetNextFatSectorId(uint id)
    {
        if (id > SectorType.Maximum)
            throw new ArgumentException("Invalid sector ID", nameof(id));

        int elementCount = ioContext.Header.SectorSize / sizeof(uint);
        uint sectorId = (uint)Math.DivRem(id, elementCount, out long sectorOffset);
        if (!fatEnumerator.MoveTo(sectorId))
            throw new ArgumentException("Invalid sector ID", nameof(id));

        long position = fatEnumerator.Current.Position + sectorOffset * sizeof(uint);
        ioContext.Reader.Seek(position);
        uint nextId = ioContext.Reader.ReadUInt32();
        return nextId;
    }
}
