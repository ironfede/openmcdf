using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics;

namespace OpenMcdf3;

/// <summary>
/// Encapsulates getting and setting entries in the FAT.
/// </summary>
internal sealed class Fat : IEnumerable<FatEntry>, IDisposable
{
    private readonly IOContext ioContext;
    private readonly FatSectorEnumerator fatSectorEnumerator;
    internal readonly int FatElementsPerSector;
    private readonly byte[] cachedSectorBuffer;
    Sector cachedSector = Sector.EndOfChain;
    private bool isDirty;

    public Fat(IOContext ioContext)
    {
        this.ioContext = ioContext;
        FatElementsPerSector = ioContext.SectorSize / sizeof(uint);
        fatSectorEnumerator = new(ioContext);
        cachedSectorBuffer = new byte[ioContext.SectorSize];
    }

    public void Dispose()
    {
        Flush();

        fatSectorEnumerator.Dispose();
    }

    public uint this[uint key]
    {
        get
        {
            if (!TryGetValue(key, out uint value))
                throw new KeyNotFoundException($"FAT index not found: {key}.");
            return value;

        }
        set
        {
            if (!TrySetValue(key, value))
                throw new KeyNotFoundException($"FAT index not found: {key}.");
        }
    }

    uint GetSectorIndexAndElementOffset(uint key, out long elementIndex)
    {
        uint index = (uint)Math.DivRem(key, FatElementsPerSector, out elementIndex);
        return index;
    }

    void CacheCurrentSector()
    {
        Sector current = fatSectorEnumerator.Current;
        if (cachedSector.Id == current.Id)
            return;

        Flush();

        CfbBinaryReader reader = ioContext.Reader;
        reader.Position = current.Position;
        reader.Read(cachedSectorBuffer);
        cachedSector = current;
    }

    public void Flush()
    {
        if (isDirty)
        {
            CfbBinaryWriter writer = ioContext.Writer;
            writer.Position = cachedSector.Position;
            writer.Write(cachedSectorBuffer);
            isDirty = false;
        }
    }

    bool TryMoveToSectorForKey(uint key, out long offset)
    {
        uint sectorId = GetSectorIndexAndElementOffset(key, out offset);
        bool ok = fatSectorEnumerator.MoveTo(sectorId);
        if (!ok)
            return false;

        CacheCurrentSector();
        return true;
    }

    public bool TryGetValue(uint key, out uint value)
    {
        ThrowHelper.ThrowIfSectorIdIsInvalid(key);

        bool ok = TryMoveToSectorForKey(key, out long elementIndex);
        if (!ok)
        {
            value = uint.MaxValue;
            return false;
        }

        ReadOnlySpan<byte> slice = cachedSectorBuffer.AsSpan((int)elementIndex * sizeof(uint));
        value = BinaryPrimitives.ReadUInt32LittleEndian(slice);
        return true;
    }

    public bool TrySetValue(uint key, uint value)
    {
        ThrowHelper.ThrowIfSectorIdIsInvalid(key);

        if (!TryMoveToSectorForKey(key, out long elementIndex))
            return false;

        Span<byte> slice = cachedSectorBuffer.AsSpan((int)elementIndex * sizeof(uint));
        BinaryPrimitives.WriteUInt32LittleEndian(slice, value);
        isDirty = true;
        return true;
    }

    /// <summary>
    /// Adds a new entry to the FAT.
    /// </summary>
    /// <returns>The index of the new entry in the FAT</returns>
    public uint Add(FatEnumerator fatEnumerator, uint startIndex)
    {
        ThrowHelper.ThrowIfSectorIdIsInvalid(startIndex);

        bool movedToFreeEntry = fatEnumerator.MoveTo(startIndex)
            && fatEnumerator.MoveNextFreeEntry();
        if (!movedToFreeEntry)
        {
            Flush();

            uint newSectorId = fatSectorEnumerator.Add();

            // Next id must be free
            bool ok = fatEnumerator.MoveTo(newSectorId);
            Debug.Assert(ok);

            ok = fatEnumerator.MoveNextFreeEntry();
            Debug.Assert(ok);

            CacheCurrentSector();
        }

        FatEntry entry = fatEnumerator.Current;
        Sector sector = new(entry.Index, ioContext.SectorSize);
        ioContext.ExtendStreamLength(sector.EndPosition);
        this[entry.Index] = SectorType.EndOfChain;
        return entry.Index;
    }

    public IEnumerator<FatEntry> GetEnumerator() => new FatEnumerator(ioContext);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void WriteTrace(TextWriter writer)
    {
        byte[] data = new byte[ioContext.SectorSize];

        Stream baseStream = ioContext.Reader.BaseStream;

        writer.WriteLine("Start of FAT =================");

        foreach (FatEntry entry in this)
        {
            Sector sector = new(entry.Index, ioContext.SectorSize);
            if (entry.IsFree)
            {
                writer.WriteLine($"{entry}");
            }
            else
            {
                baseStream.Position = sector.Position;
                baseStream.ReadExactly(data, 0, data.Length);
                string hex = BitConverter.ToString(data);
                writer.WriteLine($"{entry}: {hex}");
            }
        }

        writer.WriteLine("End of FAT ===================");
    }

    internal void Validate()
    {
        long fatSectorCount = 0;
        long difatSectorCount = 0;
        foreach (FatEntry entry in this)
        {
            Sector sector = new(entry.Index, ioContext.SectorSize);
            if (entry.Value <= SectorType.Maximum && sector.EndPosition > ioContext.Length)
                throw new FormatException($"FAT entry {entry} is beyond the end of the stream.");
            if (entry.Value == SectorType.Fat)
                fatSectorCount++;
            if (entry.Value == SectorType.Difat)
                difatSectorCount++;
        }

        if (ioContext.Header.FatSectorCount != fatSectorCount)
            throw new FormatException($"FAT sector count mismatch. Expected: {ioContext.Header.FatSectorCount} Actual: {fatSectorCount}.");
        if (ioContext.Header.DifatSectorCount != difatSectorCount)
            throw new FormatException($"DIFAT sector count mismatch: Expected: {ioContext.Header.DifatSectorCount} Actual: {difatSectorCount}.");
    }

    internal long GetFreeSectorCount() => this.Count(entry => entry.IsFree);
}
