using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace OpenMcdf.Test
{
    /// <summary>
    /// Summary description for CFStorageTest
    /// </summary>
    [TestClass]
    public class CFStorageTest
    {
        //const String OUTPUT_DIR = "C:\\TestOutputFiles\\";

        public CFStorageTest()
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
            const string STORAGE_NAME = "NewStorage";
            using CompoundFile cf = new();

            CFStorage st = cf.RootStorage.AddStorage(STORAGE_NAME);

            Assert.IsNotNull(st);
            Assert.AreEqual(STORAGE_NAME, st.Name, false);
        }

        [TestMethod]
        public void Test_CREATE_STORAGE_WITH_CREATION_DATE()
        {
            const string STORAGE_NAME = "NewStorage1";
            using CompoundFile cf = new();

            CFStorage st = cf.RootStorage.AddStorage(STORAGE_NAME);
            st.CreationDate = DateTime.Now;

            Assert.IsNotNull(st);
            Assert.AreEqual(STORAGE_NAME, st.Name, false);

            cf.SaveAs("ProvaData.cfs");
        }

        [TestMethod]
        public void Test_VISIT_ENTRIES()
        {
            const string STORAGE_NAME = "report.xls";
            using CompoundFile cf = new(STORAGE_NAME);

            using FileStream output = new("LogEntries.txt", FileMode.Create);
            using StreamWriter tw = new(output);

            Action<CFItem> va = delegate (CFItem item)
            {
                tw.WriteLine(item.Name);
            };

            cf.RootStorage.VisitEntries(va, true);
        }

        [TestMethod]
        public void Test_TRY_GET_STREAM_STORAGE()
        {
            string FILENAME = "MultipleStorage.cfs";
            using CompoundFile cf = new(FILENAME);

            cf.RootStorage.TryGetStorage("MyStorage", out CFStorage st);
            Assert.IsNotNull(st);

            cf.RootStorage.TryGetStorage("IDONTEXIST", out CFStorage nf);
            Assert.IsNull(nf);

            st.TryGetStream("MyStream", out CFStream s);
            Assert.IsNotNull(s);
            st.TryGetStream("IDONTEXIST2", out CFStream ns);
            Assert.IsNull(ns);

        }

        [TestMethod]
        public void Test_TRY_GET_STREAM_STORAGE_NEW()
        {
            string FILENAME = "MultipleStorage.cfs";
            using CompoundFile cf = new(FILENAME);
            bool bs = cf.RootStorage.TryGetStorage("MyStorage", out CFStorage st);

            Assert.IsTrue(bs);
            Assert.IsNotNull(st);

            bool nb = cf.RootStorage.TryGetStorage("IDONTEXIST", out CFStorage nf);
            Assert.IsFalse(nb);
            Assert.IsNull(nf);

            var b = st.TryGetStream("MyStream", out CFStream s);
            Assert.IsNotNull(s);
            b = st.TryGetStream("IDONTEXIST2", out CFStream ns);
            Assert.IsFalse(b);
        }

        [TestMethod]
        public void Test_VISIT_ENTRIES_CORRUPTED_FILE_VALIDATION_ON()
        {
            using CompoundFile f = new("CorruptedDoc_bug3547815.doc", CFSUpdateMode.ReadOnly, CFSConfiguration.Default);

            Assert.ThrowsException<CFCorruptedFileException>(() =>
            {
                using StreamWriter tw = new StreamWriter("LogEntriesCorrupted_1.txt");
                f.RootStorage.VisitEntries(item => tw.WriteLine(item.Name), true);
            });
        }

        [TestMethod]
        public void Test_VISIT_ENTRIES_CORRUPTED_FILE_VALIDATION_OFF_BUT_CAN_LOAD()
        {
            using CompoundFile f = new("CorruptedDoc_bug3547815_B.doc", CFSUpdateMode.ReadOnly, CFSConfiguration.NoValidationException);

            using StreamWriter tw = new("LogEntriesCorrupted_2.txt");
            f.RootStorage.VisitEntries(item => tw.WriteLine(item.Name), true);
        }

        [TestMethod]
        public void Test_VISIT_STORAGE()
        {
            string FILENAME = "testVisiting.xls";

            // Remove...
            File.Delete(FILENAME);

            //Create...

            using (CompoundFile ncf = new())
            {
                CFStorage l1 = ncf.RootStorage.AddStorage("Storage Level 1");
                l1.AddStream("l1ns1");
                l1.AddStream("l1ns2");
                l1.AddStream("l1ns3");

                CFStorage l2 = l1.AddStorage("Storage Level 2");
                l2.AddStream("l2ns1");
                l2.AddStream("l2ns2");

                ncf.SaveAs(FILENAME);
            }

            // Read...

            using CompoundFile cf = new(FILENAME);
            using FileStream output = new("reportVisit.txt", FileMode.Create);
            using StreamWriter sw = new(output);

            Console.SetOut(sw);

            Action<CFItem> va = delegate (CFItem target)
            {
                sw.WriteLine(target.Name);
            };

            cf.RootStorage.VisitEntries(va, true);
        }

        [TestMethod]
        public void Test_DELETE_DIRECTORY()
        {
            string FILENAME = "MultipleStorage2.cfs";
            using CompoundFile cf = new(FILENAME, CFSUpdateMode.ReadOnly, CFSConfiguration.Default);

            CFStorage st = cf.RootStorage.GetStorage("MyStorage");

            Assert.IsNotNull(st);

            st.Delete("AnotherStorage");

            cf.SaveAs("MultipleStorage_Delete.cfs");
        }

        [TestMethod]
        public void Test_DELETE_MINISTREAM_STREAM()
        {
            string FILENAME = "MultipleStorage2.cfs";
            using CompoundFile cf = new(FILENAME);

            CFStorage found = null;
            Action<CFItem> action = delegate (CFItem item) { if (item.Name == "AnotherStorage") found = item as CFStorage; };
            cf.RootStorage.VisitEntries(action, true);

            Assert.IsNotNull(found);

            found.Delete("AnotherStream");

            cf.SaveAs("MultipleDeleteMiniStream");
        }

        [TestMethod]
        public void Test_DELETE_STREAM()
        {
            string FILENAME = "MultipleStorage3.cfs";
            using CompoundFile cf = new(FILENAME);

            CFStorage found = null;
            Action<CFItem> action = delegate (CFItem item)
            {
                if (item.Name == "AnotherStorage")
                    found = item as CFStorage;
            };

            cf.RootStorage.VisitEntries(action, true);

            Assert.IsNotNull(found);

            found.Delete("Another2Stream");

            cf.SaveAs("MultipleDeleteStream");
        }

        [TestMethod]
        public void Test_CHECK_DISPOSED_()
        {
            const string FILENAME = "MultipleStorage.cfs";
            using CompoundFile cf = new CompoundFile(FILENAME);

            CFStorage st = cf.RootStorage.GetStorage("MyStorage");
            cf.Close();

            Assert.ThrowsException<CFDisposedException>(() => st.GetStream("MyStream").GetData());
        }

        [TestMethod]
        public void Test_LAZY_LOAD_CHILDREN_()
        {
            using (CompoundFile cf = new())
            {
                cf.RootStorage.AddStorage("Level_1")
                    .AddStorage("Level_2")
                    .AddStream("Level2Stream")
                    .SetData(Helpers.GetBuffer(100));
                cf.SaveAs("$Hel1");
            }

            using (CompoundFile cf = new("$Hel1"))
            {
                IList<CFItem> i = cf.GetAllNamedEntries("Level2Stream");
                Assert.IsNotNull(i[0]);
                Assert.IsTrue(i[0] is CFStream);
                Assert.AreEqual(100, (i[0] as CFStream).GetData().Length);
                cf.SaveAs("$Hel2");
            }

            File.Delete("$Hel1");
            File.Delete("$Hel2");
        }

        [TestMethod]
        public void Test_FIX_BUG_31()
        {
            using (CompoundFile cf = new())
            {
                cf.RootStorage.AddStorage("Level_1")
                    .AddStream("Level2Stream")
                    .SetData(Helpers.GetBuffer(100));

                cf.SaveAs("$Hel3");
            }

            using CompoundFile cf1 = new("$Hel3");

            Assert.ThrowsException<CFDuplicatedItemException>(() =>
            {
                CFStream cs = cf1.RootStorage.GetStorage("Level_1").AddStream("Level2Stream");
            });
        }

        [TestMethod]
        public void Test_FIX_BUG_116()
        {
            using (CompoundFile cf = new())
            {
                cf.RootStorage.AddStorage("AStorage")
                    .AddStream("AStream")
                    .SetData(Helpers.GetBuffer(100));

                cf.SaveAs("Hello$File");
            }

            using (CompoundFile cf1 = new("Hello$File", CFSUpdateMode.Update, CFSConfiguration.Default))
            {
                cf1.RootStorage.RenameItem("AStorage", "NewStorage");
                cf1.Commit();
            }

            using CompoundFile cf2 = new CompoundFile("Hello$File");
            var st2 = cf2.RootStorage.GetStorage("NewStorage");
            Assert.IsNotNull(st2);
        }

        [TestMethod]
        [ExpectedException(typeof(CFCorruptedFileException))]
        public void Test_CORRUPTEDDOC_BUG36_SHOULD_THROW_CORRUPTED_FILE_EXCEPTION()
        {
            using CompoundFile file = new CompoundFile("CorruptedDoc_bug36.doc", CFSUpdateMode.ReadOnly, CFSConfiguration.NoValidationException);
            //Many thanks to theseus for bug reporting
        }
    }
}
