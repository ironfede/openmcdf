namespace OpenMcdf;

/// <summary>
/// Specifies the type of entry for a <see cref="EntryInfo"/>.
/// </summary>
public enum EntryType
{
    /// <summary>
    /// A storage entry, which may contain other entries.
    /// </summary>
    Storage,

    /// <summary>
    /// A stream entry containing binary data.
    /// </summary>
    Stream,
}

/// <summary>
/// Encapsulates information about an entry in a <see cref="Storage"/>.
/// </summary>
public readonly record struct EntryInfo(
    EntryType Type,
    string Path,
    string Name,
    long Length,
    Guid CLSID,
    DateTime CreationTime,
    DateTime ModifiedTime);
