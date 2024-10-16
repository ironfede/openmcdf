namespace OpenMcdf3;


internal sealed class Header
{
    internal const int DifatLength = 109;
    internal const ushort LittleEndian = 0xFFFE;
    internal const ushort SectorShiftV3 = 0x0009;
    internal const ushort SectorShiftV4 = 0x000C;
    internal const short MiniSectorShift = 6;
    internal const uint MiniStreamCutoffSize = 4096;

    internal static readonly byte[] Signature = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

    internal static readonly byte[] Unused = new byte[6];
    private ushort majorVersion;
    private ushort sectorShift = SectorShiftV3;

    public Guid CLSID { get; set; }

    public ushort MinorVersion { get; set; }

    public ushort MajorVersion
    {
        get => majorVersion; set
        {
            if (value is not 3 and not 4)
                throw new FormatException($"Unsupported major version: {value}. Only 3 and 4 are supported");
            majorVersion = value;
        }
    }

    public ushort SectorShift
    {
        get => sectorShift; set
        {
            if (MajorVersion == 3 && value != SectorShiftV3)
                throw new FormatException($"Unsupported sector shift {value:X4}. Only {SectorShiftV3:X4} is supported for Major Version 3");
            if (MajorVersion == 4 && value != SectorShiftV4)
                throw new FormatException($"Unsupported sector shift {value:X4}. Only {SectorShiftV4:X4} is supported for Major Version 4");

            sectorShift = value;
        }
    }

    public uint DirectorySectorCount { get; set; }

    public uint FatSectorCount { get; set; }

    public uint FirstDirectorySectorId { get; set; } = SectorType.EndOfChain;

    public uint TransactionSignature { get; set; }

    /// <summary>
    /// This integer field contains the starting sector number for the mini FAT
    /// </summary>
    public uint FirstMiniFatSectorId { get; set; } = SectorType.EndOfChain;

    public uint MiniFatSectorCount { get; set; }

    public uint FirstDifatSectorId { get; set; } = SectorType.EndOfChain;

    public uint DifatSectorCount { get; set; }

    public uint[] Difat { get; } = new uint[DifatLength];

    public int SectorSize => 1 << SectorShift;

    public Header(Version version = Version.V3)
    {
        MajorVersion = (ushort)version;
        for (int i = 0; i < Difat.Length; i++)
        {
            Difat[i] = SectorType.Free;
        }
    }
}
