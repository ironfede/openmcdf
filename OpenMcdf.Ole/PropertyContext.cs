namespace OpenMcdf.Ole;

public enum Behavior
{
    CaseSensitive,
    CaseInsensitive,
}

public sealed class PropertyContext
{
    public int CodePage { get; set; }
    public Behavior Behavior { get; set; }
    public uint Locale { get; set; }
}
