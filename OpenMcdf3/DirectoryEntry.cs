using System.Text;

namespace OpenMcdf3;

/// <summary>
/// The storage type of a <see cref="DirectoryEntry"/>.
/// </summary>
public enum StorageType
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
internal sealed class DirectoryEntry
{
    internal const int Length = 128;
    internal const int NameFieldLength = 64;
    internal const uint MaxV3StreamLength = 0x80000000;

    internal static readonly DateTime ZeroFileTime = DateTime.FromFileTimeUtc(0);

    string name = string.Empty;
    DateTime creationTime;
    DateTime modifiedTime;

    public string Name
    {
        get => name;
        set
        {
            if (value.Contains(@"\") || value.Contains(@"/") || value.Contains(@":") || value.Contains(@"!"))
                throw new ArgumentException("Name cannot contain any of the following characters: '\\', '/', ':','!'");

            if (Encoding.Unicode.GetByteCount(value) + 2 > NameFieldLength)
                throw new ArgumentException($"{value} exceeds maximum encoded length of {NameFieldLength} bytes");

            name = value;
        }
    }

    /// <summary>
    /// The type of the storage object.
    /// </summary>
    public StorageType Type { get; set; } = StorageType.Unallocated;

    public NodeColor Color { get; set; }

    /// <summary>
    /// Stream ID of the left sibling.
    /// </summary>
    public uint LeftSiblingId { get; set; }

    /// <summary>
    /// Stream ID of the right sibling.
    /// </summary>
    public uint RightSiblingId { get; set; }

    /// <summary>
    /// Stream ID of the child.
    /// </summary>
    public uint ChildId { get; set; }

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
                throw new ArgumentException("Creation time must be zero for streams and root");

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
                throw new ArgumentException("Modified time must be zero for streams");

            modifiedTime = value;
        }
    }

    /// <summary>
    /// The starting sector location for a stream or the first sector of the mini-stream for the root storage object.
    /// </summary>
    public uint StartSectorId { get; set; }

    /// <summary>
    /// The length of the stream.
    /// </summary>
    public long StreamLength { get; set; }

    public EntryInfo ToEntryInfo() => new() { Name = Name };

    public override string ToString() => Name;
}
