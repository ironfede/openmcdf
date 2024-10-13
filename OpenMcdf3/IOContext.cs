namespace OpenMcdf3;

internal sealed class IOContext : IDisposable
{
    public Header Header { get; }
    public McdfBinaryReader Reader { get; }
    public McdfBinaryWriter? Writer { get; }
    List<Sector> fatSectors;

    public IOContext(Header header, McdfBinaryReader reader, McdfBinaryWriter? writer = null)
    {
        Header = header;
        Reader = reader;
        Writer = writer;
    }

    public void Dispose()
    {
        Reader.Dispose();
        Writer?.Dispose();
    }

    IEnumerable<Sector> EnumerateFatSectors()
    {
        for (uint i = 0; i < Header.FatSectorCount && i < Header.DifatLength; i++)
        {
            uint nextId = Header.Difat[i];
            Sector s = new(nextId, Header.SectorSize);
            yield return s;
        }

        ChainEnumerable<Sector> iterator = new(this);
        foreach (Sector difatSector in iterator)
        {
            Reader.Seek(difatSector.StartOffset);
            int difatElementCount = Header.SectorSize / sizeof(uint) - 1;
            for (int i = 0; i < difatElementCount; i++)
            {
                uint nextId = Reader.ReadUInt32();
                Sector s = new(nextId, Header.SectorSize);
                yield return s;
            }
        }
    }

    uint GetNextFatSectorId(uint id)
    {
        int elementLength = Header.SectorSize / sizeof(uint);
        int sectorId = (int)Math.DivRem(id, elementLength, out long sectorOffset);
        fatSectors ??= EnumerateFatSectors().ToList();
        Sector sector = fatSectors[sectorId];
        long position = sector.StartOffset + sectorOffset * sizeof(uint);
        Reader.Seek(position);
        uint nextId = Reader.ReadUInt32();
        return nextId;
    }

    internal IEnumerable<Sector> EnumerateFatSectorChain(uint startId)
    {
        uint nextId = startId;
        while (nextId is not (uint)SectorType.EndOfChain and not (uint)SectorType.Free)
        {
            Sector sector = new(nextId, Header.SectorSize);
            yield return sector;
            nextId = GetNextFatSectorId(nextId);
        }
    }
}
