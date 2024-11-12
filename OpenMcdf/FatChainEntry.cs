namespace OpenMcdf;

internal record struct FatChainEntry(uint Index, uint Value)
{
    internal static readonly FatChainEntry Invalid = new(uint.MaxValue, SectorType.EndOfChain);

    public readonly bool IsFreeOrEndOfChain => SectorType.IsFreeOrEndOfChain(Value);

    public override readonly string ToString() => $"#{Index}: {Value}";
}
