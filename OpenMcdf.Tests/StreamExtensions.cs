#if !NET7_0_OR_GREATER
using System.Buffers;
#endif

namespace OpenMcdf.Tests;

internal static class StreamExtensions
{
#if !NET7_0_OR_GREATER
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
#endif

    public static void Write(this Stream stream, byte[] buffer, WriteMode mode)
    {
#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
        switch (mode)
        {
            case WriteMode.Array:
                stream.Write(buffer, 0, buffer.Length);
                break;
            case WriteMode.Span:
                stream.Write(buffer);
                break;
            case WriteMode.SingleByte:
                {
                    for (int i = 0; i < buffer.Length; i++)
                        stream.WriteByte(buffer[i]);
                    break;
                }
        }
#else
        stream.Write(buffer, 0, buffer.Length);
#endif
    }
}
