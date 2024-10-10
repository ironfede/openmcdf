using System.Text;

namespace OpenMcdf3;

internal class McdfBinaryReader : BinaryReader
{
    readonly byte[] buffer = new byte[DirectoryEntry.NameFieldLength];

    public McdfBinaryReader(Stream input) : base(input)
    {
    }

    public Guid ReadGuid() => new(ReadBytes(16));

    public DateTime ReadFileTime()
    {
        long fileTime = ReadInt64();
        return DateTime.FromFileTimeUtc(fileTime);
    }

    private void ReadBytes(byte[] buffer) => Read(buffer, 0, buffer.Length);

    public Header ReadHeader()
    {
        Header header = new();
        Read(buffer, 0, Header.Signature.Length);
        if (!buffer.Take(Header.Signature.Length).SequenceEqual(Header.Signature))
            throw new FormatException("Invalid signature");
        header.CLSID = ReadGuid();
        header.MinorVersion = ReadUInt16();
        header.MajorVersion = ReadUInt16();
        ushort byteOrder = ReadUInt16();
        if (byteOrder != Header.LittleEndian)
            throw new FormatException($"Unsupported byte order: {byteOrder:X4}. Only little-endian is supported ({Header.LittleEndian:X4})");
        header.SectorShift = ReadUInt16();
        ushort miniSectorShift = ReadUInt16();
        if (miniSectorShift != Header.MiniSectorShift)
            throw new FormatException($"Unsupported sector shift {miniSectorShift}. Only {Header.MiniSectorShift} is supported");
        this.FillBuffer(6);
        header.DirectorySectorCount = ReadUInt32();
        header.FatSectorCount = ReadUInt32();
        header.FirstDirectorySectorID = ReadUInt32();
        this.FillBuffer(4);
        uint miniStreamCutoffSize = ReadUInt32();
        if (miniStreamCutoffSize != Header.MiniStreamCutoffSize)
            throw new FormatException("Mini stream cutoff size must be 4096 byte");
        header.FirstMiniFatSectorID = ReadUInt32();
        header.MiniFatSectorCount = ReadUInt32();
        header.FirstDifatSectorID = ReadUInt32();
        header.DifatSectorCount = ReadUInt32();

        for (int i = 0; i < Header.DifatLength; i++)
        {
            header.Difat[i] = ReadUInt32();
        }

        return header;
    }

    public StorageType ReadStorageType() => (StorageType)ReadByte();

    public Color ReadColor() => (Color)ReadByte();

    public DirectoryEntry ReadDirectoryEntry(Version version)
    {
        if (version is not Version.V3 and not Version.V4)
            throw new ArgumentException($"Unsupported version: {version}");

        DirectoryEntry entry = new();
        Read(buffer, 0, DirectoryEntry.NameFieldLength);
        int nameLength = Math.Max(0, ReadUInt16() - 2);
        entry.Name = Encoding.Unicode.GetString(buffer, 0, nameLength);
        entry.Type = ReadStorageType();
        entry.Color = ReadColor();
        entry.LeftSiblingID = ReadUInt32();
        entry.RightSiblingID = ReadUInt32();
        entry.ChildID = ReadUInt32();
        entry.CLSID = ReadGuid();
        entry.StateBits = ReadUInt32();
        entry.CreationTime = ReadFileTime();
        entry.ModifiedTime = ReadFileTime();
        entry.StartSectorLocation = ReadUInt32();

        if (version == Version.V3)
        {
            entry.StreamLength = ReadUInt32();
            if (entry.StreamLength > DirectoryEntry.MaxV3StreamLength)
                throw new FormatException($"Stream length {entry.StreamLength} exceeds maximum value {DirectoryEntry.MaxV3StreamLength}");
            ReadUInt32(); // Skip unused 4 bytes
        }
        else if (version == Version.V4)
        {
            entry.StreamLength = ReadInt64();
        }

        return entry;
    }
}
