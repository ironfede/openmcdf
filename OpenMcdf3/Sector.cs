namespace OpenMcdf3;

/// <summary>
/// Encapsulates information about a sector in a compound file.
/// </summary>
/// <param name="Id">The sector ID</param>
/// <param name="Length">The sector length</param>
internal record struct Sector(uint Id, int Length)
{
    public static readonly Sector EndOfChain = new(SectorType.EndOfChain, 0);

    /// <summary>
    /// Compound File Binary File Format only specifies that ENDOFCHAIN ends the DIFAT chain
    /// but some implementations use FREESECT
    /// </summary>
    public readonly bool IsEndOfChain => Id is SectorType.EndOfChain or SectorType.Free;

    public readonly bool IsValid => Id <= SectorType.Maximum;

    /// <summary>
    /// The position of the mini sector in the compound file stream.
    /// </summary>
    public readonly long Position
    {
        get
        {
            ThrowIfInvalid();
            return (Id + 1) * Length;
        }
    }

    /// <summary>
    /// The end position of the mini sector in the compound file stream.
    /// </summary>
    public readonly long EndPosition
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
            throw new InvalidOperationException($"Invalid sector ID: {Id}");
    }

    public override readonly string ToString() => $"{Id}";
}
