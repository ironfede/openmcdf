using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace OpenMcdf.Test
{
    [TestClass]
    public class StreamRWTest
    {
        [TestMethod]
        public void ReadInt64_MaxSizeRead()
        {
            long input = long.MaxValue;
            byte[] bytes = BitConverter.GetBytes(input);
            long actual = 0;
            using (MemoryStream memStream = new MemoryStream(bytes))
            {
                StreamRW reader = new StreamRW(memStream);
                actual = reader.ReadInt64();
            }

            Assert.AreEqual(input, actual);
        }

        [TestMethod]
        public void ReadInt64_SmallNumber()
        {
            long input = 1234;
            byte[] bytes = BitConverter.GetBytes(input);
            long actual = 0;
            using (MemoryStream memStream = new MemoryStream(bytes))
            {
                StreamRW reader = new StreamRW(memStream);
                actual = reader.ReadInt64();
            }

            Assert.AreEqual(input, actual);
        }

        [TestMethod]
        public void ReadInt64_Int32MaxPlusTen()
        {
            long input = (long)int.MaxValue + 10;
            byte[] bytes = BitConverter.GetBytes(input);
            long actual = 0;
            using (MemoryStream memStream = new MemoryStream(bytes))
            {
                StreamRW reader = new StreamRW(memStream);
                actual = reader.ReadInt64();
            }

            Assert.AreEqual(input, actual);
        }
    }
}
