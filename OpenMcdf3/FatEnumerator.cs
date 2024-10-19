using System.Collections;

namespace OpenMcdf3;

/// <summary>
/// Enumerates the entries in a FAT.
/// </summary>
internal class FatEnumerator : IEnumerator<FatEntry>
{
    readonly IOContext ioContext;
    bool start = true;
    uint index = uint.MaxValue;
    uint value = uint.MaxValue;

    public FatEnumerator(IOContext ioContext)
    {
        this.ioContext = ioContext;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    public Sector CurrentSector
    {
        get
        {
            if (index == uint.MaxValue)
                throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
            return new(index, ioContext.SectorSize);
        }
    }

    /// <inheritdoc/>
    public FatEntry Current
    {
        get
        {
            if (index == uint.MaxValue)
                throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
            return new(index, value);
        }
    }

    /// <inheritdoc/>
    object IEnumerator.Current => Current;

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (start)
        {
            start = false;
            return MoveTo(0);
        }

        if (index >= SectorType.Maximum)
            return false;

        uint next = index + 1;
        return MoveTo(next);
    }

    public bool MoveTo(uint index)
    {
        ThrowHelper.ThrowIfSectorIdIsInvalid(index);

        start = false;
        if (this.index == index)
            return true;

        if (ioContext.Fat.TryGetValue(index, out value))
        {
            this.index = index;
            return true;
        }

        this.index = uint.MaxValue;
        return false;
    }

    public bool MoveNextFreeEntry()
    {
        while (MoveNext())
        {
            if (value == SectorType.Free)
                return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        start = true;
        index = uint.MaxValue;
        value = uint.MaxValue;
    }

    internal void Trace(TextWriter writer)
    {
        Reset();

        byte[] data = new byte[ioContext.SectorSize];

        Stream baseStream = ioContext.Reader.BaseStream;

        writer.WriteLine("Start of FAT =================");

        while (MoveNext())
        {
            FatEntry current = Current;
            if (current.IsFree)
            {
                writer.WriteLine($"{current}");
            }
            else
            {
                baseStream.Position = CurrentSector.Position;
                baseStream.ReadExactly(data, 0, data.Length);
                string hex = BitConverter.ToString(data);
                writer.WriteLine($"{current}: {hex}");
            }
        }

        writer.WriteLine("End of FAT ===================");
    }

    public override string ToString() => $"{Current}";
}
