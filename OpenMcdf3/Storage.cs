namespace OpenMcdf3;

/// <summary>
/// An object in a compound file that is analogous to a file system directory.
/// </summary>
public class Storage
{
    internal readonly IOContext ioContext;

    internal DirectoryEntry DirectoryEntry { get; }

    internal Storage(IOContext ioContext, DirectoryEntry directoryEntry)
    {
        if (directoryEntry.Type is not StorageType.Storage and not StorageType.Root)
            throw new ArgumentException("DirectoryEntry must be a Storage or Root.", nameof(directoryEntry));

        this.ioContext = ioContext;
        DirectoryEntry = directoryEntry;
    }

    public IEnumerable<EntryInfo> EnumerateEntries()
    {
        this.ThrowIfDisposed(ioContext.IsDisposed);

        return EnumerateDirectoryEntries()
            .Select(e => e.ToEntryInfo());
    }

    public IEnumerable<EntryInfo> EnumerateEntries(StorageType type)
    {
        this.ThrowIfDisposed(ioContext.IsDisposed);

        return EnumerateDirectoryEntries(type)
            .Select(e => e.ToEntryInfo());
    }

    IEnumerable<DirectoryEntry> EnumerateDirectoryEntries()
    {
        using DirectoryTreeEnumerator treeEnumerator = new(ioContext, DirectoryEntry);
        while (treeEnumerator.MoveNext())
        {
            yield return treeEnumerator.Current;
        }
    }

    IEnumerable<DirectoryEntry> EnumerateDirectoryEntries(StorageType type) => EnumerateDirectoryEntries()
        .Where(e => e.Type == type);

    DirectoryEntry? TryGetDirectoryEntry(string name)
    {
        using DirectoryTreeEnumerator directoryTreeEnumerator = new(ioContext, DirectoryEntry);
        return directoryTreeEnumerator.TryGetDirectoryEntry(name);
    }

    DirectoryEntry AddDirectoryEntry(StorageType storageType, string name)
    {
        using DirectoryTreeEnumerator directoryTreeEnumerator = new(ioContext, DirectoryEntry);
        return directoryTreeEnumerator.Add(storageType, name);
    }

    public Storage CreateStorage(string name)
    {
        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(ioContext.IsDisposed);

        DirectoryEntry entry = AddDirectoryEntry(StorageType.Storage, name);
        return new Storage(ioContext, entry);
    }

    public CfbStream CreateStream(string name)
    {
        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(ioContext.IsDisposed);

        // TODO: Return a Stream that can transition between FAT and mini FAT
        DirectoryEntry entry = AddDirectoryEntry(StorageType.Stream, name);
        return new CfbStream(ioContext, entry);
    }

    public Storage OpenStorage(string name)
    {
        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(ioContext.IsDisposed);

        DirectoryEntry? entry = TryGetDirectoryEntry(name);
        if (entry is null || entry.Type is not StorageType.Storage)
            throw new DirectoryNotFoundException($"Storage not found: {name}.");
        return new Storage(ioContext, entry);
    }

    public CfbStream OpenStream(string name)
    {
        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(ioContext.IsDisposed);

        DirectoryEntry? entry = TryGetDirectoryEntry(name);
        if (entry is null || entry.Type is not StorageType.Stream)
            throw new FileNotFoundException($"Stream not found: {name}.", name);

        // TODO: Return a Stream that can transition between FAT and mini FAT
        return new CfbStream(ioContext, entry);
    }

    public void Delete(string name)
    {
        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(ioContext.IsDisposed);

        using DirectoryTreeEnumerator directoryTreeEnumerator = new(ioContext, DirectoryEntry);
        DirectoryEntry? entry = directoryTreeEnumerator.TryGetDirectoryEntry(name);
        if (entry is null)
            return;

        if (entry.Type is StorageType.Storage && entry.ChildId is not StreamId.NoStream)
        {
            Storage storage = new(ioContext, entry);
            foreach (EntryInfo childEntry in storage.EnumerateEntries())
            {
                storage.Delete(childEntry.Name);
            };
        }

        if (entry.Type is StorageType.Stream && entry.StartSectorId is not StreamId.NoStream)
        {
            if (entry.StreamLength < Header.MiniStreamCutoffSize)
            {
                using MiniFatChainEnumerator miniFatChainEnumerator = new(ioContext, entry.StartSectorId);
                miniFatChainEnumerator.Shrink(0);
            }
            else
            {
                using FatChainEnumerator fatChainEnumerator = new(ioContext, entry.StartSectorId);
                fatChainEnumerator.Shrink(0);
            }
        }

        directoryTreeEnumerator.Remove(entry);
    }

    internal void TraceDirectoryEntries(TextWriter writer)
    {
        using DirectoryTreeEnumerator treeEnumerator = new(ioContext, DirectoryEntry);
        treeEnumerator.PrintTrace(writer);
    }
}
