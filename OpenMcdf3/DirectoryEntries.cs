namespace OpenMcdf3;

internal sealed class DirectoryEntries : IDisposable
{
    private readonly IOContext ioContext;
    private readonly FatChainEnumerator fatChainEnumerator;
    private readonly DirectoryEntryEnumerator directoryEntryEnumerator;
    private readonly int entriesPerSector;

    public DirectoryEntries(IOContext ioContext)
    {
        this.ioContext = ioContext;
        fatChainEnumerator = new FatChainEnumerator(ioContext, ioContext.Header.FirstDirectorySectorId);
        directoryEntryEnumerator = new DirectoryEntryEnumerator(this);
        entriesPerSector = ioContext.SectorSize / DirectoryEntry.Length;
    }

    public void Dispose()
    {
        fatChainEnumerator.Dispose();
    }

    /// <summary>
    /// Gets the <see cref="DirectoryEntry"/> for the specified stream ID.
    /// </summary>
    public DirectoryEntry GetDictionaryEntry(uint streamId)
    {
        if (!TryGetDictionaryEntry(streamId, out DirectoryEntry? entry))
            throw new KeyNotFoundException($"Directory entry {streamId} was not found.");
        return entry!;
    }

    public bool TryGetDictionaryEntry(uint streamId, out DirectoryEntry? entry)
    {
        if (streamId == StreamId.NoStream)
        {
            entry = null;
            return false;
        }

        if (streamId > StreamId.Maximum)
            throw new ArgumentException($"Invalid directory entry stream ID: ${streamId:X8}.", nameof(streamId));

        uint chainIndex = (uint)Math.DivRem(streamId, entriesPerSector, out long entryIndex);
        if (!fatChainEnumerator.MoveTo(chainIndex))
        {
            entry = null;
            return false;
        }

        ioContext.Reader.Position = fatChainEnumerator.CurrentSector.Position + (entryIndex * DirectoryEntry.Length);
        entry = ioContext.Reader.ReadDirectoryEntry(ioContext.Version, streamId);
        return true;
    }

    public DirectoryEntry CreateOrRecycleDirectoryEntry()
    {
        DirectoryEntry? entry = TryRecycleDirectoryEntry();
        if (entry is not null)
            return entry;

        CfbBinaryWriter writer = ioContext.Writer;
        uint id = fatChainEnumerator.Extend();
        Header header = ioContext.Header;
        if (header.FirstDirectorySectorId == SectorType.EndOfChain)
            header.FirstDirectorySectorId = id;
        if (ioContext.Version == Version.V4)
            header.DirectorySectorCount++;

        Sector sector = new(id, ioContext.SectorSize);
        writer.Position = sector.Position;
        for (int i = 0; i < entriesPerSector; i++)
            writer.Write(DirectoryEntry.Unallocated);

        entry = TryRecycleDirectoryEntry()
            ?? throw new InvalidOperationException("Failed to add or recycle directory entry.");
        return entry;
    }

    private DirectoryEntry? TryRecycleDirectoryEntry()
    {
        directoryEntryEnumerator.Reset();

        while (directoryEntryEnumerator.MoveNext())
        {
            DirectoryEntry current = directoryEntryEnumerator.Current;
            if (directoryEntryEnumerator.Current.Type is StorageType.Unallocated)
                return current;
        }

        return null;
    }

    public void Write(DirectoryEntry entry)
    {
        uint chainIndex = (uint)Math.DivRem(entry.Id, entriesPerSector, out long entryIndex);
        if (!fatChainEnumerator.MoveTo(chainIndex))
            throw new KeyNotFoundException($"Directory entry {entry.Id} was not found.");

        CfbBinaryWriter writer = ioContext.Writer;
        writer.Position = fatChainEnumerator.CurrentSector.Position + (entryIndex * DirectoryEntry.Length);
        writer.Write(entry);
    }
}
