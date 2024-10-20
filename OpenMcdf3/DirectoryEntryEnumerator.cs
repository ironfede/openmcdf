using System.Collections;

namespace OpenMcdf3;

/// <summary>
/// Enumerates <see cref="DirectoryEntry"/> instances from a <see cref="FatChainEnumerator"/>.
/// </summary>
internal sealed class DirectoryEntryEnumerator : IEnumerator<DirectoryEntry>
{
    private readonly IOContext ioContext;
    private readonly Version version;
    private readonly int entryCount;
    private readonly FatChainEnumerator chainEnumerator;
    private int entryIndex = -1;
    private DirectoryEntry? current;

    public DirectoryEntryEnumerator(IOContext ioContext)
    {
        this.ioContext = ioContext;
        this.version = (Version)ioContext.Header.MajorVersion;
        this.entryCount = ioContext.Header.SectorSize / DirectoryEntry.Length;
        this.chainEnumerator = new FatChainEnumerator(ioContext, ioContext.Header.FirstDirectorySectorId);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        chainEnumerator.Dispose();
    }

    /// <inheritdoc/>
    public DirectoryEntry Current
    {
        get
        {
            if (current is null)
                throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
            return current;
        }
    }

    /// <inheritdoc/>
    object IEnumerator.Current => Current;

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (entryIndex == -1 || entryIndex >= entryCount)
        {
            if (!chainEnumerator.MoveNext())
            {
                entryIndex = int.MaxValue;
                current = null;
                return false;
            }

            ioContext.Reader.Seek(chainEnumerator.Current.Position);
            entryIndex = 0;
        }

        current = ioContext.Reader.ReadDirectoryEntry(version);
        entryIndex++;
        return true;
    }

    /// <summary>
    /// Gets the <see cref="DirectoryEntry"/> for the specified stream ID.
    /// </summary>
    public DirectoryEntry GetDictionaryEntry(uint streamId)
    {
        if (streamId > StreamId.Maximum)
            throw new ArgumentException($"Invalid directory entry stream ID: ${streamId:X8}");

        uint chainIndex = (uint)Math.DivRem(streamId, entryCount, out long entryIndex);
        if (!chainEnumerator.MoveTo(chainIndex))
            throw new KeyNotFoundException($"Directory entry {streamId} was not found");

        long position = chainEnumerator.Current.Position + entryIndex * DirectoryEntry.Length;
        ioContext.Reader.Seek(position);
        current = ioContext.Reader.ReadDirectoryEntry(version);
        return current;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        chainEnumerator.Reset();
        entryIndex = -1;
        current = null;
    }
}
