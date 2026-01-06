namespace OpenMcdf;

/// <summary>
/// Provides a <see cref="IComparer{T}"/> for <see cref="DirectoryEntry"/> objects.
/// </summary>
internal sealed class DirectoryEntryComparer : IComparer<DirectoryEntry>
{
    public static DirectoryEntryComparer Default { get; } = new();

    public static int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        if (x.Length < y.Length)
            return -1;

        if (x.Length > y.Length)
            return 1;

        for (int i = 0; i < x.Length; i++)
        {
            char xChar = char.ToUpperInvariant(x[i]);
            char yChar = char.ToUpperInvariant(y[i]);

            if (xChar < yChar)
                return -1;
            if (xChar > yChar)
                return 1;
        }

        return 0;
    }

    public int Compare(DirectoryEntry? x, DirectoryEntry? y)
    {
        if (x is null && y is null)
            return 0;

        if (x is null)
            return -1;

        if (y is null)
            return 1;

        return Compare(x.NameCharSpan, y.NameCharSpan);
    }
}
