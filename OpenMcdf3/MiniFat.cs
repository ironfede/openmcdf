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

    internal int ElementsPerSector => ioContext.SectorSize / sizeof(uint);

    public MiniFat(IOContext ioContext)
    {
        this.ioContext = ioContext;
        fatChainEnumerator = new(ioContext, ioContext.Header.FirstMiniFatSectorId);
    }

    public void Dispose() => fatChainEnumerator.Dispose();

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
            ThrowHelper.ThrowIfSectorIdIsInvalid(key);

            uint fatSectorIndex = (uint)Math.DivRem(key, ElementsPerSector, out long elementIndex);
            if (!fatChainEnumerator.MoveTo(fatSectorIndex))
                throw new KeyNotFoundException($"Mini FAT index not found: {fatSectorIndex}.");

            CfbBinaryWriter writer = ioContext.Writer;
            writer.Position = fatChainEnumerator.CurrentSector.Position + elementIndex * sizeof(uint);
            writer.Write(value);
        }
    }

    public bool TryGetValue(uint key, out uint value)
    {
        ThrowHelper.ThrowIfSectorIdIsInvalid(key);

        uint fatSectorIndex = (uint)Math.DivRem(key, ElementsPerSector, out long elementIndex);
        bool ok = fatChainEnumerator.MoveTo(fatSectorIndex);
        if (!ok)
        {
            value = uint.MaxValue;
            return false;
        }

        CfbBinaryReader reader = ioContext.Reader;
        reader.Position = fatChainEnumerator.CurrentSector.Position + elementIndex * sizeof(uint);
        value = reader.ReadUInt32();
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
        miniFatEnumerator.Trace(writer);
    }
}
