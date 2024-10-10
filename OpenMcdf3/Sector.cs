namespace OpenMcdf3;

internal struct Sector
{
    const int MiniSectorSize = 64;

    public Sector(uint index, long length)
    {
        Index = index;
        Length = length;
    }

    public uint Index { get; }

    public long Length { get; }

    public readonly long StartOffset => (Index + 1) * Length;

    public readonly long EndOffset => (Index + 2) * Length;
}
