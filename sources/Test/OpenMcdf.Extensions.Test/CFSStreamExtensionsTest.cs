using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace OpenMcdf.Extensions.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class CFSStreamExtensionsTest
    {
        public CFSStreamExtensionsTest()
        {
        }

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void Test_AS_IOSTREAM_READ()
        {
            using CompoundFile cf = new("MultipleStorage.cfs");
            using Stream s = cf.RootStorage.GetStorage("MyStorage").GetStream("MyStream").AsIOStream();
            using BinaryReader br = new(s);
            byte[] result = br.ReadBytes(32);
            CollectionAssert.AreEqual(Helpers.GetBuffer(32, 1), result);
        }

        [TestMethod]
        public void Test_AS_IOSTREAM_WRITE()
        {
            const string cmp = "Hello World of BinaryWriter !";

            using (CompoundFile cf = new())
            {
                using Stream s = cf.RootStorage.AddStream("ANewStream").AsIOStream();
                using BinaryWriter bw = new(s);
                bw.Write(cmp);
                cf.SaveAs("$ACFFile.cfs");
            }

            using (CompoundFile cf = new("$ACFFile.cfs"))
            {
                using Stream s = cf.RootStorage.GetStream("ANewStream").AsIOStream();
                using BinaryReader br = new(s);
                string st = br.ReadString();
                Assert.AreEqual(cmp, st);
            }
        }

        [TestMethod]
        public void Test_AS_IOSTREAM_MULTISECTOR_WRITE()
        {
            byte[] data = new byte[670];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 255);
            }

            using (CompoundFile cf = new())
            {
                using Stream s = cf.RootStorage.AddStream("ANewStream").AsIOStream();
                using BinaryWriter bw = new(s);
                bw.Write(data);
                cf.SaveAs("$ACFFile2.cfs");
            }

            // Works
            using (CompoundFile cf = new("$ACFFile2.cfs"))
            {
                using Stream s = cf.RootStorage.GetStream("ANewStream").AsIOStream();
                using BinaryReader br = new(s);
                byte[] readData = new byte[data.Length];
                int readCount = br.Read(readData, 0, readData.Length);
                Assert.AreEqual(readData.Length, readCount);
                CollectionAssert.AreEqual(data, readData);
            }

            // Won't work until #88 is fixed.
            using (CompoundFile cf = new("$ACFFile2.cfs"))
            {
                using Stream readStream = cf.RootStorage.GetStream("ANewStream").AsIOStream();
                using MemoryStream ms = new();
                readStream.CopyTo(ms);
                CollectionAssert.AreEqual(data, ms.ToArray());
            }
        }
    }
}
