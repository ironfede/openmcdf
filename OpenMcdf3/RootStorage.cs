namespace OpenMcdf3;

public enum Version : ushort
{
    V3 = 3,
    V4 = 4
}

public sealed class RootStorage : Storage
{
    readonly Header header;
    readonly McdfBinaryReader reader;
    readonly McdfBinaryWriter? writer;

    RootStorage(Header header, McdfBinaryReader reader, McdfBinaryWriter? writer = null)
    {
        this.header = header;
        this.reader = reader;
        this.writer = writer;
    }

    public static RootStorage Create(string fileName, Version version = Version.V3)
    {
        FileStream stream = File.Create(fileName);
        Header header = new(version);
        McdfBinaryReader reader = new(stream);
        McdfBinaryWriter writer = new(stream);
        return new RootStorage(header, reader, writer);
    }

    public static RootStorage Open(string fileName, FileMode mode)
    {
        FileStream stream = File.Open(fileName, mode);
        McdfBinaryReader reader = new(stream);
        Header header = reader.ReadHeader();
        return new RootStorage(header, reader);
    }
}
