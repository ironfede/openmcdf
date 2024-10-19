namespace OpenMcdf3;

/// <summary>
/// Defines the types of sectors in a compound file.
/// </summary>
internal static class SectorType
{
    public const uint Maximum = 0xFFFFFFFA;
    public const uint Difat = 0xFFFFFFFC; // Specifies a DIFAT sector in the FAT.
    public const uint Fat = 0xFFFFFFFD;
    public const uint EndOfChain = 0xFFFFFFFE;
    public const uint Free = 0xFFFFFFFF;

    public static bool IsFreeOrEndOfChain(uint value) => value is Free or EndOfChain;
}
