using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public static byte[] GetBuffer(int count, byte c)
        {
            byte[] b = new byte[count];
            Array.Fill(b, c);
            return b;
        }

        public static bool CompareBuffer(byte[] b, byte[] p)
        {
            return (b.Length == p.Length) && CompareBuffer(b, p, b.Length);
        }

        public static bool CompareBuffer(byte[] b, byte[] p, int count)
        {
            if (b == null && p == null)
                throw new Exception("Null buffers");

            if (b == null || p == null)
                return false;

            for (int i = 0; i < count; i++)
            {
                if (b[i] != p[i])
                    return false;
            }

            return true;
        }
    }
}
