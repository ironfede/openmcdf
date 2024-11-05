namespace OpenMcdf3;

/// <summary>
/// Encapsulates an entry in the File Allocation Table (FAT).
/// </summary>
internal record struct FatEntry(uint Index, uint Value)
{
    internal static readonly FatEntry Invalid = new(uint.MaxValue, uint.MaxValue);

    public readonly bool IsFree => Value == SectorType.Free;

    public override readonly string ToString() => $"#{Index}: {Value}";
}
