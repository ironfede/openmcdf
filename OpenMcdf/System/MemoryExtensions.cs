namespace System;

internal static class MemoryExtensions
{
    public static bool Contains<T>(this ReadOnlySpan<T> span, T value)
        where T : IEquatable<T>
    {
        foreach (T other in span)
        {
            if (value.Equals(other))
                return true;
        }

        return false;
    }

    public static bool ContainsAny<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> values)
        where T : IEquatable<T>
    {
        foreach (T value in values)
        {
            if (span.Contains(value))
                return true;
        }

        return false;
    }
}
