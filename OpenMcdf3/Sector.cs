namespace OpenMcdf3;

internal record struct Sector(uint Index, int Length)
{
    public const int MiniSectorSize = 64;

    public static readonly Sector EndOfChain = new(SectorType.EndOfChain, 0);

    public readonly long StartOffset => (Index + 1) * Length;

    public readonly long EndOffset => (Index + 2) * Length;

    public override readonly string ToString() => $"{Index}";
}
