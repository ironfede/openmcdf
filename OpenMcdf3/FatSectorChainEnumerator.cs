using System.Collections;

namespace OpenMcdf3;

/// <summary>
/// Enumerates the <see cref="Sector"/>s in a FAT sector chain.
/// </summary>
internal sealed class FatSectorChainEnumerator : IEnumerator<Sector>
{
    private readonly IOContext ioContext;
    private readonly FatSectorEnumerator fatEnumerator;
    private readonly uint startId;
    private bool start = true;
    private Sector current = Sector.EndOfChain;

    public FatSectorChainEnumerator(IOContext ioContext, uint startSectorId)
    {
        this.ioContext = ioContext;
        this.startId = startSectorId;
        fatEnumerator = new(ioContext);
    }

    public void Dispose()
    {
        fatEnumerator.Dispose();
    }

    /// <summary>
    /// The index within the FAT sector chain, or <see cref="uint.MaxValue"/> if the enumeration has not started.
    /// </summary>
    public uint Index { get; private set; } = uint.MaxValue;

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
            uint sectorId = fatEnumerator.GetNextFatSectorId(current.Id);
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
}
