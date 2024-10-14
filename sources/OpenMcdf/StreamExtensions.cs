using System;
using System.IO;

namespace OpenMcdf
{
    internal static class StreamExtensions
    {
        public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int count)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

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
}
