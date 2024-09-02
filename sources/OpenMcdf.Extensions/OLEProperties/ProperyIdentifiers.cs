using System;
using System.Collections.Generic;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    public enum ContainerType
    {
        AppSpecific = 0,
        SummaryInfo = 1,
        DocumentSummaryInfo = 2,
        UserDefinedProperties = 3,
        GlobalInfo = 4,
        ImageContents = 5,
        ImageInfo = 6
    }

    public static class CommonIdentifiers
    {

        public static Dictionary<uint, string> PropertyIdentifiersSummaryInfo = new Dictionary<uint, string>()
        {
            {0x00000001,"CodePageString" },
            {0x00000002,"PIDSI_TITLE" },
            {0x00000003,"PIDSI_SUBJECT" },
            {0x00000004,"PIDSI_AUTHOR" },
            {0x00000005,"PIDSI_KEYWORDS" },
            {0x00000006,"PIDSI_COMMENTS" },
            {0x00000007,"PIDSI_TEMPLATE" },
            {0x00000008,"PIDSI_LASTAUTHOR" },
            {0x00000009,"PIDSI_REVNUMBER" },
            {0x00000012,"PIDSI_APPNAME" },
            {0x0000000A,"PIDSI_EDITTIME" },
            {0x0000000B,"PIDSI_LASTPRINTED" },
            {0x0000000C,"PIDSI_CREATE_DTM" },
            {0x0000000D,"PIDSI_LASTSAVE_DTM" },
            {0x0000000E,"PIDSI_PAGECOUNT" },
            {0x0000000F,"PIDSI_WORDCOUNT" },
            {0x00000010,"PIDSI_CHARCOUNT" },
            {0x00000013,"PIDSI_DOC_SECURITY" }
        };

        public static Dictionary<uint, string> PropertyIdentifiersDocumentSummaryInfo = new Dictionary<uint, string>()
        {
            {0x00000001,"CodePageString" },
            {0x00000002,"PIDDSI_CATEGORY" },
            {0x00000003,"PIDDSI_PRESFORMAT" },
            {0x00000004,"PIDDSI_BYTECOUNT" },
            {0x00000005,"PIDDSI_LINECOUNT" },
            {0x00000006,"PIDDSI_PARCOUNT" },
            {0x00000007,"PIDDSI_SLIDECOUNT" },
            {0x00000008,"PIDDSI_NOTECOUNT" },
            {0x00000009,"PIDDSI_HIDDENCOUNT" },
            {0x0000000A,"PIDDSI_MMCLIPCOUNT" },
            {0x0000000B,"PIDDSI_SCALE" },
            {0x0000000C,"PIDDSI_HEADINGPAIR" },
            {0x0000000D,"PIDDSI_DOCPARTS" },
            {0x0000000E,"PIDDSI_MANAGER" },
            {0x0000000F,"PIDDSI_COMPANY" },
            {0x00000010,"PIDDSI_LINKSDIRTY" }

        };
    }

    public static class Extensions
    {
        public static string GetDescription(this uint identifier, ContainerType map, Dictionary<uint, string> customDict = null)
        {
            Dictionary<uint, string> nameDictionary = customDict;

            if (nameDictionary is null)
            {
                switch (map)
                {
                    case ContainerType.SummaryInfo:
                        nameDictionary = CommonIdentifiers.PropertyIdentifiersSummaryInfo;
                        break;
                    case ContainerType.DocumentSummaryInfo:
                        nameDictionary = CommonIdentifiers.PropertyIdentifiersDocumentSummaryInfo;
                        break;
                }
            }

            if (nameDictionary?.TryGetValue(identifier, out string value) == true)
            {
                return value;
            }

            return "0x" + identifier.ToString("x8");
        }


    }
}