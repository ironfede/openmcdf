using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OleCompoundFileStorage;
using System.IO;

namespace OleCfsTest
{
    /// <summary>
    /// Summary description for CompoundFileTest
    /// </summary>
    [TestClass]
    public class CompoundFileTest
    {
        public CompoundFileTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

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
        public void TestCompressSpace()
        {
            String FILENAME = "MultipleStorage3.cfs";

            FileInfo srcFile = new FileInfo(FILENAME);


            CompoundFile cf = new CompoundFile(FILENAME);

            CFStorage st = cf.RootStorage.GetStorage("MyStorage");
            st = st.GetStorage("AnotherStorage");

            Assert.IsNotNull(st);

            st.Delete("Another2Stream"); //17Kb

            cf.CompressFreeSpace();
            cf.Save("MultipleStorage_Deleted_Compress.cfs");

            cf.Close();
            FileInfo dstFile = new FileInfo("MultipleStorage_Deleted_Compress.cfs");

            Assert.IsTrue(srcFile.Length > dstFile.Length);

        }

        [TestMethod]
        public void TestDeleteWithoutCompression()
        {
            String FILENAME = "MultipleStorage3.cfs";

            FileInfo srcFile = new FileInfo(FILENAME);


            CompoundFile cf = new CompoundFile(FILENAME);

            CFStorage st = cf.RootStorage.GetStorage("MyStorage");
            st = st.GetStorage("AnotherStorage");

            Assert.IsNotNull(st);

            st.Delete("Another2Stream"); //17Kb

            //cf.CompressFreeSpace();
            cf.Save("MultipleStorage_Deleted_Compress.cfs");

            cf.Close();
            FileInfo dstFile = new FileInfo("MultipleStorage_Deleted_Compress.cfs");

            Assert.IsFalse(srcFile.Length > dstFile.Length);

        }
    }
}
