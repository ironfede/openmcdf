using System.Text;

namespace OpenMcdf3;

internal sealed class McdfBinaryReader : BinaryReader
{
    readonly byte[] buffer = new byte[DirectoryEntry.NameFieldLength];

    public McdfBinaryReader(Stream input)
        : base(input, Encoding.Unicode, true)
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
            throw new FormatException("Invalid header signature");
        header.CLSID = ReadGuid();
        if (header.CLSID != Guid.Empty)
            throw new FormatException($"Invalid header CLSID: {header.CLSID}");
        header.MinorVersion = ReadUInt16();
        header.MajorVersion = ReadUInt16();
        if (header.MajorVersion is not (ushort)Version.V3 and not (ushort)Version.V4)
            throw new FormatException($"Unsupported major version: {header.MajorVersion}");
        else if (header.MinorVersion is not Header.ExpectedMinorVersion)
            throw new FormatException($"Unsupported minor version: {header.MinorVersion}");
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
        header.FirstDirectorySectorId = ReadUInt32();
        this.FillBuffer(4);
        uint miniStreamCutoffSize = ReadUInt32();
        if (miniStreamCutoffSize != Header.MiniStreamCutoffSize)
            throw new FormatException("Mini stream cutoff size must be 4096 byte");
        header.FirstMiniFatSectorId = ReadUInt32();
        header.MiniFatSectorCount = ReadUInt32();
        header.FirstDifatSectorId = ReadUInt32();
        header.DifatSectorCount = ReadUInt32();

        for (int i = 0; i < Header.DifatLength; i++)
        {
            header.Difat[i] = ReadUInt32();
        }

        return header;
    }

    public StorageType ReadStorageType()
    {
        var type = (StorageType)ReadByte();
        if (type is not StorageType.Storage and not StorageType.Stream and not StorageType.Root and not StorageType.Unallocated)
            throw new FormatException($"Invalid storage type: {type}");
        return type;
    }

    public NodeColor ReadColor()
    {
        var color = (NodeColor)ReadByte();
        if (color is not NodeColor.Black and not NodeColor.Red)
            throw new FormatException($"Invalid node color: {color}");
        return color;
    }

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
        entry.LeftSiblingId = ReadUInt32();
        entry.RightSiblingId = ReadUInt32();
        entry.ChildId = ReadUInt32();
        entry.CLSID = ReadGuid();
        entry.StateBits = ReadUInt32();
        entry.CreationTime = ReadFileTime();
        entry.ModifiedTime = ReadFileTime();
        entry.StartSectorId = ReadUInt32();

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

    public void Seek(long offset) => BaseStream.Seek(offset, SeekOrigin.Begin);
}
