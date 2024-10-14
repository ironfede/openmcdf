namespace OpenMcdf3;

internal record struct Sector(uint Index, int Length)
{
    public const int MiniSectorSize = 64;

    public static readonly Sector EndOfChain = new(SectorType.EndOfChain, 0);

    readonly void ThrowIfInvalid()
    {
        if (Index > SectorType.Maximum)
            throw new InvalidOperationException($"Invalid sector index: {Index}");
    }

    public readonly long StartOffset
    {
        get
        {
            ThrowIfInvalid();
            return (Index + 1) * Length;
        }
    }

    public readonly long EndOffset
    {
        get
        {
            ThrowIfInvalid();
            return (Index + 2) * Length;
        }
    }

    public override readonly string ToString() => $"{Index}";
}
