using System.Collections;
using System.Diagnostics;

namespace OpenMcdf3;

/// <summary>
/// Enumerates the <see cref="Sector"/>s in a FAT sector chain.
/// </summary>
internal sealed class FatChainEnumerator : IEnumerator<FatChainEntry>
{
    private readonly IOContext ioContext;
    private readonly FatEnumerator fatEnumerator;
    private uint startId;
    private bool start = true;
    private uint index = uint.MaxValue;
    private FatChainEntry current = FatChainEntry.Invalid;
    private long length = -1;

    public FatChainEnumerator(IOContext ioContext, uint startSectorId)
    {
        this.ioContext = ioContext;
        startId = startSectorId;
        fatEnumerator = new(ioContext);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        fatEnumerator.Dispose();
    }

    public Sector CurrentSector => new(Current.Value, ioContext.SectorSize);

    /// <inheritdoc/>
    public FatChainEntry Current
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

    public bool IsAt(uint index) => !start && index == this.index;

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (start)
        {
            if (startId is SectorType.EndOfChain or SectorType.Free)
            {
                index = uint.MaxValue;
                current = FatChainEntry.Invalid;
                return false;
            }

            index = 0;
            current = new(index, startId);
            start = false;
            return true;
        }

        if (current.IsFreeOrEndOfChain || current == FatChainEntry.Invalid)
        {
            index = uint.MaxValue;
            current = FatChainEntry.Invalid;
            return false;
        }

        uint value = ioContext.Fat[current.Value];
        if (value is SectorType.EndOfChain)
        {
            index = uint.MaxValue;
            current = FatChainEntry.Invalid;
            return false;
        }

        index++;
        if (index > SectorType.Maximum)
        {
            // If the index is greater than the maximum, then the chain must contain a loop
            index = uint.MaxValue;
            current = FatChainEntry.Invalid;
            throw new IOException("FAT sector chain is corrupt");
        }

        current = new(index, value);
        return true;
    }

    /// <summary>
    /// Moves to the specified index within the FAT sector chain.
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

    /// <summary>
    /// Extends the chain by one
    /// </summary>
    /// <returns>The ID of the new sector</returns>
    public uint Extend()
    {
        return ExtendFrom(0);
    }

    /// <summary>
    /// Returns the ID of the first sector in the chain.
    /// </summary>
    public uint Extend(uint requiredChainLength)
    {
        uint chainLength = (uint)GetLength();
        if (chainLength >= requiredChainLength)
            throw new ArgumentException("The chain is already longer than required.", nameof(requiredChainLength));

        if (startId == StreamId.NoStream)
        {
            startId = ioContext.Fat.Add(fatEnumerator, 0);
            chainLength = 1;
        }

        bool ok = MoveTo(chainLength - 1);
        Debug.Assert(ok);

        uint lastId = current.Value;
        ok = fatEnumerator.MoveTo(lastId);
        Debug.Assert(ok);
        while (chainLength < requiredChainLength)
        {
            uint id = ioContext.Fat.Add(fatEnumerator, lastId);
            ioContext.Fat[lastId] = id;
            lastId = id;
            chainLength++;
        }

        length = requiredChainLength;
        return startId;
    }

    public uint ExtendFrom(uint hintId)
    {
        if (startId == SectorType.EndOfChain)
        {
            startId = ioContext.Fat.Add(fatEnumerator, hintId);
            return startId;
        }

        uint lastId = startId;
        while (MoveNext())
        {
            lastId = current.Value;
        }

        uint id = ioContext.Fat.Add(fatEnumerator, lastId);
        ioContext.Fat[lastId] = id;
        return id;
    }

    public uint Shrink(uint requiredChainLength)
    {
        uint chainLength = (uint)GetLength();
        if (chainLength <= requiredChainLength)
            throw new ArgumentException("The chain is already shorter than required.", nameof(requiredChainLength));

        Reset();

        uint lastId = current.Value;
        while (MoveNext())
        {
            if (lastId is not SectorType.EndOfChain and not SectorType.Free)
            {
                if (index == requiredChainLength)
                    ioContext.Fat[lastId] = SectorType.EndOfChain;
                else if (index > requiredChainLength)
                    ioContext.Fat[lastId] = SectorType.Free;
            }

            lastId = current.Value;
        }

        ioContext.Fat[lastId] = SectorType.Free;

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
    public void Reset() => Reset(startId);

    public void Reset(uint startSectorId)
    {
        startId = startSectorId;
        start = true;
        index = uint.MaxValue;
        current = FatChainEntry.Invalid;
    }

    public override string ToString() => $"{current}";
}
