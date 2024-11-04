using System.Text;

namespace OpenMcdf3;

/// <summary>
/// Reads CFB data types from a stream.
/// </summary>
internal sealed class CfbBinaryReader : BinaryReader
{
    readonly byte[] guidBuffer = new byte[16];
    readonly byte[] buffer = new byte[DirectoryEntry.NameFieldLength];

    public CfbBinaryReader(Stream input)
        : base(input, Encoding.Unicode, true)
    {
    }

    public long Position
    {
        get => BaseStream.Position;
        set => BaseStream.Position = value;
    }

    public Guid ReadGuid()
    {
        int bytesRead = 0;
        do
        {
            int n = Read(guidBuffer, bytesRead, guidBuffer.Length - bytesRead);
            if (n == 0)
                throw new EndOfStreamException();
            bytesRead += n;
        } while (bytesRead < guidBuffer.Length);

        return new Guid(guidBuffer);
    }

    public DateTime ReadFileTime()
    {
        long fileTime = ReadInt64();
        return DateTime.FromFileTimeUtc(fileTime);
    }

    public Header ReadHeader()
    {
        Header header = new();
        Read(buffer, 0, Header.Signature.Length);
        if (!buffer.Take(Header.Signature.Length).SequenceEqual(Header.Signature))
            throw new FormatException("Invalid header signature.");
        header.CLSID = ReadGuid();
        if (header.CLSID != Guid.Empty)
            throw new FormatException($"Invalid header CLSID: {header.CLSID}.");
        header.MinorVersion = ReadUInt16();
        header.MajorVersion = ReadUInt16();
        if (header.MajorVersion is not (ushort)Version.V3 and not (ushort)Version.V4)
            throw new FormatException($"Unsupported major version: {header.MajorVersion}.");
        else if (header.MinorVersion is not Header.ExpectedMinorVersion)
            throw new FormatException($"Unsupported minor version: {header.MinorVersion}.");
        ushort byteOrder = ReadUInt16();
        if (byteOrder != Header.LittleEndian)
            throw new FormatException($"Unsupported byte order: {byteOrder:X4}. Only little-endian is supported ({Header.LittleEndian:X4}).");
        header.SectorShift = ReadUInt16();
        header.MiniSectorShift = ReadUInt16();
        this.FillBuffer(6);
        header.DirectorySectorCount = ReadUInt32();
        header.FatSectorCount = ReadUInt32();
        header.FirstDirectorySectorId = ReadUInt32();
        this.FillBuffer(4);
        uint miniStreamCutoffSize = ReadUInt32();
        if (miniStreamCutoffSize != Header.MiniStreamCutoffSize)
            throw new FormatException($"Mini stream cutoff size must be {Header.MiniStreamCutoffSize} bytes.");
        header.FirstMiniFatSectorId = ReadUInt32();
        header.MiniFatSectorCount = ReadUInt32();
        header.FirstDifatSectorId = ReadUInt32();
        header.DifatSectorCount = ReadUInt32();

        for (int i = 0; i < Header.DifatArrayLength; i++)
        {
            header.Difat[i] = ReadUInt32();
        }

        return header;
    }

    public StorageType ReadStorageType()
    {
        var type = (StorageType)ReadByte();
        if (type is not StorageType.Storage and not StorageType.Stream and not StorageType.Root and not StorageType.Unallocated)
            throw new FormatException($"Invalid storage type: {type}.");
        return type;
    }

    public NodeColor ReadColor()
    {
        var color = (NodeColor)ReadByte();
        if (color is not NodeColor.Black and not NodeColor.Red)
            throw new FormatException($"Invalid node color: {color}.");
        return color;
    }

    public DirectoryEntry ReadDirectoryEntry(Version version, uint sid)
    {
        if (version is not Version.V3 and not Version.V4)
            throw new ArgumentException($"Unsupported version: {version}.", nameof(version));

        Read(buffer, 0, DirectoryEntry.NameFieldLength); // TODO
        ushort nameLength = ReadUInt16();
        int clampedNameLength = Math.Max(0, Math.Min(DirectoryEntry.NameFieldLength, nameLength - 2));
        string name = Encoding.Unicode.GetString(buffer, 0, clampedNameLength);

        DirectoryEntry entry = new()
        {
            Id = sid,
            Name = name,
            Type = ReadStorageType(),
            Color = ReadColor(),
            LeftSiblingId = ReadUInt32(),
            RightSiblingId = ReadUInt32(),
            ChildId = ReadUInt32(),
            CLSID = ReadGuid(),
            StateBits = ReadUInt32(),
            CreationTime = ReadFileTime(),
            ModifiedTime = ReadFileTime(),
            StartSectorId = ReadUInt32()
        };

        if (version == Version.V3)
        {
            entry.StreamLength = ReadUInt32();
            if (entry.StreamLength > DirectoryEntry.MaxV3StreamLength)
                throw new FormatException($"Stream length {entry.StreamLength} exceeds maximum value {DirectoryEntry.MaxV3StreamLength}.");
            ReadUInt32(); // Skip unused 4 bytes
        }
        else if (version == Version.V4)
        {
            entry.StreamLength = ReadInt64();
        }

        return entry;
    }
}
