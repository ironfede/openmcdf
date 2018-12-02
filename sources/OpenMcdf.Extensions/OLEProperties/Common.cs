using System;
using System.Collections.Generic;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    public enum Behavior
    {
        CaseSensitive, CaseInsensitive
    }

    public class PropertyContext
    {

        public Int32 CodePage { get; set; }
        public Behavior Behavior { get; set; }
        public UInt32 Locale { get; set; }
    }

    public static class WellKnownFMTID
    {
        public static string FMTID_SummaryInformation = "{F29F85E0-4FF9-1068-AB91-08002B27B3D9}";
        public static string FMTID_DocSummaryInformation = "{D5CDD502-2E9C-101B-9397-08002B2CF9AE}";
        public static string FMTID_UserDefinedProperties = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}";
        public static string FMTID_GlobalInfo = "{56616F00-C154-11CE-8553-00AA00A1F95B}";
        public static string FMTID_ImageContents = "{56616400-C154-11CE-8553-00AA00A1F95B}";
        public static string FMTID_ImageInfo = "{56616500-C154-11CE-8553-00AA00A1F95B}";
    }

    public enum PropertyDimensions
    {
        IsScalar, IsVector, IsArray
    }

    public enum PropertyType
    {
        TypedPropertyValue = 0, DictionaryProperty = 1
    }
}
