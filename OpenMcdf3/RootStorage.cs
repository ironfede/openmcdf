using System.Diagnostics;

namespace OpenMcdf3;

public enum Version : ushort
{
    V3 = 3,
    V4 = 4
}

public sealed class RootStorage : Storage, IDisposable
{
    readonly Header header;
    readonly McdfBinaryReader reader;
    readonly McdfBinaryWriter? writer;
    bool disposed;

    internal McdfBinaryReader Reader => reader;

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
        return Open(stream);
    }

    public static RootStorage Open(Stream stream)
    {
        McdfBinaryReader reader = new(stream);
        McdfBinaryWriter? writer = stream.CanWrite ? new(stream) : null;
        Header header = reader.ReadHeader();
        return new RootStorage(header, reader, writer);
    }

    RootStorage(Header header, McdfBinaryReader reader, McdfBinaryWriter? writer = null)
    {
        this.header = header;
        this.reader = reader;
        this.writer = writer;
    }

    public void Dispose()
    {
        if (disposed)
            return;

        writer?.Dispose();
        reader.Dispose();
        disposed = true;
    }

    IEnumerable<Sector> EnumerateDifatSectorChain()
    {
        uint nextId = header.FirstDifatSectorID;
        while (nextId != (uint)SectorType.EndOfChain)
        {
            Sector s = new(nextId, header.SectorSize);
            yield return s;
            long nextIdOffset = s.EndOffset - sizeof(uint);
            reader.Seek(nextIdOffset);
            nextId = reader.ReadUInt32();
        }
    }

    IEnumerable<Sector> EnumerateFatSectors()
    {
        for (uint i = 0; i < header.FatSectorCount && i < Header.DifatLength; i++)
        {
            uint nextId = header.Difat[i];
            Sector s = new(nextId, header.SectorSize);
            yield return s;
        }

        foreach (Sector difatSector in EnumerateDifatSectorChain())
        {
            reader.Seek(difatSector.StartOffset);
            int difatElementCount = header.SectorSize / sizeof(uint) - 1;
            for (int i = 0; i < difatElementCount; i++)
            {
                uint nextId = reader.ReadUInt32();
                Sector s = new(nextId, header.SectorSize);
                yield return s;
            }
        }
    }

    uint GetNextFatSectorId(uint id)
    {
        int elementLength = header.SectorSize / sizeof(uint);
        int sectorId = (int)Math.DivRem(id, elementLength, out long sectorOffset);
        Sector sector = EnumerateFatSectors().ElementAt(sectorId);
        long position = sector.StartOffset + sectorOffset * sizeof(uint);
        reader.Seek(position);
        uint nextId = reader.ReadUInt32();
        return nextId;
    }

    internal IEnumerable<Sector> EnumerateFatSectorChain(uint startId)
    {
        uint nextId = startId;
        while (nextId is not (uint)SectorType.EndOfChain and not (uint)SectorType.Free)
        {
            Sector sector = new(nextId, header.SectorSize);
            yield return sector;
            nextId = GetNextFatSectorId(nextId);
        }
    }

    IEnumerable<DirectoryEntry> EnumerateDirectoryEntries()
    {
        foreach (Sector sector in EnumerateFatSectorChain(header.FirstDirectorySectorID))
        {
            reader.Seek(sector.StartOffset);

            int entryCount = header.SectorSize / DirectoryEntry.Length;
            for (int i = 0; i < entryCount; i++)
            {
                DirectoryEntry entry = reader.ReadDirectoryEntry((Version)header.MajorVersion);
                if (entry.Type is not StorageType.Invalid)
                    yield return entry;
            }
        }
    }

    public IEnumerable<EntryInfo> EnumerateEntries() => EnumerateDirectoryEntries().Select(e => new EntryInfo { Name = e.Name });

    public CfbStream OpenStream(string name)
    {
        DirectoryEntry? entry = EnumerateDirectoryEntries()
            .FirstOrDefault(entry => entry.Name == name) ?? throw new FileNotFoundException("Stream not found", name);
        return new CfbStream(this, header.SectorSize, entry);
    }
}
