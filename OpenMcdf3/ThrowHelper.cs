namespace OpenMcdf3;

/// <summary>
/// Extensions to consistently throw exceptions.
/// </summary>
internal static class ThrowHelper
{
    public static void ThrowIfDisposed(this object instance, bool disposed)
    {
        if (disposed)
            throw new ObjectDisposedException(instance.GetType().FullName);
    }

    public static void ThrowIfDisposed(this object instance, IOContext context)
    {
        if (context.IsDisposed)
            throw new InvalidOperationException("Root storage has been disposed");
    }
}
