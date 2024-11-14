namespace OpenMcdf;

public enum EntryType
{
    Storage,
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
