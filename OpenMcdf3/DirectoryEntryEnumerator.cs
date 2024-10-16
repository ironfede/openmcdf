using System.Collections;

namespace OpenMcdf3;

internal sealed class DirectoryEntryEnumerator : IEnumerator<DirectoryEntry>
{
    private readonly IOContext ioContext;
    private readonly Version version;
    private readonly int entryCount;
    private readonly FatSectorChainEnumerator chainEnumerator;
    private int entryIndex = -1;
    private DirectoryEntry? current;

    public DirectoryEntryEnumerator(IOContext ioContext)
    {
        this.ioContext = ioContext;
        this.version = (Version)ioContext.Header.MajorVersion;
        this.entryCount = ioContext.Header.SectorSize / DirectoryEntry.Length;
        this.chainEnumerator = new FatSectorChainEnumerator(ioContext, ioContext.Header.FirstDirectorySectorId);
    }

    public DirectoryEntry Current
    {
        get
        {
            if (current is null)
                throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
            return current;
        }
    }

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (entryIndex == -1 || entryIndex >= entryCount)
        {
            if (!chainEnumerator.MoveNext())
                return false;

            ioContext.Reader.Seek(chainEnumerator.Current.StartOffset);
            entryIndex = 0;
        }

        current = ioContext.Reader.ReadDirectoryEntry(version);
        entryIndex++;
        return current.Type != StorageType.Unallocated;
    }

    public DirectoryEntry? Get(uint id)
    {
        if (id == StreamId.NoStream)
            return null;

        uint chainIndex = (uint)Math.DivRem(id, entryCount, out long entryIndex);
        if (!chainEnumerator.MoveTo(chainIndex))
            throw new ArgumentException("Invalid directory entry ID");

        long position = chainEnumerator.Current.StartOffset + entryIndex * DirectoryEntry.Length;
        ioContext.Reader.Seek(position);
        current = ioContext.Reader.ReadDirectoryEntry(version);
        return current;
    }

    public void Reset()
    {
        chainEnumerator.Reset();
        entryIndex = -1;
        current = default!;
    }

    public void Dispose()
    {
        chainEnumerator.Dispose();
    }
}
