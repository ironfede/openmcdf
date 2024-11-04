using System.Collections;
using System.Diagnostics;

namespace OpenMcdf3;

/// <summary>
/// Enumerates the <see cref="MiniSector"/>s in a Mini FAT sector chain.
/// </summary>
internal sealed class MiniFatChainEnumerator : IEnumerator<FatChainEntry>
{
    private readonly IOContext ioContext;
    private readonly MiniFatEnumerator miniFatEnumerator;
    private uint startId;
    private bool start = true;
    uint index = uint.MaxValue;
    private FatChainEntry current = FatChainEntry.Invalid;
    private long length = -1;

    public MiniFatChainEnumerator(IOContext ioContext, uint startSectorId)
    {
        this.ioContext = ioContext;
        this.startId = startSectorId;
        miniFatEnumerator = new(ioContext);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <summary>
    /// The index within the Mini FAT sector chain, or <see cref="uint.MaxValue"/> if the enumeration has not started.
    /// </summary>

    public uint StartId => startId;

    public uint Index => index;

    public MiniSector CurrentSector => new(Current.Value, ioContext.MiniSectorSize);

    /// <inheritdoc/>
    public FatChainEntry Current
    {
        get
        {
            if (current.IsFreeOrEndOfChain)
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
            index = 0;
            current = new(index, startId);
        }
        else if (!current.IsFreeOrEndOfChain)
        {
            uint sectorId = ioContext.MiniFat[current.Value];
            if (sectorId == SectorType.EndOfChain)
            {
                index = uint.MaxValue;
                current = FatChainEntry.Invalid;
                return false;
            }

            uint nextIndex = index + 1;
            if (nextIndex > SectorType.Maximum)
                throw new FormatException("Mini FAT chain is corrupt.");

            index = nextIndex;
            current = new(nextIndex, sectorId);
            return true;
        }

        if (current.IsFreeOrEndOfChain)
        {
            index = uint.MaxValue;
            current = FatChainEntry.Invalid;
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
        if (index < this.index)
            Reset();

        while (start || this.index < index)
        {
            if (!MoveNext())
                return false;
        }

        return true;
    }

    public long GetLength()
    {
        if (length == -1)
        {
            Reset();
            length = 0;
            while (MoveNext())
            {
                length++;
            }
        }

        return length;
    }

    public void Extend(uint requiredChainLength)
    {
        uint chainLength = (uint)GetLength();
        if (chainLength >= requiredChainLength)
            throw new ArgumentException("The chain is already longer than required.", nameof(requiredChainLength));

        if (startId == StreamId.NoStream)
        {
            startId = ioContext.MiniFat.Add(miniFatEnumerator, 0);
            chainLength = 1;
        }

        bool ok = MoveTo(chainLength - 1);
        Debug.Assert(ok);

        uint lastId = current.Value;
        ok = miniFatEnumerator.MoveTo(lastId);
        Debug.Assert(ok);
        while (chainLength < requiredChainLength)
        {
            uint id = ioContext.MiniFat.Add(miniFatEnumerator, lastId);
            ioContext.MiniFat[lastId] = id;
            lastId = id;
            chainLength++;
        }

#if DEBUG
        this.length = -1;
        this.length = GetLength();
        Debug.Assert(length == requiredChainLength);
#endif

        this.length = requiredChainLength;
    }

    public void Shrink(uint requiredChainLength)
    {
        uint chainLength = (uint)GetLength();
        if (chainLength <= requiredChainLength)
            throw new ArgumentException("The chain is already shorter than required.", nameof(requiredChainLength));

        Reset();

        uint lastId = current.Value;
        while (MoveNext())
        {
            if (lastId <= SectorType.Maximum)
            {
                if (index == requiredChainLength)
                    ioContext.MiniFat[lastId] = SectorType.EndOfChain;
                else if (index > requiredChainLength)
                    ioContext.MiniFat[lastId] = SectorType.Free;
            }

            lastId = current.Value;
        }

        if (lastId <= SectorType.Maximum)
            ioContext.MiniFat[lastId] = SectorType.Free;

        if (requiredChainLength == 0)
        {
            startId = StreamId.NoStream;
        }

#if DEBUG
        this.length = -1;
        this.length = GetLength();
        Debug.Assert(length == requiredChainLength);
#endif

        this.length = requiredChainLength;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        start = true;
        index = uint.MaxValue;
        current = FatChainEntry.Invalid;
    }

    public override string ToString() => $"Index: {index} Value {current}";
}
