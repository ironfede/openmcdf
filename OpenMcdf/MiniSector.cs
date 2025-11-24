using System.Diagnostics.CodeAnalysis;

namespace OpenMcdf;

/// <summary>
/// Encapsulates information about a mini sector in a compound file.
/// </summary>
/// <param name="Id">The ID of the mini sector.</param>
/// <param name="Length">The sector length.</param>
internal record struct MiniSector(uint Id, int Length)
{
    /// <summary>
    /// Gets the position of the mini sector in the mini FAT stream.
    /// </summary>
    public readonly long Position => Id * Length;

    /// <summary>
    /// Gets the end position of the mini sector in the mini FAT stream.
    /// </summary>
    public readonly long EndPosition => (Id + 1) * Length;

    [ExcludeFromCodeCoverage]
    public override readonly string ToString() => $"{Id}";
}
