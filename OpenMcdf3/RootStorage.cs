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
        return Create(stream, version);
    }

    public static RootStorage Create(Stream stream, Version version = Version.V3)
    {
        Header header = new(version);
        stream.SetLength(0);
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
        stream.Position = 0;

        Header header;
        using (CfbBinaryReader headerReader = new(stream))
        {
            header = headerReader.ReadHeader();
        }

        CfbBinaryReader reader = new(stream);
        CfbBinaryWriter? writer = stream.CanWrite ? new(stream) : null;
        IOContextFlags contextFlags = leaveOpen ? IOContextFlags.LeaveOpen : IOContextFlags.None;
        IOContext ioContext = new(header, reader, writer, contextFlags);
        return new RootStorage(ioContext);
    }

    RootStorage(IOContext ioContext)
        : base(ioContext, ioContext.RootEntry)
    {
    }

    public void Dispose() => ioContext?.Dispose();

    internal void Trace(TextWriter writer)
    {
        writer.WriteLine(ioContext.Header);
        ioContext.Fat.Trace(writer);
        ioContext.MiniFat.Trace(writer);
    }
}
