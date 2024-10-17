using System.Collections;

namespace OpenMcdf3;

/// <summary>
/// Enumerates the <see cref="MiniSector"/>s in a FAT sector chain.
/// </summary>
internal sealed class MiniFatSectorEnumerator : IEnumerator<MiniSector>
{
    private readonly IOContext ioContext;
    private readonly FatSectorChainEnumerator fatChain;
    bool start = true;
    MiniSector current = MiniSector.EndOfChain;

    public MiniFatSectorEnumerator(IOContext ioContext)
    {
        this.ioContext = ioContext;
        fatChain = new(ioContext, ioContext.Header.FirstMiniFatSectorId);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        fatChain.Dispose();
    }

    /// <inheritdoc/>
    public MiniSector Current
    {
        get
        {
            if (current.IsEndOfChain)
                throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
            return current;
        }
    }

    /// <inheritdoc/>
    object IEnumerator.Current => Current;

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (start)
        {
            current = new(ioContext.Header.FirstMiniFatSectorId);
            start = false;
        }
        else if (!current.IsEndOfChain)
        {
            uint sectorId = GetNextMiniFatSectorId(current.Id);
            current = new(sectorId);
        }

        return !current.IsEndOfChain;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        start = true;
        current = MiniSector.EndOfChain;
    }

    /// <summary>
    /// Gets the next mini FAT sector ID.
    /// </summary>
    /// <param name="sectorId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public uint GetNextMiniFatSectorId(uint sectorId)
    {
        if (sectorId > SectorType.Maximum)
            throw new ArgumentException($"Invalid sector ID: {sectorId}", nameof(sectorId));

        int elementLength = ioContext.Header.SectorSize / sizeof(uint);
        uint fatSectorId = (uint)Math.DivRem(sectorId, elementLength, out long sectorOffset);
        if (!fatChain.MoveTo(fatSectorId))
            throw new ArgumentException($"Invalid sector ID: {sectorId}", nameof(sectorId));

        long position = fatChain.Current.Position + sectorOffset * sizeof(uint);
        ioContext.Reader.Seek(position);
        uint nextId = ioContext.Reader.ReadUInt32();
        return nextId;
    }
}
