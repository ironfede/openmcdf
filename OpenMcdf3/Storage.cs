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

    DirectoryEntry? TryGetDirectoryEntry(StorageType storageType, string name)
    {
        using DirectoryTreeEnumerator directoryTreeEnumerator = new(ioContext, DirectoryEntry);
        return directoryTreeEnumerator.TryGetDirectoryEntry(storageType, name);
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

        DirectoryEntry entry = TryGetDirectoryEntry(StorageType.Storage, name)
            ?? throw new DirectoryNotFoundException($"Storage not found: {name}.");
        return new Storage(ioContext, entry);
    }

    public CfbStream OpenStream(string name)
    {
        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(ioContext.IsDisposed);

        DirectoryEntry? entry = TryGetDirectoryEntry(StorageType.Stream, name)
            ?? throw new FileNotFoundException($"Stream not found: {name}.", name);

        // TODO: Return a Stream that can transition between FAT and mini FAT
        return new CfbStream(ioContext, entry);
    }
}
