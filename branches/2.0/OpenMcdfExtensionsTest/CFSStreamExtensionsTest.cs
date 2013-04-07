using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMcdf;
using OpenMcdf.Extensions;
using System.IO;

namespace OpenMcdfExtensionsTest
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
        public void Test_AS_IOSTREAM_READ()
        {
            CompoundFile cf = new CompoundFile("MultipleStorage.cfs");

            Stream s = cf.RootStorage.GetStorage("MyStorage").GetStream("MyStream").AsIOStream();
            BinaryReader br = new BinaryReader(s);
            byte[] result = br.ReadBytes(32);
            Assert.IsTrue(Helpers.CompareBuffer(Helpers.GetBuffer(32, 1), result));
        }

        [TestMethod]
        public void Test_AS_IOSTREAM_WRITE()
        {

        }
    }
}
