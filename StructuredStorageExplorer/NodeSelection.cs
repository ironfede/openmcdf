#define OLE_PROPERTY

using OpenMcdf;
using System.Globalization;

// Author Federico Blaseotto

namespace StructuredStorageExplorer;

sealed record class NodeSelection(Storage? Parent, EntryInfo EntryInfo)
{
    public string SanitizedFileName
    {
        get
        {
            // A lot of stream and storage have only non-printable characters.
            // We need to sanitize filename.

            string sanitizedFileName = string.Empty;

            foreach (char c in EntryInfo.Name)
            {
                UnicodeCategory category = char.GetUnicodeCategory(c);
                if (category is UnicodeCategory.LetterNumber or UnicodeCategory.LowercaseLetter or UnicodeCategory.UppercaseLetter)
                    sanitizedFileName += c;
            }

            if (string.IsNullOrEmpty(sanitizedFileName))
            {
                sanitizedFileName = "tempFileName";
            }

            return $"{sanitizedFileName}.bin";
        }
    }
}
