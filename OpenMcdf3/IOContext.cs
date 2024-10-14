namespace OpenMcdf3;

internal sealed class IOContext : IDisposable
{
    public Header Header { get; }
    public McdfBinaryReader Reader { get; }
    public McdfBinaryWriter? Writer { get; }

    public IOContext(Header header, McdfBinaryReader reader, McdfBinaryWriter? writer, bool leaveOpen = false)
    {
        Header = header;
        Reader = reader;
        Writer = writer;
    }

    public void Dispose()
    {
        Reader.Dispose();
        Writer?.Dispose();
    }

    public IEnumerable<DirectoryEntry> EnumerateDirectoryEntries()
    {
        using DirectoryEntryEnumerator directoryEntriesEnumerator = new(this);
        while (directoryEntriesEnumerator.MoveNext())
            yield return directoryEntriesEnumerator.Current;
    }
}
