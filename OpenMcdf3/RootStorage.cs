using System.Diagnostics;

namespace OpenMcdf3;

public enum Version : ushort
{
    V3 = 3,
    V4 = 4
}

public sealed class RootStorage : Storage, IDisposable
{
    readonly IOContext ioContext;
    bool disposed;

    public static RootStorage Create(string fileName, Version version = Version.V3)
    {
        FileStream stream = File.Create(fileName);
        Header header = new(version);
        McdfBinaryReader reader = new(stream);
        McdfBinaryWriter writer = new(stream);
        IOContext ioContext = new(header, reader, writer);
        return new RootStorage(ioContext);
    }

    public static RootStorage Open(string fileName, FileMode mode)
    {
        FileStream stream = File.Open(fileName, mode);
        return Open(stream);
    }

    public static RootStorage OpenRead(string fileName)
    {
        FileStream stream = File.OpenRead(fileName);
        return Open(stream);
    }

    public static RootStorage Open(Stream stream)
    {
        McdfBinaryReader reader = new(stream);
        McdfBinaryWriter? writer = stream.CanWrite ? new(stream) : null;
        Header header = reader.ReadHeader();
        IOContext ioContext = new(header, reader, writer);
        return new RootStorage(ioContext);
    }

    RootStorage(IOContext ioContext)
    {
        this.ioContext = ioContext;
    }

    public void Dispose()
    {
        if (disposed)
            return;

        ioContext.Writer?.Dispose();
        ioContext.Reader.Dispose();
        disposed = true;
    }

    IEnumerable<DirectoryEntry> EnumerateDirectoryEntries()
    {
        using FatSectorChainEnumerator chainEnumerator = new(ioContext, ioContext.Header.FirstDirectorySectorID);
        while (chainEnumerator.MoveNext())
        {
            ioContext.Reader.Seek(chainEnumerator.Current.StartOffset);

            int entryCount = ioContext.Header.SectorSize / DirectoryEntry.Length;
            for (int i = 0; i < entryCount; i++)
            {
                DirectoryEntry entry = ioContext.Reader.ReadDirectoryEntry((Version)ioContext.Header.MajorVersion);
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
        return new CfbStream(ioContext, ioContext.Header.SectorSize, entry);
    }
}
