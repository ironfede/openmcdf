using System.Diagnostics;
using System.Text;

namespace OpenMcdf;

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

    void ReadExactly(byte[] buffer, int offset, int count) => BaseStream.ReadExactly(buffer, offset, count);

    public Guid ReadGuid()
    {
        BaseStream.ReadExactly(guidBuffer, 0, guidBuffer.Length);

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
        ReadExactly(buffer, 0, Header.Signature.Length);
        Span<byte> signature = buffer.AsSpan(0, Header.Signature.Length);
        if (!signature.SequenceEqual(Header.Signature))
            throw new FileFormatException("Invalid header signature.");
        header.CLSID = ReadGuid();
        if (header.CLSID != Guid.Empty)
            throw new FileFormatException($"Invalid header CLSID: {header.CLSID}.");
        header.MinorVersion = ReadUInt16();
        header.MajorVersion = ReadUInt16();
        if (header.MajorVersion is not (ushort)Version.V3 and not (ushort)Version.V4)
            throw new FileFormatException($"Unsupported major version: {header.MajorVersion}.");
        else if (header.MinorVersion is not Header.ExpectedMinorVersion)
            Trace.WriteLine($"Unexpected minor version: {header.MinorVersion}.");
        ushort byteOrder = ReadUInt16();
        if (byteOrder != Header.LittleEndian)
            throw new FileFormatException($"Unsupported byte order: {byteOrder:X4}. Only little-endian is supported ({Header.LittleEndian:X4}).");
        header.SectorShift = ReadUInt16();
        header.MiniSectorShift = ReadUInt16();
        FillBuffer(6);
        header.DirectorySectorCount = ReadUInt32();
        header.FatSectorCount = ReadUInt32();
        header.FirstDirectorySectorId = ReadUInt32();
        FillBuffer(4);
        uint miniStreamCutoffSize = ReadUInt32();
        if (miniStreamCutoffSize != Header.MiniStreamCutoffSize)
            throw new FileFormatException($"Mini stream cutoff size must be {Header.MiniStreamCutoffSize} bytes.");
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
            throw new FileFormatException($"Invalid storage type: {type}.");
        return type;
    }

    public NodeColor ReadColor()
    {
        var color = (NodeColor)ReadByte();
        if (color is not NodeColor.Black and not NodeColor.Red)
            throw new FileFormatException($"Invalid node color: {color}.");
        return color;
    }

    public DirectoryEntry ReadDirectoryEntry(Version version, uint sid)
    {
        if (version is not Version.V3 and not Version.V4)
            throw new ArgumentException($"Unsupported version: {version}.", nameof(version));

        ReadExactly(buffer, 0, DirectoryEntry.NameFieldLength);

        DirectoryEntry entry = new()
        {
            Id = sid,
            NameLength = ReadUInt16(),
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

        Buffer.BlockCopy(buffer, 0, entry.Name, 0, DirectoryEntry.NameFieldLength);

        if (version == Version.V3)
        {
            entry.StreamLength = ReadUInt32();
            if (entry.StreamLength > DirectoryEntry.MaxV3StreamLength)
                throw new FileFormatException($"Stream length {entry.StreamLength} exceeds maximum value {DirectoryEntry.MaxV3StreamLength}.");
            ReadUInt32(); // Skip unused 4 bytes
        }
        else if (version == Version.V4)
        {
            entry.StreamLength = ReadInt64();
        }

        return entry;
    }
}
