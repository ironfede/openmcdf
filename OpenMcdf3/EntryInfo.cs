namespace OpenMcdf3;

/// <summary>
/// Encapsulates information about an entry in a <see cref="Storage"/>.
/// </summary>
public class EntryInfo
{
    public string Name { get; internal set; } = string.Empty;

    public override string ToString() => Name;
}
