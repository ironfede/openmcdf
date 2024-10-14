namespace OpenMcdf3;

internal record struct Sector(uint Id, int Length)
{
    public const int MiniSectorSize = 64;

    public static readonly Sector EndOfChain = new(SectorType.EndOfChain, 0);

    /// <summary>
    /// Compound File Binary File Format only specifies that ENDOFCHAIN ends the DIFAT chain
    /// but some implementations use FREESECT
    /// </summary>
    public readonly bool IsEndOfChain => Id is SectorType.EndOfChain or SectorType.Free;

    public readonly bool IsValid => Id <= SectorType.Maximum;

    public readonly long StartOffset
    {
        get
        {
            ThrowIfInvalid();
            return (Id + 1) * Length;
        }
    }

    public readonly long EndOffset
    {
        get
        {
            ThrowIfInvalid();
            return (Id + 2) * Length;
        }
    }

    readonly void ThrowIfInvalid()
    {
        if (!IsValid)
            throw new InvalidOperationException($"Invalid sector index: {Id}");
    }

    public override readonly string ToString() => $"{Id}";
}
