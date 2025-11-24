namespace OpenMcdf;

/// <summary>
/// Adds modern extension methods to the <see cref="Stream"/> class for netstandard2.0.
/// </summary>
internal static class StreamExtensions
{
#if !NET7_0_OR_GREATER

    public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int count)
    {
        if (count == 0)
            return;

        int totalRead = 0;
        do
        {
            int read = stream.Read(buffer, offset + totalRead, count - totalRead);
            if (read == 0)
                throw new EndOfStreamException();

            totalRead += read;
        } while (totalRead < count);
    }

#endif

#if !NETSTANDARD2_0 && !NETFRAMEWORK

    public static int ReadByteCore(this Stream stream)
    {
        Span<byte> bytes = stackalloc byte[1];
        int read = stream.Read(bytes);
        return read == 0 ? -1 : bytes[0];
    }

    public static void WriteByteCore(this Stream stream, byte value)
    {
        stream.ThrowIfNotWritable();

        ReadOnlySpan<byte> bytes = [value];
        stream.Write(bytes);
    }

#endif

    public static void CopyAllTo(this Stream source, Stream destination)
    {
        source.Position = 0;
        destination.Position = 0;
        destination.SetLength(source.Length);
        source.CopyTo(destination);
        destination.Position = 0;
    }
}
