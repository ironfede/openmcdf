using System.Collections;

namespace OpenMcdf3;

/// <summary>
/// Enumerates the <see cref="MiniSector"/>s in a Mini FAT sector chain.
/// </summary>
internal sealed class MiniFatSectorChainEnumerator : IEnumerator<MiniSector>
{
    private readonly MiniFatSectorEnumerator miniFatEnumerator;
    private readonly uint startId;
    private bool start = true;
    private MiniSector current = MiniSector.EndOfChain;

    public MiniFatSectorChainEnumerator(IOContext ioContext, uint startSectorId)
    {
        this.startId = startSectorId;
        miniFatEnumerator = new(ioContext);
    }

    /// <summary>
    /// The index within the Mini FAT sector chain, or <see cref="uint.MaxValue"/> if the enumeration has not started.
    /// </summary>
    public uint Index { get; private set; } = uint.MaxValue;

    /// <inheritdoc/>
    public MiniSector Current
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
            current = new(startId);
            Index = 0;
            start = false;
        }
        else if (!current.IsEndOfChain)
        {
            uint sectorId = miniFatEnumerator.GetNextMiniFatSectorId(current.Id);
            current = new(sectorId);
            Index++;
        }

        if (current.IsEndOfChain)
        {
            current = MiniSector.EndOfChain;
            Index = uint.MaxValue;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Moves to the specified index within the mini FAT sector chain.
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
        start = true;
        miniFatEnumerator.Reset();
        current = MiniSector.EndOfChain;
        Index = uint.MaxValue;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        miniFatEnumerator.Dispose();
    }
}
