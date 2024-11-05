using System.Text;

namespace OpenMcdf3;

/// <summary>
/// Writes CFB data types to a stream.
/// </summary>
internal sealed class CfbBinaryWriter : BinaryWriter
{
    readonly byte[] buffer = new byte[DirectoryEntry.NameFieldLength];

    public CfbBinaryWriter(Stream input)
        : base(input, Encoding.Unicode, true)
    {
    }

    public long Position
    {
        get => BaseStream.Position;
        set => BaseStream.Position = value;
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER

    public override void Write(ReadOnlySpan<byte> buffer) => BaseStream.Write(buffer);

    public override void Write(byte value)
    {
        Span<byte> localBuffer = stackalloc byte[1] { value };
        Write(localBuffer);
    }

#endif

    public void Write(in Guid value)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        Span<byte> localBuffer = stackalloc byte[16];
        value.TryWriteBytes(localBuffer);
        Write(localBuffer);
#else
        byte[] bytes = value.ToByteArray();
        Write(bytes);
#endif
    }

    public void Write(DateTime value)
    {
        long fileTime = value.ToFileTimeUtc();
        Write(fileTime);
    }

    public void Write(Header header)
    {
        Write(Header.Signature);
        Write(header.CLSID);
        Write(header.MinorVersion);
        Write(header.MajorVersion);
        Write(Header.LittleEndian);
        Write(header.SectorShift);
        Write(header.MiniSectorShift);
        Write(Header.Unused);
        Write(header.DirectorySectorCount);
        Write(header.FatSectorCount);
        Write(header.FirstDirectorySectorId);
        Write((uint)0);
        Write(Header.MiniStreamCutoffSize);
        Write(header.FirstMiniFatSectorId);
        Write(header.MiniFatSectorCount);
        Write(header.FirstDifatSectorId);
        Write(header.DifatSectorCount);
        for (int i = 0; i < Header.DifatArrayLength; i++)
            Write(header.Difat[i]);
    }

    public void Write(DirectoryEntry entry)
    {
        buffer.AsSpan().Clear();
        int nameLength = Encoding.Unicode.GetBytes(entry.Name, 0, entry.Name.Length, buffer, 0);
        Write(buffer, 0, DirectoryEntry.NameFieldLength);
        Write((short)(nameLength + 2));
        Write((byte)entry.Type);
        Write((byte)entry.Color);
        Write(entry.LeftSiblingId);
        Write(entry.RightSiblingId);
        Write(entry.ChildId);
        Write(entry.CLSID);
        Write(entry.StateBits);
        Write(entry.CreationTime);
        Write(entry.ModifiedTime);
        Write(entry.StartSectorId);
        Write(entry.StreamLength);
    }
}
