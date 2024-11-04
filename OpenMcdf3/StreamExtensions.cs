using System.Buffers;

namespace OpenMcdf3;

#if !NET7_0_OR_GREATER

internal static class StreamExtensions
{
    public static void ReadExactly(this Stream stream, Span<byte> buffer)
    {
        byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            stream.ReadExactly(array, 0, buffer.Length);
            array.AsSpan(0, buffer.Length).CopyTo(buffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

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
}

#endif
