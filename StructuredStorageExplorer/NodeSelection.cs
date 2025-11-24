using OpenMcdf;
using System.Text;

namespace StructuredStorageExplorer;

sealed record class NodeSelection(Storage? Parent, EntryInfo EntryInfo)
{
    /// <summary>
    /// Gets fileName derived from the EntryInfo.Name, with invalid filename chars replaces.
    /// </summary>
    public string SanitizedFileName
    {
        get
        {
            StringBuilder builder = new(EntryInfo.Name.Length + 4);
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in EntryInfo.Name)
            {
                char validChar = invalidChars.Contains(c) ? '_' : c;
                builder.Append(validChar);
            }

            builder.Append(".bin");
            return builder.ToString();
        }
    }
}
