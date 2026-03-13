using System.Text;

namespace OpenMcdf.Explorer.Models;

static class EntryInfoExtensions
{
    public static string FriendlyName(this EntryInfo info)
    {
        string name = info.Name;

        StringBuilder sb = new(name.Length);
        foreach (char c in name)
        {
            if (char.IsControl(c))
                sb.Append($"\\u{(int)c:x4}");
            else
                sb.Append(c);
        }

        return sb.ToString();
    }
}
