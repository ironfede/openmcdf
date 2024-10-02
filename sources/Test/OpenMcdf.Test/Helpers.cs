using System;

namespace OpenMcdf.Test
{
    public static class Helpers
    {
        public static byte[] GetBuffer(int count)
        {
            Random r = new Random();
            byte[] b = new byte[count];
            r.NextBytes(b);
            return b;
        }

        public static void FillBufferWithRandomData(byte[] buffer)
        {
            Random r = new Random();
            r.NextBytes(buffer);
        }

        public static void FillBuffer(byte[] buffer, byte c)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = c;
            }
        }

        public static byte[] GetBuffer(int count, byte c)
        {
            byte[] b = new byte[count];
            FillBuffer(b, c);
            return b;
        }
    }
}
