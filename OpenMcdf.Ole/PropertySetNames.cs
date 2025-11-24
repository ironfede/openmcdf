namespace OpenMcdf.Ole;

/// <summary>
/// Some well known property set names.
/// </summary>
/// <remarks>
/// As defined at https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-oleps/e5484a83-3cc1-43a6-afcf-6558059fe36e.
/// </remarks>
public static class PropertySetNames
{
    public const string SummaryInformation = "\u0005SummaryInformation";
    public const string DocSummaryInformation = "\u0005DocumentSummaryInformation";
    public const string GlobalInfo = "\u0005GlobalInfo";
    public const string ImageContents = "\u000505ImageContents";
    public const string ImageInfo = "\u0005ImageInfo";
}
