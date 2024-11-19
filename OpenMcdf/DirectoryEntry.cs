using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenMcdf;

/// <summary>
/// The storage type of a <see cref="DirectoryEntry"/>.
/// </summary>
enum StorageType
{
    Unallocated = 0,
    Storage = 1,
    Stream = 2,
    Root = 5
}

/// <summary>
/// Red-black node color.
/// </summary>
enum NodeColor
{
    Red = 0,
    Black = 1
}

/// <summary>
/// Stream ID constants for <see cref="DirectoryEntry"/>.
/// </summary>
internal static class StreamId
{
    public const uint Maximum = 0xFFFFFFFA;
    public const uint NoStream = 0xFFFFFFFF;
}

/// <summary>
/// Encapsulates data about a <see cref="Storage"/> or Stream.
/// </summary>
internal sealed class DirectoryEntry : IEquatable<DirectoryEntry?>
{
    internal const int Length = 128;
    internal const int NameFieldLength = 64;
    internal const uint MaxV3StreamLength = 0x80000000;

    internal static readonly DateTime ZeroFileTime = DateTime.FromFileTimeUtc(0);

    internal static readonly byte[] Unallocated = new byte[128];

    DateTime creationTime;
    DateTime modifiedTime;

    public uint Id { get; set; }

    public byte[] Name { get; } = new byte[NameFieldLength];

    public ushort NameLength { get; set; }

    /// <summary>
    /// The type of the storage object.
    /// </summary>
    public StorageType Type { get; set; } = StorageType.Unallocated;

    public NodeColor Color { get; set; }

    /// <summary>
    /// Stream ID of the left sibling.
    /// </summary>
    public uint LeftSiblingId { get; set; } = StreamId.NoStream;

    /// <summary>
    /// Stream ID of the right sibling.
    /// </summary>
    public uint RightSiblingId { get; set; } = StreamId.NoStream;

    /// <summary>
    /// Stream ID of the child.
    /// </summary>
    public uint ChildId { get; set; } = StreamId.NoStream;

    /// <summary>
    /// GUID for storage objects.
    /// </summary>
    public Guid CLSID { get; set; }

    /// <summary>
    /// User defined flags for storage objects.
    /// </summary>
    public uint StateBits { get; set; }

    /// <summary>
    /// The creation time of the storage object.
    /// </summary>
    public DateTime CreationTime
    {
        get => creationTime;
        set
        {
            if (Type is StorageType.Stream or StorageType.Root && value != ZeroFileTime)
                throw new ArgumentException("Creation time must be zero for streams and root.", nameof(value));

            creationTime = value;
        }
    }

    /// <summary>
    /// The modified time of the storage object.
    /// </summary>
    public DateTime ModifiedTime
    {
        get => modifiedTime;
        set
        {
            if (Type is StorageType.Stream && value != ZeroFileTime)
                throw new ArgumentException("Modified time must be zero for streams.", nameof(value));

            modifiedTime = value;
        }
    }

    /// <summary>
    /// The starting sector location for a stream or the first sector of the mini-stream for the root storage object.
    /// </summary>
    public uint StartSectorId { get; set; } = StreamId.NoStream;

    /// <summary>
    /// The length of the stream.
    /// </summary>
    public long StreamLength { get; set; }

    internal char ColorChar => Color switch
    {
        NodeColor.Red => 'R',
        NodeColor.Black => 'B',
        _ => '?'
    };

    public ReadOnlySpan<byte> NameByteSpan
    {
        get
        {
            int clampedNameLength = Math.Max(0, Math.Min(NameFieldLength, NameLength - 2));
            return Name.AsSpan(0, clampedNameLength);
        }
    }

    public ReadOnlySpan<char> NameCharSpan => MemoryMarshal.Cast<byte, char>(NameByteSpan);

    public string NameString
    {
        get
        {
            int clampedNameLength = Math.Max(0, Math.Min(NameFieldLength, NameLength - 2));
            return Encoding.Unicode.GetString(Name, 0, clampedNameLength);
        }
        set => NameLength = (ushort)(Encoding.Unicode.GetBytes(value, 0, value.Length, Name, 0) + 2);
    }

    public override int GetHashCode()
    {
        HashCode code = new();
        code.Add(Id);
        code.Add(NameLength);
        foreach (byte b in Name)
            code.Add(b);
        return code.GetHashCode();
    }

    public override bool Equals(object? obj) => Equals(obj as DirectoryEntry);

    public bool Equals(DirectoryEntry? other)
    {
        return other is not null
            && Id == other.Id
            && Name.SequenceEqual(other.Name)
            && NameLength == other.NameLength
            && Type == other.Type
            && Color == other.Color
            && LeftSiblingId == other.LeftSiblingId
            && RightSiblingId == other.RightSiblingId
            && ChildId == other.ChildId
            && CLSID == other.CLSID
            && StateBits == other.StateBits
            && CreationTime == other.CreationTime
            && ModifiedTime == other.ModifiedTime
            && StartSectorId == other.StartSectorId
            && StreamLength == other.StreamLength;
    }

    public void RecycleRoot() => Recycle(StorageType.Root, "Root Entry");

    public void Recycle() => Recycle(StorageType.Unallocated, string.Empty);

    public void Recycle(StorageType storageType, string name)
    {
        Type = storageType;
        NameString = name;
        Color = NodeColor.Black;
        LeftSiblingId = StreamId.NoStream;
        RightSiblingId = StreamId.NoStream;
        ChildId = StreamId.NoStream;
        StartSectorId = StreamId.NoStream;
        StreamLength = 0;

        if (storageType is StorageType.Root)
        {
            CreationTime = ZeroFileTime;
            ModifiedTime = DateTime.UtcNow;
        }
        else if (storageType is StorageType.Storage)
        {
            DateTime now = DateTime.UtcNow;
            CreationTime = now;
            ModifiedTime = now;
        }
        else
        {
            CreationTime = ZeroFileTime;
            ModifiedTime = ZeroFileTime;
        }
    }

    public EntryType EntryType => Type switch
    {
        StorageType.Stream => EntryType.Stream,
        StorageType.Storage => EntryType.Storage,
        StorageType.Root => EntryType.Storage,
        _ => throw new FileFormatException($"Invalid storage type: {Type}.")
    };

    public EntryInfo ToEntryInfo(string path) => new(EntryType, path, NameString, StreamLength, CLSID, CreationTime, ModifiedTime);

    [ExcludeFromCodeCoverage]
    public override string ToString() => $"{Id}: \"{NameString}\"";

    public DirectoryEntry Clone()
    {
        DirectoryEntry clone = new()
        {
            Id = Id,
            NameLength = NameLength,
            Type = Type,
            Color = Color,
            LeftSiblingId = LeftSiblingId,
            RightSiblingId = RightSiblingId,
            ChildId = ChildId,
            CLSID = CLSID,
            StateBits = StateBits,
            CreationTime = CreationTime,
            ModifiedTime = ModifiedTime,
            StartSectorId = StreamId.NoStream,
            StreamLength = 0
        };

        Array.Copy(Name, clone.Name, Name.Length);

        return clone;
    }
}
