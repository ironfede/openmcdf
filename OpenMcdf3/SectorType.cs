namespace OpenMcdf3;

internal enum SectorType : uint
{
    Maximum = 0xFFFFFFFA,
    Difat = 0xFFFFFFFC, // Specifies a DIFAT sector in the FAT.
    Fat = 0xFFFFFFFD,
    EndOfChain = 0xFFFFFFFE,
    Free = 0xFFFFFFFF,
}
