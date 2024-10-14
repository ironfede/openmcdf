using System.Diagnostics;

namespace OpenMcdf3;

public enum Version : ushort
{
    V3 = 3,
    V4 = 4
}

public sealed class RootStorage : Storage, IDisposable
{
    bool disposed;

    public static RootStorage Create(string fileName, Version version = Version.V3)
    {
        FileStream stream = File.Create(fileName);
        Header header = new(version);
        McdfBinaryReader reader = new(stream);
        McdfBinaryWriter writer = new(stream);
        IOContext ioContext = new(header, reader, writer);
        DirectoryEntry directoryEntry = new();
        return new RootStorage(ioContext, directoryEntry);
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

    public static RootStorage Open(Stream stream, bool leaveOpen = false)
    {
        McdfBinaryReader reader = new(stream);
        McdfBinaryWriter? writer = stream.CanWrite ? new(stream) : null;
        Header header = reader.ReadHeader();
        IOContext ioContext = new(header, reader, writer, leaveOpen);
        DirectoryEntry rootDirectoryEntry = ioContext.EnumerateDirectoryEntries().First();
        return new RootStorage(ioContext, rootDirectoryEntry);
    }

    RootStorage(IOContext ioContext, DirectoryEntry rootDirectoryEntry)
        : base(ioContext, rootDirectoryEntry)
    {
    }

    public void Dispose()
    {
        if (disposed)
            return;

        IOContext?.Dispose();
        disposed = true;
    }
}
