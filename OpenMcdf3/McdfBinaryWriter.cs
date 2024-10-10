using System.IO;
using System.Security.Claims;

namespace OpenMcdf3;

internal class McdfBinaryWriter : BinaryWriter
{
    public McdfBinaryWriter(Stream input) : base(input)
    {
    }

    public void Write(Guid value)
    {
        // TODO: Avoid heap allocation
        byte[] bytes = value.ToByteArray();
        Write(bytes, 0, bytes.Length);
    }

    public void Write(DateTime value)
    {
        long fileTime = value.ToFileTimeUtc();
        Write(fileTime);
    }

    private void WriteBytes(byte[] buffer) => Write(buffer, 0, buffer.Length);

    public void Write(Header header)
    {
        Write(Header.Signature);
        Write(header.CLSID);
        Write(header.MinorVersion);
        Write(header.MajorVersion);
        Write(Header.LittleEndian);
        Write(header.SectorShift);
        Write(Header.MiniSectorShift);
        WriteBytes(Header.Unused);
        Write(header.DirectorySectorCount);
        Write(header.FatSectorCount);
        Write(header.FirstDirectorySectorID);
        Write((uint)0);
        Write(Header.MiniStreamCutoffSize);
        Write(header.FirstMiniFatSectorID);
        Write(header.MiniFatSectorCount);
        Write(header.FirstDifatSectorID);
        Write(header.DifatSectorCount);
    }
}
