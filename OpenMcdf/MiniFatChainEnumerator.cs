using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace OpenMcdf;

/// <summary>
/// Enumerates the <see cref="MiniSector"/>s in a Mini FAT chain.
/// </summary>
internal sealed class MiniFatChainEnumerator : ContextBase, IEnumerator<uint>
{
    private readonly MiniFatEnumerator miniFatEnumerator;
    private uint startId;
    private bool start = true;
    uint index = uint.MaxValue;
    private uint current = uint.MaxValue;
    private long length = -1;

    public MiniFatChainEnumerator(RootContextSite rootContextSite, uint startSectorId)
        : base(rootContextSite)
    {
        startId = startSectorId;
        miniFatEnumerator = new(rootContextSite);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        miniFatEnumerator.Dispose();
    }

    /// <summary>
    /// The index within the Mini FAT sector chain, or <see cref="uint.MaxValue"/> if the enumeration has not started.
    /// </summary>

    public MiniSector CurrentSector => new(Current, Context.MiniSectorSize);

    /// <inheritdoc/>
    public uint Current
    {
        get
        {
            if (index == uint.MaxValue)
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
            current = startId;
        }
        else if (!SectorType.IsFreeOrEndOfChain(current))
        {
            uint sectorId = Context.MiniFat[current];
            if (sectorId == SectorType.EndOfChain)
            {
                index = uint.MaxValue;
                current = uint.MaxValue;
                return false;
            }

            uint nextIndex = index + 1;
            if (nextIndex > SectorType.Maximum)
                throw new FileFormatException("Mini FAT chain is corrupt.");

            index = nextIndex;
            current = sectorId;
            return true;
        }

        if (SectorType.IsFreeOrEndOfChain(current))
        {
            index = uint.MaxValue;
            current = uint.MaxValue;
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

    public uint Extend(uint requiredChainLength)
    {
        uint chainLength = (uint)GetLength();
        if (chainLength >= requiredChainLength)
            throw new ArgumentException("The chain is already longer than required.", nameof(requiredChainLength));

        if (startId == StreamId.NoStream)
        {
            startId = Context.MiniFat.Add(miniFatEnumerator, 0);
            chainLength = 1;
        }

        bool ok = MoveTo(chainLength - 1);
        Debug.Assert(ok);

        uint lastId = current;
        ok = miniFatEnumerator.MoveTo(lastId);
        Debug.Assert(ok);
        while (chainLength < requiredChainLength)
        {
            uint id = Context.MiniFat.Add(miniFatEnumerator, lastId);
            Context.MiniFat[lastId] = id;
            lastId = id;
            chainLength++;
        }

#if DEBUG
        this.length = -1;
        this.length = GetLength();
        Debug.Assert(length == requiredChainLength);
#endif

        length = requiredChainLength;
        return startId;
    }

    public uint Shrink(uint requiredChainLength)
    {
        uint chainLength = (uint)GetLength();
        if (chainLength <= requiredChainLength)
            throw new ArgumentException("The chain is already shorter than required.", nameof(requiredChainLength));

        Reset();

        uint lastId = current;
        while (MoveNext())
        {
            if (lastId <= SectorType.Maximum)
            {
                if (index == requiredChainLength)
                    Context.MiniFat[lastId] = SectorType.EndOfChain;
                else if (index > requiredChainLength)
                    Context.MiniFat[lastId] = SectorType.Free;
            }

            lastId = current;
        }

        if (lastId <= SectorType.Maximum)
            Context.MiniFat[lastId] = SectorType.Free;

        if (requiredChainLength == 0)
        {
            startId = StreamId.NoStream;
        }

#if DEBUG
        this.length = -1;
        this.length = GetLength();
        Debug.Assert(length == requiredChainLength);
#endif

        length = requiredChainLength;
        return startId;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        start = true;
        index = uint.MaxValue;
        current = uint.MaxValue;
    }

    [ExcludeFromCodeCoverage]
    public override string ToString() => $"Index: {index} Current: {current}";
}
