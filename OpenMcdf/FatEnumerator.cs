using System.Collections;

namespace OpenMcdf;

/// <summary>
/// Enumerates the entries in a FAT.
/// </summary>
internal class FatEnumerator : IEnumerator<FatEntry>
{
    readonly Fat fat;
    bool start = true;
    uint index = uint.MaxValue;
    uint value = uint.MaxValue;

    public FatEnumerator(Fat fat)
    {
        this.fat = fat;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
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

        start = false;
        if (this.index == index)
            return true;

        if (fat.TryGetValue(index, out value))
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
                return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        start = true;
        index = uint.MaxValue;
        value = uint.MaxValue;
    }

    public override string ToString() => $"{Current}";
}
