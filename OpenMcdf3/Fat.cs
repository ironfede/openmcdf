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
    private readonly int DifatArrayElementCount;
    private readonly int FatElementsPerSector;
    private readonly int DifatElementsPerSector;
    private readonly byte[] sector;
    private bool isDirty;

    public Fat(IOContext ioContext)
    {
        this.ioContext = ioContext;
        FatElementsPerSector = ioContext.SectorSize / sizeof(uint);
        DifatElementsPerSector = FatElementsPerSector - 1;
        DifatArrayElementCount = Header.DifatArrayLength * FatElementsPerSector;
        fatSectorEnumerator = new(ioContext);
        sector = new byte[ioContext.SectorSize];
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
        if (key < DifatArrayElementCount)
            return (uint)Math.DivRem(key, FatElementsPerSector, out elementIndex);
        return (uint)Math.DivRem(key - DifatArrayElementCount, DifatElementsPerSector, out elementIndex);
    }

    public void Flush()
    {
        if (isDirty)
        {
            CfbBinaryWriter writer = ioContext.Writer;
            writer.Position = fatSectorEnumerator.Current.Position;
            writer.Write(sector);
            isDirty = false;
        }
    }

    bool TryMoveToSectorForKey(uint key, out long offset)
    {
        uint sectorId = GetSectorIndexAndElementOffset(key, out offset);
        if (fatSectorEnumerator.IsAt(sectorId))
            return true;

        Flush();

        bool ok = fatSectorEnumerator.MoveTo(sectorId);
        if (!ok)
            return false;

        CfbBinaryReader reader = ioContext.Reader;
        reader.Position = fatSectorEnumerator.Current.Position;
        reader.Read(sector);
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

        ReadOnlySpan<byte> slice = sector.AsSpan((int)elementIndex * sizeof(uint));
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
            uint newSectorId = fatSectorEnumerator.Add();

            // Next id must be free
            bool ok = fatEnumerator.MoveTo(newSectorId + 1);
            Debug.Assert(ok);
        }

        FatEntry entry = fatEnumerator.Current;
        ioContext.ExtendStreamLength(fatEnumerator.CurrentSector.EndPosition);
        this[entry.Index] = SectorType.EndOfChain;
        return entry.Index;
    }

    public IEnumerator<FatEntry> GetEnumerator() => new FatEnumerator(ioContext);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Trace(TextWriter writer)
    {
        using FatEnumerator fatEnumerator = new(ioContext);

        byte[] data = new byte[ioContext.SectorSize];

        Stream baseStream = ioContext.Reader.BaseStream;

        writer.WriteLine("Start of FAT =================");

        while (fatEnumerator.MoveNext())
        {
            FatEntry current = fatEnumerator.Current;
            if (current.IsFree)
            {
                writer.WriteLine($"{current}");
            }
            else
            {
                baseStream.Position = fatEnumerator.CurrentSector.Position;
                baseStream.ReadExactly(data, 0, data.Length);
                string hex = BitConverter.ToString(data);
                writer.WriteLine($"{current}: {hex}");
            }
        }

        writer.WriteLine("End of FAT ===================");
    }
}
