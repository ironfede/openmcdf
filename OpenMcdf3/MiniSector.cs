namespace OpenMcdf3;

/// <summary>
/// Encapsulates information about a mini sector in a compound file.
/// </summary>
/// <param name="Id">The ID of the mini sector</param>
internal record struct MiniSector(uint Id)
{
    public const int Length = 64;

    public static readonly MiniSector EndOfChain = new(SectorType.EndOfChain);

    public readonly bool IsValid => Id <= SectorType.Maximum;

    public readonly bool IsEndOfChain => Id is SectorType.EndOfChain or SectorType.Free;

    /// <summary>
    /// The position of the mini sector in the mini FAT stream.
    /// </summary>
    public readonly long Position
    {
        get
        {
            ThrowIfInvalid();
            return Id * Length;
        }
    }

    /// <summary>
    /// The end position of the mini sector in the mini FAT stream.
    /// </summary>
    public readonly long EndPosition
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
            throw new InvalidOperationException($"Invalid sector ID: {Id}");
    }

    public override readonly string ToString() => $"{Id}";
}
