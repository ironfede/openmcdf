using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace OpenMcdf;

/// <summary>
/// Caches data for adding new sectors to the FAT.
/// </summary>
internal static class SectorDataCache
{
    static readonly ConcurrentDictionary<int, byte[]> FreeFatSectorData = new(1, 2);

    public static byte[] GetFatEntryData(int sectorSize)
    {
        if (!FreeFatSectorData.TryGetValue(sectorSize, out byte[]? data))
        {
            data = new byte[sectorSize];
            Span<uint> uintSpan = MemoryMarshal.Cast<byte, uint>((Span<byte>)data);
            uintSpan.Fill(SectorType.Free);
            FreeFatSectorData.TryAdd(sectorSize, data);
        }

        return data;
    }
}
