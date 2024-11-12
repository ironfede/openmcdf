namespace OpenMcdf.Ole;

public enum Behavior
{
    CaseSensitive,
    CaseInsensitive
}

public class PropertyContext
{
    public int CodePage { get; set; }
    public Behavior Behavior { get; set; }
    public uint Locale { get; set; }
}
