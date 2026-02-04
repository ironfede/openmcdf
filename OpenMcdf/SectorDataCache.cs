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
        return FreeFatSectorData.GetOrAdd(sectorSize, static size =>
        {
            byte[] data = new byte[size];
            Span<uint> uintSpan = MemoryMarshal.Cast<byte, uint>((Span<byte>)data);
            uintSpan.Fill(SectorType.Free);
            return data;
        });
    }
}
