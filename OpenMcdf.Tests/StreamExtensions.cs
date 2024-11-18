namespace OpenMcdf.Tests;

internal static class StreamExtensions
{
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
