using System.Collections;

namespace OpenMcdf3;

internal sealed class DirectoryEntryEnumerator : IEnumerator<DirectoryEntry>
{
    private readonly IOContext ioContext;
    private readonly Version version;
    private readonly int entryCount;
    private readonly FatSectorChainEnumerator chainEnumerator;
    private int entryIndex;
    private DirectoryEntry? current;

    public DirectoryEntryEnumerator(IOContext ioContext)
    {
        this.ioContext = ioContext;
        this.version = (Version)ioContext.Header.MajorVersion;
        this.entryCount = ioContext.Header.SectorSize / DirectoryEntry.Length;
        this.chainEnumerator = new FatSectorChainEnumerator(ioContext, ioContext.Header.FirstDirectorySectorID);
        this.entryIndex = -1;
        this.current = default;
    }

    public DirectoryEntry Current => current!;

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

        int sectorIndex = Math.DivRem((int)id, entryCount, out int entryIndex);
        if (sectorIndex < chainEnumerator.Index)
        {
            chainEnumerator.Reset();
            chainEnumerator.MoveNext();
        }

        while (chainEnumerator.Index - 1 < sectorIndex)
        {
            if (!chainEnumerator.MoveNext())
                return null;
        }

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
