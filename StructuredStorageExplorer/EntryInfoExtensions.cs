using OpenMcdf;
using System.Text;

namespace StructuredStorageExplorer;

static class EntryInfoExtensions
{
    public static EntryInfo WithEscapedControlChars(this EntryInfo entry) => entry with
    {
        Name = EscapeControlChars(entry.Name),
        Path = EscapeControlChars(entry.Path),
    };

    public static string EscapeControlChars(this string s)
    {
        StringBuilder sb = new(s.Length);
        foreach (char c in s)
        {
            if (char.IsControl(c))
                sb.Append($"\\u{ (int)c:x4}");
            else
                sb.Append(c);
        }
        return sb.ToString();
    }
}
