using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace OpenMcdf;

/// <summary>
/// Caches data for adding new sectors to the FAT.
/// </summary>
internal static class SectorDataCache
{
    static readonly ConcurrentDictionary<int, byte[]> freeFatSectorData = new(1, 2);

    public static byte[] GetFatEntryData(int sectorSize)
    {
        if (!freeFatSectorData.TryGetValue(sectorSize, out byte[]? data))
        {
            data = new byte[sectorSize];
            Span<uint> uintSpan = MemoryMarshal.Cast<byte, uint>(data);
            uintSpan.Fill(SectorType.Free);
            freeFatSectorData.TryAdd(sectorSize, data);
        }

        return data;
    }
}
