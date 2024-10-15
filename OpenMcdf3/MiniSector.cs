namespace OpenMcdf3;

internal record struct MiniSector(uint Id)
{
    public const int Length = 64;

    public static readonly MiniSector EndOfChain = new(SectorType.EndOfChain);

    public readonly bool IsValid => Id <= SectorType.Maximum;

    public readonly bool IsEndOfChain => Id is SectorType.EndOfChain or SectorType.Free;

    public readonly long StartOffset
    {
        get
        {
            ThrowIfInvalid();
            return Id * Length;
        }
    }

    public readonly long EndOffset
    {
        get
        {
            ThrowIfInvalid();
            return (Id + 1) * Length;
        }
    }

    readonly void ThrowIfInvalid()
    {
        if (!IsValid)
            throw new InvalidOperationException($"Invalid sector index: {Id}");
    }

    public override readonly string ToString() => $"{Id}";
}
