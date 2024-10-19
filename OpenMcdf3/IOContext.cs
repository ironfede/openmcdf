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
    readonly DirectoryEntryEnumerator directoryEnumerator;
    readonly CfbBinaryWriter? writer;
    MiniFat? miniFat;
    FatStream? miniStream;

    public Header Header { get; }

    public CfbBinaryReader Reader { get; }

    public CfbBinaryWriter Writer
    {
        get
        {
            if (writer is null)
                throw new InvalidOperationException("Stream is not writable");
            return writer;
        }
    }

    public Fat Fat { get; }

    public DirectoryEntry RootEntry { get; }

    public MiniFat MiniFat
    {
        get
        {
            miniFat ??= new(this);
            return miniFat;
        }
    }

    public FatStream MiniStream
    {
        get
        {
            miniStream ??= new(this, RootEntry);
            return miniStream;
        }
    }

    public bool CanWrite => writer is not null;

    public bool IsDisposed { get; private set; }

    /// <summary>
    /// The size of a regular sector.
    /// </summary>
    public int SectorSize { get; }

    public int MiniSectorSize { get; }

    public Version Version => (Version)Header.MajorVersion;

    public IOContext(Header header, CfbBinaryReader reader, CfbBinaryWriter? writer, IOContextFlags contextFlags = IOContextFlags.None)
    {
        if (contextFlags.HasFlag(IOContextFlags.Create) && writer is null)
            throw new ArgumentNullException(nameof(writer), "A writer is required to create a new compound file.");

        Header = header;
        Reader = reader;
        this.writer = writer;

        SectorSize = 1 << header.SectorShift;
        MiniSectorSize = 1 << header.MiniSectorShift;

        Fat = new(this);
        directoryEnumerator = new(this);

        if (contextFlags.HasFlag(IOContextFlags.Create))
        {
            RootEntry = directoryEnumerator.CreateOrRecycleDirectoryEntry();
            RootEntry.RecycleRoot();

            WriteHeader();
            Write(RootEntry);
        }
        else
        {
            if (!directoryEnumerator.MoveNext())
                throw new FormatException("Root directory entry not found.");
            RootEntry = directoryEnumerator.Current;
        }
    }

    public void Dispose()
    {
        if (!IsDisposed)
        {
            if (CanWrite)
                WriteHeader();
            miniStream?.Dispose();
            miniFat?.Dispose();
            directoryEnumerator.Dispose();
            Fat.Dispose();
            writer?.Dispose();
            Reader.Dispose();
            IsDisposed = true;
        }
    }

    public void ExtendStreamLength(long length)
    {
        Stream baseStream = Writer.BaseStream;
        if (baseStream.Length < length)
            baseStream.SetLength(length);
    }

    public void WriteHeader()
    {
        CfbBinaryWriter writer = Writer;
        writer.Seek(0, SeekOrigin.Begin);
        writer.Write(Header);
    }

    public void Write(DirectoryEntry entry)
    {
        directoryEnumerator.Write(entry);
    }
}
