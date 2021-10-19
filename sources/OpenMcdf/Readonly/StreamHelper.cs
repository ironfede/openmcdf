using System;
using System.IO;

namespace OpenMcdf
{
    static class StreamHelper
    {
        public static void CopyTo(this Stream sourceStream, Stream destinationStream, IByteArrayPool byteArrayPool,
            long position, long size)
        {
            sourceStream.Seek(position, SeekOrigin.Begin);
            const int defaultBufferLength = 4096;
            var bufferLength = (int) Math.Min(defaultBufferLength, size);
            var buffer = byteArrayPool.Rent(bufferLength);

            var readCount = 0;
            while (readCount < size)
            {
                var count = (int) Math.Min(size - readCount, bufferLength);
                var n = sourceStream.Read(buffer, 0, count);
                if (n == 0)
                {
                    break;
                }
                readCount += n;

                destinationStream.Write(buffer, 0, n);
            }

            byteArrayPool.Return(buffer);
        }

        public static IStreamReader ToStreamReader(this Stream stream)
        {
            if (stream is IStreamReader reader)
            {
                return reader;
            }

            return new StreamRW(stream);
        }
    }
}