namespace OpenMcdf3;

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
        this.ThrowIfDisposed(ioContext);

        return EnumerateDirectoryEntries()
            .Select(e => e.ToEntryInfo());
    }

    public IEnumerable<EntryInfo> EnumerateEntries(StorageType type)
    {
        this.ThrowIfDisposed(ioContext);

        return EnumerateDirectoryEntries(type)
            .Select(e => e.ToEntryInfo());
    }

    IEnumerable<DirectoryEntry> EnumerateDirectoryEntries()
    {
        this.ThrowIfDisposed(ioContext);

        using DirectoryTreeEnumerator treeEnumerator = new(ioContext, DirectoryEntry);
        while (treeEnumerator.MoveNext())
        {
            yield return treeEnumerator.Current;
        }
    }

    IEnumerable<DirectoryEntry> EnumerateDirectoryEntries(StorageType type) => EnumerateDirectoryEntries()
        .Where(e => e.Type == type);

    public Storage OpenStorage(string name)
    {
        this.ThrowIfDisposed(ioContext);

        DirectoryEntry? entry = EnumerateDirectoryEntries(StorageType.Storage)
            .FirstOrDefault(entry => entry.Name == name) ?? throw new DirectoryNotFoundException($"Directory not found {name}");
        return new Storage(ioContext, entry);
    }

    public Stream OpenStream(string name)
    {
        this.ThrowIfDisposed(ioContext);

        DirectoryEntry? entry = EnumerateDirectoryEntries(StorageType.Stream)
            .FirstOrDefault(entry => entry.Name == name) ?? throw new FileNotFoundException("Stream not found", name);
        return entry.StreamLength switch
        {
            < Header.MiniStreamCutoffSize => new MiniFatStream(ioContext, entry),
            _ => new FatStream(ioContext, entry)
        };
    }
}
