namespace OpenMcdf3;

public enum Version : ushort
{
    V3 = 3,
    V4 = 4
}

/// <summary>
/// Encapsulates the root <see cref="Storage"> of a compound file.
/// </summary>
public sealed class RootStorage : Storage, IDisposable
{
    public static RootStorage Create(string fileName, Version version = Version.V3)
    {
        FileStream stream = File.Create(fileName);
        Header header = new(version);
        CfbBinaryReader reader = new(stream);
        CfbBinaryWriter writer = new(stream);
        IOContext ioContext = new(header, reader, writer, IOContextFlags.Create);
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

    public static RootStorage Open(Stream stream, bool leaveOpen = false)
    {
        CfbBinaryReader reader = new(stream);
        CfbBinaryWriter? writer = stream.CanWrite ? new(stream) : null;
        Header header = reader.ReadHeader();
        IOContextFlags contextFlags = leaveOpen ? IOContextFlags.LeaveOpen : IOContextFlags.None;
        IOContext ioContext = new(header, reader, writer, contextFlags);
        return new RootStorage(ioContext);
    }

    RootStorage(IOContext ioContext)
        : base(ioContext, ioContext.RootEntry)
    {
    }

    public void Dispose() => ioContext?.Dispose();
}
