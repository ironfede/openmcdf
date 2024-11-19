﻿using System.Diagnostics.CodeAnalysis;

namespace OpenMcdf;

/// <summary>
/// The structure at the beginning of a compound file.
/// </summary>
internal sealed class Header : IEquatable<Header?>
{
    internal const int DifatArrayLength = 109;
    internal const ushort ExpectedMinorVersion = 0x003E;
    internal const ushort LittleEndian = 0xFFFE;
    internal const ushort SectorShiftV3 = 0x0009;
    internal const ushort SectorShiftV4 = 0x000C;
    internal const ushort ExpectedMiniSectorShift = 6;
    internal const uint MiniStreamCutoffSize = 4096;

    /// <summary>
    /// Identification signature for the compound file structure.
    /// </summary>
    internal static readonly byte[] Signature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    internal static readonly byte[] Unused = new byte[6];

    private ushort majorVersion;
    private ushort sectorShift = SectorShiftV3;
    private ushort miniSectorShift = ExpectedMiniSectorShift;

    /// <summary>
    /// Reserved and unused class ID.
    /// </summary>
    public Guid CLSID { get; set; }

    /// <summary>
    /// Version number for non-breaking changes.
    /// </summary>
    public ushort MinorVersion { get; set; }

    /// <summary>
    /// Version number for breaking changes.
    /// </summary>
    public ushort MajorVersion
    {
        get => majorVersion; set
        {
            if (value is not 3 and not 4)
                throw new FileFormatException($"Unsupported major version: {value}. Only 3 and 4 are supported");
            majorVersion = value;
        }
    }

    /// <summary>
    /// Specifies the sector size of the compound file.
    /// </summary>
    public ushort SectorShift
    {
        get => sectorShift; set
        {
            if (MajorVersion == 3 && value != SectorShiftV3)
                throw new FileFormatException($"Unsupported sector shift {value:X4}. Only {SectorShiftV3:X4} is supported for Major Version 3.");
            if (MajorVersion == 4 && value != SectorShiftV4)
                throw new FileFormatException($"Unsupported sector shift {value:X4}. Only {SectorShiftV4:X4} is supported for Major Version 4.");

            sectorShift = value;
        }
    }

    public ushort MiniSectorShift
    {
        get => miniSectorShift;
        set
        {
            if (value != ExpectedMiniSectorShift)
                throw new FileFormatException($"Unsupported sector shift {value:X4}. Only {ExpectedMiniSectorShift:X4} is supported.");

            miniSectorShift = value;
        }
    }

    /// <summary>
    /// The number of directory sectors in the compound file (not used in V3).
    /// </summary>
    public uint DirectorySectorCount { get; set; }

    /// <summary>
    /// The number of FAT sectors in the compound file.
    /// </summary>
    public uint FatSectorCount { get; set; }

    /// <summary>
    /// The starting sector ID of the directory stream.
    /// </summary>
    public uint FirstDirectorySectorId { get; set; } = SectorType.EndOfChain;

    /// <summary>
    /// A sequence number that is incremented every time the compound file is saved by an implementation that supports file transactions.
    /// </summary>
    public uint TransactionSignature { get; set; }

    /// <summary>
    /// This integer field contains the starting sector ID of the mini FAT.
    /// </summary>
    public uint FirstMiniFatSectorId { get; set; } = SectorType.EndOfChain;

    /// <summary>
    /// The number of sectors in the mini FAT.
    /// </summary>
    public uint MiniFatSectorCount { get; set; }

    /// <summary>
    /// The starting sector ID of the DIFAT.
    /// </summary>
    public uint FirstDifatSectorId { get; set; } = SectorType.EndOfChain;

    /// <summary>
    /// The number of DIFAT sectors in the compound file.
    /// </summary>
    public uint DifatSectorCount { get; set; }

    /// <summary>
    /// An array of the first FAT sector IDs.
    /// </summary>
    public uint[] Difat { get; } = new uint[DifatArrayLength];

    public Header(Version version = Version.V3)
    {
        MajorVersion = (ushort)version;
        MinorVersion = ExpectedMinorVersion;
        SectorShift = version switch
        {
            Version.V3 => SectorShiftV3,
            Version.V4 => SectorShiftV4,
            _ => throw new FileFormatException($"Unsupported version: {version}.")
        };
        FirstDirectorySectorId = SectorType.EndOfChain;
        DirectorySectorCount = 0; // Not used in v3
        FatSectorCount = 0;
        for (int i = 0; i < Difat.Length; i++)
        {
            Difat[i] = SectorType.Free;
        }
    }

    public override int GetHashCode()
    {
        HashCode code = new();
        code.Add(CLSID);
        code.Add(MinorVersion);
        code.Add(MajorVersion);
        code.Add(SectorShift);
        code.Add(DirectorySectorCount);
        code.Add(FatSectorCount);
        code.Add(FirstDirectorySectorId);
        code.Add(TransactionSignature);
        code.Add(FatSectorCount);
        code.Add(FirstMiniFatSectorId);
        code.Add(MiniFatSectorCount);
        code.Add(FirstDifatSectorId);
        code.Add(DifatSectorCount);
        foreach (uint value in Difat)
            code.Add(value);
        return code.ToHashCode();
    }

    public override bool Equals(object? obj) => Equals(obj as Header);

    public bool Equals(Header? other)
    {
        return other is not null
            && CLSID == other.CLSID
            && MinorVersion == other.MinorVersion
            && MajorVersion == other.MajorVersion
            && SectorShift == other.SectorShift
            && DirectorySectorCount == other.DirectorySectorCount
            && FatSectorCount == other.FatSectorCount
            && FirstDirectorySectorId == other.FirstDirectorySectorId
            && TransactionSignature == other.TransactionSignature
            && FirstMiniFatSectorId == other.FirstMiniFatSectorId
            && MiniFatSectorCount == other.MiniFatSectorCount
            && FirstDifatSectorId == other.FirstDifatSectorId
            && DifatSectorCount == other.DifatSectorCount
            && Difat.SequenceEqual(other.Difat);
    }

    [ExcludeFromCodeCoverage]
    public override string ToString() => $"MajorVersion: {MajorVersion}, MinorVersion: {MinorVersion}, FirstDirectorySectorId: {FirstDirectorySectorId}, FirstMiniFatSectorId: {FirstMiniFatSectorId}";
}
