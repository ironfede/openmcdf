using System;
using System.Collections.Generic;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    public enum SummaryInfoMap
    {
        SummaryInfo = 0, DocumentSummaryInfo = 1
    }



    public enum PropertyIdentifiersSummaryInfo : uint
    {
        CodePageString = 0x00000001,
        PIDSI_TITLE = 0x00000002,
        PIDSI_SUBJECT = 0x00000003,
        PIDSI_AUTHOR = 0x00000004,
        PIDSI_KEYWORDS = 0x00000005,
        PIDSI_COMMENTS = 0x00000006,
        PIDSI_TEMPLATE = 0x00000007,
        PIDSI_LASTAUTHOR = 0x00000008,
        PIDSI_REVNUMBER = 0x00000009,
        PIDSI_APPNAME = 0x00000012,
        PIDSI_EDITTIME = 0x0000000A,
        PIDSI_LASTPRINTED = 0x0000000B,
        PIDSI_CREATE_DTM = 0x0000000C,
        PIDSI_LASTSAVE_DTM = 0x0000000D,
        PIDSI_PAGECOUNT = 0x0000000E,
        PIDSI_WORDCOUNT = 0x0000000F,
        PIDSI_CHARCOUNT = 0x00000010,
        PIDSI_DOC_SECURITY = 0x00000013
    }

    public enum PropertyIdentifiersDocumentSummaryInfo : uint
    {
        CodePageString = 0x00000001,
        PIDDSI_CATEGORY = 0x00000002, //Category VT_LPSTR
        PIDDSI_PRESFORMAT = 0x00000003,//PresentationTarget	VT_LPSTR
        PIDDSI_BYTECOUNT = 0x00000004,//Bytes   	VT_I4
        PIDDSI_LINECOUNT = 0x00000005,// Lines   	VT_I4
        PIDDSI_PARCOUNT = 0x00000006,// Paragraphs 	VT_I4
        PIDDSI_SLIDECOUNT = 0x00000007,// Slides 	VT_I4
        PIDDSI_NOTECOUNT = 0x00000008,// Notes  	VT_I4
        PIDDSI_HIDDENCOUNT = 0x00000009,// HiddenSlides   	VT_I4
        PIDDSI_MMCLIPCOUNT = 0x0000000A,// MMClips	VT_I4
        PIDDSI_SCALE = 0x0000000B,//ScaleCrop  VT_BOOL
        PIDDSI_HEADINGPAIR = 0x0000000C,// HeadingPairs VT_VARIANT | VT_VECTOR
        PIDDSI_DOCPARTS = 0x0000000D,//TitlesofParts   	VT_VECTOR | VT_LPSTR
        PIDDSI_MANAGER = 0x0000000E,//	  Manager VT_LPSTR
        PIDDSI_COMPANY = 0x0000000F,// Company	VT_LPSTR
        PIDDSI_LINKSDIRTY = 0x00000010,//LinksUpToDate   	VT_BOOL
    }

    public static class Extensions
    {
        public static String GetDescription(this uint identifier, SummaryInfoMap map)
        {
            switch (map)
            {
                case SummaryInfoMap.SummaryInfo:

                    PropertyIdentifiersSummaryInfo id1 = (PropertyIdentifiersSummaryInfo)identifier;

                    switch (id1)
                    {
                        case PropertyIdentifiersSummaryInfo.CodePageString:
                            return "CodePage";
                        case PropertyIdentifiersSummaryInfo.PIDSI_TITLE:
                            return "Title";
                        case PropertyIdentifiersSummaryInfo.PIDSI_SUBJECT:
                            return "Subject";
                        case PropertyIdentifiersSummaryInfo.PIDSI_AUTHOR:
                            return "Author";
                        case PropertyIdentifiersSummaryInfo.PIDSI_LASTAUTHOR:
                            return "Last Author";
                        case PropertyIdentifiersSummaryInfo.PIDSI_APPNAME:
                            return "Application Name";
                        case PropertyIdentifiersSummaryInfo.PIDSI_CREATE_DTM:
                            return "Create Time";
                        case PropertyIdentifiersSummaryInfo.PIDSI_LASTSAVE_DTM:
                            return "Last Modified Time";
                        case PropertyIdentifiersSummaryInfo.PIDSI_KEYWORDS:
                            return "Keywords";
                        case PropertyIdentifiersSummaryInfo.PIDSI_DOC_SECURITY:
                            return "Document Security";
                        default: return String.Empty;
                    }
                    break;

                case SummaryInfoMap.DocumentSummaryInfo:
                    PropertyIdentifiersDocumentSummaryInfo id2 = (PropertyIdentifiersDocumentSummaryInfo)identifier;

                    switch (id2)
                    {
                        case PropertyIdentifiersDocumentSummaryInfo.CodePageString:
                            return "CodePage";
                        case PropertyIdentifiersDocumentSummaryInfo.PIDDSI_CATEGORY:
                            return "Category";
                        case PropertyIdentifiersDocumentSummaryInfo.PIDDSI_COMPANY:
                            return "Company";
                        case PropertyIdentifiersDocumentSummaryInfo.PIDDSI_DOCPARTS:
                            return "Titles of Parts";
                        case PropertyIdentifiersDocumentSummaryInfo.PIDDSI_HEADINGPAIR:
                            return "Heading Pairs";
                        case PropertyIdentifiersDocumentSummaryInfo.PIDDSI_HIDDENCOUNT:
                            return "Hidden Slides";
                        case PropertyIdentifiersDocumentSummaryInfo.PIDDSI_LINECOUNT:
                            return "Line Count";
                        case PropertyIdentifiersDocumentSummaryInfo.PIDDSI_LINKSDIRTY:
                            return "Links up to date";
                        case PropertyIdentifiersDocumentSummaryInfo.PIDDSI_MANAGER:
                            return "Manager";
                        case PropertyIdentifiersDocumentSummaryInfo.PIDDSI_MMCLIPCOUNT:
                            return "MMClips";
                        case PropertyIdentifiersDocumentSummaryInfo.PIDDSI_NOTECOUNT:
                            return "Notes";
                        case PropertyIdentifiersDocumentSummaryInfo.PIDDSI_PARCOUNT:
                            return "Paragraphs";
                        case PropertyIdentifiersDocumentSummaryInfo.PIDDSI_PRESFORMAT:
                            return "Presenteation Target";
                        case PropertyIdentifiersDocumentSummaryInfo.PIDDSI_SCALE:
                            return "Scale";
                        case PropertyIdentifiersDocumentSummaryInfo.PIDDSI_SLIDECOUNT:
                            return "Slides";
                        default: return String.Empty;
                    }

                    break;

            }

            return string.Empty;
        }


    }
}
