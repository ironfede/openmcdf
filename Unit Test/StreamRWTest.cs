using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace OpenMcdfTest
{
    [TestClass]
    public class StreamRWTest
    {
        [TestMethod]
        public void ReadInt64_MaxSizeRead()
        {
            Int64 input = Int64.MaxValue;
            byte[] bytes = BitConverter.GetBytes(input);
            long actual = 0;
            using (MemoryStream memStream = new MemoryStream(bytes))
            {
                OpenMcdf.StreamRW reader = new OpenMcdf.StreamRW(memStream);
                actual = reader.ReadInt64();
            }
            Assert.AreEqual((long)input, actual);
        }

        [TestMethod]
        public void ReadInt64_SmallNumber()
        {
            Int64 input = 1234;
            byte[] bytes = BitConverter.GetBytes(input);
            long actual = 0;
            using (MemoryStream memStream = new MemoryStream(bytes))
            {
                OpenMcdf.StreamRW reader = new OpenMcdf.StreamRW(memStream);
                actual = reader.ReadInt64();
            }
            Assert.AreEqual((long)input, actual);
        }

        [TestMethod]
        public void ReadInt64_Int32MaxPlusTen()
        {
            Int64 input = (Int64)Int32.MaxValue + 10;
            byte[] bytes = BitConverter.GetBytes(input);
            long actual = 0;
            using (MemoryStream memStream = new MemoryStream(bytes))
            {
                OpenMcdf.StreamRW reader = new OpenMcdf.StreamRW(memStream);
                actual = reader.ReadInt64();
            }
            Assert.AreEqual((long)input, actual);
        }
    }
}
