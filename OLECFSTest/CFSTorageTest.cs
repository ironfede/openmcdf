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
    /// Summary description for CFTorageTest
    /// </summary>
    [TestClass]
    public class CFSTorageTest
    {
        //const String OUTPUT_DIR = "C:\\TestOutputFiles\\";

        public CFSTorageTest()
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
        //public static void MyClassInitialize(TestContext testContext) 

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
        public void Test_CREATE_STORAGE()
        {
            const String STORAGE_NAME = "NewStorage";
            CompoundFile cf = new CompoundFile();

            CFStorage st = cf.RootStorage.AddStorage(STORAGE_NAME);

            Assert.IsNotNull(st);
            Assert.AreEqual(STORAGE_NAME, st.Name, false);
        }

        [TestMethod]
        public void Test_CREATE_STORAGE_WITH_CREATION_DATE()
        {
            const String STORAGE_NAME = "NewStorage1";
            CompoundFile cf = new CompoundFile();

            CFStorage st = cf.RootStorage.AddStorage(STORAGE_NAME);
            st.CreationDate = DateTime.Now;

            Assert.IsNotNull(st);
            Assert.AreEqual(STORAGE_NAME, st.Name, false);

            cf.Save("ProvaData.cfs");
            cf.Close();
        }

        [TestMethod]
        public void Test_VISIT_ENTRIES()
        {
            const String STORAGE_NAME = "report.xls";
            CompoundFile cf = new CompoundFile(STORAGE_NAME);

            FileStream output = new FileStream("LogEntries.txt", FileMode.Create);
            TextWriter tw = new StreamWriter(output);

            VisitedEntryAction va = delegate(CFItem item)
            {
                tw.WriteLine(item.Name);
            };

            cf.RootStorage.VisitEntries(va, true);

            tw.Close();

        }

        //[TestMethod]
        //public void Test_OPEN_STORAGE_THUMBSDB()
        //{
        //    const String STORAGE_NAME = "C:\\Documents and Settings\\blaseotf\\My Documents\\My Pictures\\Thumbs.db";
        //    CompoundFile cf = new CompoundFile(STORAGE_NAME);

        //    FileStream output = new FileStream("C:\\ProvaThumbsdb.txt", FileMode.Create);
        //    TextWriter tw = new StreamWriter(output);
        //    Console.SetOut(tw);

        //    VisitedEntryAction va = delegate(CFItem item)
        //    {
        //        CFStream stream = item as CFStream;
        //        if (stream != null)
        //        {
        //            FileStream fs = new FileStream("C:\\Documents and Settings\\blaseotf\\My Documents\\My Pictures\\" + item.Name + ".jpg", FileMode.Create);

        //            BinaryWriter bw = new BinaryWriter(fs);
        //            byte[] b = stream.GetData();
        //            bw.Write(b);
        //            bw.Flush();
        //            fs.Flush();
        //            bw.Close();

        //        }

        //    };

        //    cf.RootStorage.VisitEntries(va, true);

        //    tw.Close();

        //}

        [TestMethod]
        public void Test_VISIT_STORAGE()
        {
            String FILENAME = "testVisiting.xls";

            // Remove...
            if (File.Exists(FILENAME))
                File.Delete(FILENAME);

            //Create...

            CompoundFile ncf = new CompoundFile();

            CFStorage l1 = ncf.RootStorage.AddStorage("Storage Level 1");
            l1.AddStream("l1ns1");
            l1.AddStream("l1ns2");
            l1.AddStream("l1ns3");

            CFStorage l2 = l1.AddStorage("Storage Level 2");
            l2.AddStream("l2ns1");
            l2.AddStream("l2ns2");

            ncf.Save(FILENAME);
            ncf.Close();


            // Read...

            CompoundFile cf = new CompoundFile(FILENAME);

            FileStream output = new FileStream("reportVisit.txt", FileMode.Create);
            TextWriter sw = new StreamWriter(output);

            Console.SetOut(sw);

            VisitedEntryAction va = delegate(CFItem target)
            {
                sw.WriteLine(target.Name);
            };

            cf.RootStorage.VisitEntries(va, true);

            cf.Close();
            sw.Close();
        }

        [TestMethod]
        public void Test_DELETE_DIRECTORY()
        {
            String FILENAME = "MultipleStorage2.cfs";
            CompoundFile cf = new CompoundFile(FILENAME, UpdateMode.ReadOnly, false, false);

            CFStorage st = cf.RootStorage.GetStorage("MyStorage");

            Assert.IsNotNull(st);

            st.Delete("AnotherStorage");

            cf.Save("MultipleStorage_Delete.cfs");

            cf.Close();
        }

        [TestMethod]
        public void Test_DELETE_MINISTREAM_STREAM()
        {
            String FILENAME = "MultipleStorage2.cfs";
            CompoundFile cf = new CompoundFile(FILENAME);

            CFStorage found = null;
            VisitedEntryAction action = delegate(CFItem item) { if (item.Name == "AnotherStorage") found = item as CFStorage; };
            cf.RootStorage.VisitEntries(action, true);

            Assert.IsNotNull(found);

            found.Delete("AnotherStream");

            cf.Save("MultipleDeleteMiniStream");
            cf.Close();
        }

        [TestMethod]
        public void Test_DELETE_STREAM()
        {
            String FILENAME = "MultipleStorage3.cfs";
            CompoundFile cf = new CompoundFile(FILENAME);

            CFStorage found = null;
            VisitedEntryAction action = delegate(CFItem item) { if (item.Name == "AnotherStorage") found = item as CFStorage; };
            cf.RootStorage.VisitEntries(action, true);

            Assert.IsNotNull(found);

            found.Delete("Another2Stream");

            cf.Save("MultipleDeleteStream");
            cf.Close();
        }

        [TestMethod]
        public void Test_CHECK_DISPOSED_()
        {
            const String FILENAME = "MultipleStorage.cfs";
            CompoundFile cf = new CompoundFile(FILENAME);

            CFStorage st = cf.RootStorage.GetStorage("MyStorage");
            cf.Close();

            try
            {
                byte[] temp = st.GetStream("MyStream").GetData();
                Assert.Fail("Stream without media");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CFDisposedException);
            }
        }
    }
}
