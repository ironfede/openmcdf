using System.Collections;

namespace OpenMcdf3;

/// <summary>
/// Enumerates the <see cref="MiniSector"/>s from the Mini FAT.
/// </summary>
internal sealed class MiniFatEnumerator : IEnumerator<FatEntry>
{
    private readonly IOContext ioContext;
    private readonly FatChainEnumerator fatChainEnumerator;
    private bool start = true;
    private uint index = uint.MaxValue;
    private uint value = uint.MaxValue;

    public MiniFatEnumerator(IOContext ioContext)
    {
        fatChainEnumerator = new(ioContext, ioContext.Header.FirstMiniFatSectorId);
        this.ioContext = ioContext;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        fatChainEnumerator.Dispose();
    }

    public MiniSector CurrentSector
    {
        get
        {
            if (index == uint.MaxValue)
                throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
            return new(value, ioContext.MiniSectorSize);
        }
    }

    /// <inheritdoc/>
    public FatEntry Current
    {
        get
        {
            if (index == uint.MaxValue)
                throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
            return new(index, value);
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
            return MoveTo(0);
        }

        if (index >= SectorType.Maximum)
            return false;

        uint next = index + 1;
        return MoveTo(next);
    }

    public bool MoveTo(uint index)
    {
        ThrowHelper.ThrowIfSectorIdIsInvalid(index);

        if (this.index == index)
            return true;

        if (ioContext.MiniFat.TryGetValue(index, out value))
        {
            this.index = index;
            return true;
        }

        this.index = uint.MaxValue;
        return false;
    }

    public bool MoveNextFreeEntry()
    {
        while (MoveNext())
        {
            if (value == SectorType.Free)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        fatChainEnumerator.Reset(ioContext.Header.FirstMiniFatSectorId);
        start = true;
        index = uint.MaxValue;
        value = uint.MaxValue;
    }
}
