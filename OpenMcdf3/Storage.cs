namespace OpenMcdf3;

public class Storage
{
    internal IOContext IOContext { get; }

    internal DirectoryEntry DirectoryEntry { get; }

    internal Storage(IOContext ioContext, DirectoryEntry directoryEntry)
    {
        IOContext = ioContext;
        DirectoryEntry = directoryEntry;
    }

    public IEnumerable<EntryInfo> EnumerateEntries() => EnumerateDirectoryEntries()
        .Select(e => e.ToEntryInfo());

    public IEnumerable<EntryInfo> EnumerateEntries(StorageType type) => EnumerateDirectoryEntries(type)
        .Select(e => e.ToEntryInfo());

    IEnumerable<DirectoryEntry> EnumerateDirectoryEntries()
    {
        using DirectoryTreeEnumerator treeEnumerator = new(IOContext, DirectoryEntry);
        while (treeEnumerator.MoveNext())
        {
            yield return treeEnumerator.Current;
        }
    }

    IEnumerable<DirectoryEntry> EnumerateDirectoryEntries(StorageType type) => EnumerateDirectoryEntries()
        .Where(e => e.Type == type);

    public Storage OpenStorage(string name)
    {
        DirectoryEntry? entry = EnumerateDirectoryEntries(StorageType.Storage)
            .FirstOrDefault(entry => entry.Name == name) ?? throw new DirectoryNotFoundException($"Directory not found {name}");
        return new Storage(IOContext, entry);
    }

    public CfbStream OpenStream(string name)
    {
        DirectoryEntry? entry = EnumerateDirectoryEntries(StorageType.Stream)
            .FirstOrDefault(entry => entry.Name == name) ?? throw new FileNotFoundException("Stream not found", name);
        return new CfbStream(IOContext, entry);
    }
}
