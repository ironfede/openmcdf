namespace OpenMcdf.Extensions.OLEProperties
{
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

    public static class WellKnownFMTID
    {
        public const string FMTID_SummaryInformation = "{F29F85E0-4FF9-1068-AB91-08002B27B3D9}";
        public const string FMTID_DocSummaryInformation = "{D5CDD502-2E9C-101B-9397-08002B2CF9AE}";
        public const string FMTID_UserDefinedProperties = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}";
        public const string FMTID_GlobalInfo = "{56616F00-C154-11CE-8553-00AA00A1F95B}";
        public const string FMTID_ImageContents = "{56616400-C154-11CE-8553-00AA00A1F95B}";
        public const string FMTID_ImageInfo = "{56616500-C154-11CE-8553-00AA00A1F95B}";
    }

    public enum PropertyDimensions
    {
        IsScalar,
        IsVector,
        IsArray
    }

    public enum PropertyType
    {
        TypedPropertyValue = 0,
        DictionaryProperty = 1
    }

    internal static class CodePages
    {
        public const int CP_WINUNICODE = 0x04B0;
    }
}
