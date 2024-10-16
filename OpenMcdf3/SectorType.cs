namespace OpenMcdf3;

internal static class SectorType
{
    public const uint Maximum = 0xFFFFFFFA;
    public const uint Difat = 0xFFFFFFFC; // Specifies a DIFAT sector in the FAT.
    public const uint Fat = 0xFFFFFFFD;
    public const uint EndOfChain = 0xFFFFFFFE;
    public const uint Free = 0xFFFFFFFF;
}
