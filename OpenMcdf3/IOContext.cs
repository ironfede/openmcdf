namespace OpenMcdf3;

enum IOContextFlags
{
    None = 0,
    Create = 1,
    LeaveOpen = 2
}

/// <summary>
/// Encapsulates the objects required to read and write data to and from a compound file.
/// </summary>
internal sealed class IOContext : IDisposable
{
    public Header Header { get; }

    public CfbBinaryReader Reader { get; }

    public CfbBinaryWriter? Writer { get; }

    public DirectoryEntry RootEntry { get; }

    public bool IsDisposed { get; private set; }

    public IOContext(Header header, CfbBinaryReader reader, CfbBinaryWriter? writer, IOContextFlags contextFlags = IOContextFlags.None)
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

    /// <summary>
    /// Enumerates all the <see cref="DirectoryEntry"/> instances in the compound file.
    /// </summary>
    public IEnumerable<DirectoryEntry> EnumerateDirectoryEntries()
    {
        this.ThrowIfDisposed(IsDisposed);

        using DirectoryEntryEnumerator directoryEntriesEnumerator = new(this);
        while (directoryEntriesEnumerator.MoveNext())
            yield return directoryEntriesEnumerator.Current;
    }
}
