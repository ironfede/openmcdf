namespace OpenMcdf3;

enum IOContextFlags
{
    None = 0,
    Create = 1,
    LeaveOpen = 2
}

internal sealed class IOContext : IDisposable
{
    public Header Header { get; }

    public McdfBinaryReader Reader { get; }

    public McdfBinaryWriter? Writer { get; }

    public DirectoryEntry RootEntry { get; }

    public bool IsDisposed { get; private set; }

    public IOContext(Header header, McdfBinaryReader reader, McdfBinaryWriter? writer, IOContextFlags contextFlags = IOContextFlags.None)
    {
        Header = header;
        Reader = reader;
        Writer = writer;
        RootEntry = contextFlags.HasFlag(IOContextFlags.Create)
            ? new DirectoryEntry()
            : EnumerateDirectoryEntries().First();
    }

    public void Dispose()
    {
        if (!IsDisposed)
        {
            Reader.Dispose();
            Writer?.Dispose();
            IsDisposed = true;
        }
    }

    public IEnumerable<DirectoryEntry> EnumerateDirectoryEntries()
    {
        this.ThrowIfDisposed(IsDisposed);

        using DirectoryEntryEnumerator directoryEntriesEnumerator = new(this);
        while (directoryEntriesEnumerator.MoveNext())
            yield return directoryEntriesEnumerator.Current;
    }
}
