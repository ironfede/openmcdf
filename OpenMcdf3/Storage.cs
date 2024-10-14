namespace OpenMcdf3;

public enum StorageType
{
    Invalid = 0,
    Storage = 1,
    Stream = 2,
    Lockbytes = 3,
    Property = 4,
    Root = 5
}

public class Storage
{
    internal IOContext IOContext { get; }

    uint firstDirectorySector;

    internal Storage(IOContext ioContext, uint firstDirectorySector)
    {
        IOContext = ioContext;
        this.firstDirectorySector = firstDirectorySector;
    }

    IEnumerable<DirectoryEntry> EnumerateDirectoryEntries()
    {
        var version = (Version)IOContext.Header.MajorVersion;
        int entryCount = IOContext.Header.SectorSize / DirectoryEntry.Length;
        using FatSectorChainEnumerator chainEnumerator = new(IOContext, firstDirectorySector);
        while (chainEnumerator.MoveNext())
        {
            IOContext.Reader.Seek(chainEnumerator.Current.StartOffset);

            for (int i = 0; i < entryCount; i++)
            {
                DirectoryEntry entry = IOContext.Reader.ReadDirectoryEntry(version);
                if (entry.Type is not StorageType.Invalid)
                    yield return entry;
            }
        }
    }

    public IEnumerable<EntryInfo> EnumerateEntries() => EnumerateDirectoryEntries().Select(e => new EntryInfo { Name = e.Name });

    public CfbStream OpenStream(string name)
    {
        DirectoryEntry? entry = EnumerateDirectoryEntries()
            .FirstOrDefault(entry => entry.Name == name) ?? throw new FileNotFoundException("Stream not found", name);
        return new CfbStream(IOContext, IOContext.Header.SectorSize, entry);
    }
}
