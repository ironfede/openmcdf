namespace OpenMcdf3;

/// <summary>
/// Encapsulates information about an entry in a <see cref="Storage"/>.
/// </summary>
public readonly record struct EntryInfo(string Name, long Length);
