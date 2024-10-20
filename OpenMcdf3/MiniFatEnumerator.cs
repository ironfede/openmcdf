using System.Collections;

namespace OpenMcdf3;

/// <summary>
/// Enumerates the <see cref="MiniSector"/>s from the Mini FAT.
/// </summary>
internal sealed class MiniFatEnumerator : IEnumerator<MiniSector>
{
    private readonly IOContext ioContext;
    private readonly MiniFatSectorEnumerator miniFatSectorEnumerator;
    private bool start = true;
    private int index = int.MaxValue;
    private MiniSector current = MiniSector.EndOfChain;

    public MiniFatEnumerator(IOContext ioContext)
    {
        miniFatSectorEnumerator = new(ioContext);
        this.ioContext = ioContext;
    }

    /// <inheritdoc/>
    public MiniSector Current
    {
        get
        {
            if (index == int.MaxValue)
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
            if (!miniFatSectorEnumerator.MoveNext())
            {
                index = int.MaxValue;
                return false;
            }

            index = -1;
            start = false;
        }

        index++;
        int elementCount = MiniSector.Length / sizeof(uint);
        if (index > elementCount)
        {
            if (!miniFatSectorEnumerator.MoveNext())
            {
                index = int.MaxValue;
                return false;
            }

            index = 0;
        }

        long position = miniFatSectorEnumerator.Current.Position + index * sizeof(uint);
        ioContext.Reader.Seek(position);
        uint sectorId = ioContext.Reader.ReadUInt32();
        current = new(sectorId);
        return true;
    }

    public void MoveTo(uint id)
    {
        if (id > SectorType.Maximum)
            throw new ArgumentException("Invalid sector ID", nameof(id));

        int elementCount = ioContext.Header.SectorSize / sizeof(uint);
        uint sectorId = (uint)Math.DivRem(id, elementCount, out long index);

        miniFatSectorEnumerator.MoveTo(sectorId);
        long position = miniFatSectorEnumerator.Current.Position + index * sizeof(uint);
        ioContext.Reader.Seek(position);
        uint value = ioContext.Reader.ReadUInt32();
        this.index = (int)index;
        start = false;
        current = new(value);
    }

    /// <inheritdoc/>
    public void Reset()
    {
        miniFatSectorEnumerator.Reset();
        start = true;
        current = MiniSector.EndOfChain;
        index = int.MaxValue;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        miniFatSectorEnumerator.Dispose();
    }
}
