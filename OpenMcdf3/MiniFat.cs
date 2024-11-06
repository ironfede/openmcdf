using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics;

namespace OpenMcdf3;

/// <summary>
/// Encapsulates getting and setting entries in the mini FAT.
/// </summary>
internal sealed class MiniFat : IEnumerable<FatEntry>, IDisposable
{
    private readonly IOContext ioContext;
    private readonly FatChainEnumerator fatChainEnumerator;
    private readonly int ElementsPerSector;
    private readonly byte[] sector;
    private bool isDirty;

    public MiniFat(IOContext ioContext)
    {
        this.ioContext = ioContext;
        ElementsPerSector = ioContext.SectorSize / sizeof(uint);
        fatChainEnumerator = new(ioContext, ioContext.Header.FirstMiniFatSectorId);
        sector = new byte[ioContext.SectorSize];
    }

    public void Dispose()
    {
        Flush();

        fatChainEnumerator.Dispose();
    }

    public void Flush()
    {
        if (isDirty)
        {
            CfbBinaryWriter writer = ioContext.Writer;
            writer.Position = fatChainEnumerator.CurrentSector.Position;
            writer.Write(sector);
            isDirty = false;
        }
    }

    public IEnumerator<FatEntry> GetEnumerator() => new MiniFatEnumerator(ioContext);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public uint this[uint key]
    {
        get
        {
            if (!TryGetValue(key, out uint value))
                throw new KeyNotFoundException($"Mini FAT index not found: {key}.");
            return value;
        }
        set
        {
            if (!TrySetValue(key, value))
                throw new KeyNotFoundException($"Mini FAT index not found: {key}.");
        }
    }

    bool TryMoveToSectorForKey(uint key, out long elementIndex)
    {
        uint fatChain = (uint)Math.DivRem(key, ElementsPerSector, out elementIndex);
        if (fatChainEnumerator.IsAt(fatChain))
            return true;

        Flush();

        bool ok = fatChainEnumerator.MoveTo(fatChain);
        if (!ok)
            return false;

        CfbBinaryReader reader = ioContext.Reader;
        reader.Position = fatChainEnumerator.CurrentSector.Position;
        reader.Read(sector);
        return true;
    }

    public bool TryGetValue(uint key, out uint value)
    {
        ThrowHelper.ThrowIfSectorIdIsInvalid(key);

        if (!TryMoveToSectorForKey(key, out long elementIndex))
        {
            value = uint.MaxValue;
            return false;
        }

        Span<byte> slice = sector.AsSpan((int)elementIndex * sizeof(uint));
        value = BinaryPrimitives.ReadUInt32LittleEndian(slice);
        return true;
    }

    public bool TrySetValue(uint key, uint value)
    {
        ThrowHelper.ThrowIfSectorIdIsInvalid(key);

        if (!TryMoveToSectorForKey(key, out long elementIndex))
            return false;

        Span<byte> slice = sector.AsSpan((int)elementIndex * sizeof(uint));
        BinaryPrimitives.WriteUInt32LittleEndian(slice, value);
        isDirty = true;
        return true;
    }

    public uint Add(MiniFatEnumerator miniFatEnumerator, uint startIndex)
    {
        ThrowHelper.ThrowIfSectorIdIsInvalid(startIndex);

        bool movedToFreeEntry = miniFatEnumerator.MoveTo(startIndex) && miniFatEnumerator.MoveNextFreeEntry();
        if (!movedToFreeEntry)
        {
            uint newSectorIndex = fatChainEnumerator.Extend();
            Sector sector = new(newSectorIndex, ioContext.SectorSize);
            CfbBinaryWriter writer = ioContext.Writer;
            writer.Position = sector.Position;
            writer.Write(SectorDataCache.GetFatEntryData(sector.Length));

            if (ioContext.Header.FirstMiniFatSectorId == SectorType.EndOfChain)
                ioContext.Header.FirstMiniFatSectorId = newSectorIndex;

            miniFatEnumerator.Reset(); // TODO: Jump closer to the new sector

            bool ok = miniFatEnumerator.MoveNextFreeEntry();
            Debug.Assert(ok, "No free mini FAT entries found.");
        }

        FatEntry entry = miniFatEnumerator.Current;
        this[entry.Index] = SectorType.EndOfChain;

        Debug.Assert(entry.IsFree);
        MiniSector miniSector = new(entry.Index, ioContext.MiniSectorSize);
        if (ioContext.MiniStream.Length < miniSector.EndPosition)
            ioContext.MiniStream.SetLength(miniSector.EndPosition);

        return entry.Index;
    }

    internal void Trace(TextWriter writer)
    {
        using MiniFatEnumerator miniFatEnumerator = new(ioContext);

        writer.WriteLine("Start of Mini FAT ============");
        while (miniFatEnumerator.MoveNext())
            writer.WriteLine($"{miniFatEnumerator.Current}");
        writer.WriteLine("End of Mini FAT ==============");
    }
}
