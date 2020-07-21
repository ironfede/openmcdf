using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMcdf;

namespace OpenMcdf.Test
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

            Action<CFItem> va = delegate (CFItem item)
            {
                tw.WriteLine(item.Name);
            };

            cf.RootStorage.VisitEntries(va, true);

            tw.Close();

        }


        [TestMethod]
        public void Test_TRY_GET_STREAM_STORAGE()
        {
            String FILENAME = "MultipleStorage.cfs";
            CompoundFile cf = new CompoundFile(FILENAME);

            CFStorage st = null;
            cf.RootStorage.TryGetStorage("MyStorage", out st);
            Assert.IsNotNull(st);

            try
            {
                CFStorage nf = null;
                cf.RootStorage.TryGetStorage("IDONTEXIST", out nf);
                Assert.IsNull(nf);
            }
            catch (Exception)
            {
                Assert.Fail("Exception raised for try_get method");
            }

            try
            {
                CFStream s = null;
                st.TryGetStream("MyStream", out s);
                Assert.IsNotNull(s);
                CFStream ns = null;
                st.TryGetStream("IDONTEXIST2", out ns);
                Assert.IsNull(ns);
            }
            catch (Exception)
            {
                Assert.Fail("Exception raised for try_get method");
            }
        }

        [TestMethod]
        public void Test_TRY_GET_STREAM_STORAGE_NEW()
        {
            String FILENAME = "MultipleStorage.cfs";
            CompoundFile cf = new CompoundFile(FILENAME);
            CFStorage st = null;
            bool bs = cf.RootStorage.TryGetStorage("MyStorage", out st);

            Assert.IsTrue(bs);
            Assert.IsNotNull(st);

            try
            {
                CFStorage nf = null;
                bool nb = cf.RootStorage.TryGetStorage("IDONTEXIST", out nf);
                Assert.IsFalse(nb);
                Assert.IsNull(nf);
            }
            catch (Exception)
            {
                Assert.Fail("Exception raised for TryGetStorage method");
            }

            try
            {
                var b = st.TryGetStream("MyStream", out CFStream s);
                Assert.IsNotNull(s);
                b = st.TryGetStream("IDONTEXIST2", out CFStream ns);
                Assert.IsFalse(b);
            }
            catch (Exception)
            {
                Assert.Fail("Exception raised for try_get method");
            }
        }

        [TestMethod]
        public void Test_VISIT_ENTRIES_CORRUPTED_FILE_VALIDATION_ON()
        {
            CompoundFile f = null;

            try
            {
                f = new CompoundFile("CorruptedDoc_bug3547815.doc", CFSUpdateMode.ReadOnly, CFSConfiguration.NoValidationException);
            }
            catch
            {
                Assert.Fail("No exception has to be fired on creation due to lazy loading");
            }

            FileStream output = null;

            try
            {
                output = new FileStream("LogEntriesCorrupted_1.txt", FileMode.Create);

                using (TextWriter tw = new StreamWriter(output))
                {

                    Action<CFItem> va = delegate (CFItem item)
                       {
                           tw.WriteLine(item.Name);
                       };

                    f.RootStorage.VisitEntries(va, true);
                    tw.Flush();
                }
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CFCorruptedFileException);
                Assert.IsTrue(f != null && f.IsClosed);

            }
            finally
            {
                if (output != null)
                    output.Close();



            }
        }

        [TestMethod]
        public void Test_VISIT_ENTRIES_CORRUPTED_FILE_VALIDATION_OFF_BUT_CAN_LOAD()
        {
            CompoundFile f = null;

            try
            {
                //Corrupted file has invalid children item sid reference
                f = new CompoundFile("CorruptedDoc_bug3547815_B.doc", CFSUpdateMode.ReadOnly, CFSConfiguration.NoValidationException);
            }
            catch
            {
                Assert.Fail("No exception has to be fired on creation due to lazy loading");
            }

            FileStream output = null;

            try
            {
                output = new FileStream("LogEntriesCorrupted_2.txt", FileMode.Create);


                using (TextWriter tw = new StreamWriter(output))
                {

                    Action<CFItem> va = delegate (CFItem item)
                    {
                        tw.WriteLine(item.Name);
                    };

                    f.RootStorage.VisitEntries(va, true);
                    tw.Flush();
                }
            }
            catch
            {
                Assert.Fail("Fail is corrupted but it has to be loaded anyway by test design");
            }
            finally
            {
                if (output != null)
                    output.Close();


            }
        }


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

            Action<CFItem> va = delegate (CFItem target)
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
            CompoundFile cf = new CompoundFile(FILENAME, CFSUpdateMode.ReadOnly, CFSConfiguration.Default);

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
            Action<CFItem> action = delegate (CFItem item) { if (item.Name == "AnotherStorage") found = item as CFStorage; };
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
            Action<CFItem> action = delegate (CFItem item)
            {
                if (item.Name == "AnotherStorage")
                    found = item as CFStorage;
            };

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

        [TestMethod]
        public void Test_LAZY_LOAD_CHILDREN_()
        {
            CompoundFile cf = new CompoundFile();
            cf.RootStorage.AddStorage("Level_1")
                .AddStorage("Level_2")
                .AddStream("Level2Stream")
                .SetData(Helpers.GetBuffer(100));

            cf.Save("$Hel1");

            cf.Close();

            cf = new CompoundFile("$Hel1");
            IList<CFItem> i = cf.GetAllNamedEntries("Level2Stream");
            Assert.IsNotNull(i[0]);
            Assert.IsTrue(i[0] is CFStream);
            Assert.IsTrue((i[0] as CFStream).GetData().Length == 100);
            cf.Save("$Hel2");
            cf.Close();

            if (File.Exists("$Hel1"))
            {
                File.Delete("$Hel1");
            }
            if (File.Exists("$Hel2"))
            {
                File.Delete("$Hel2");
            }
        }

        [TestMethod]
        public void Test_FIX_BUG_31()
        {
            CompoundFile cf = new CompoundFile();
            cf.RootStorage.AddStorage("Level_1")

                .AddStream("Level2Stream")
                .SetData(Helpers.GetBuffer(100));

            cf.Save("$Hel3");

            cf.Close();

            CompoundFile cf1 = new CompoundFile("$Hel3");
            try
            {
                CFStream cs = cf1.RootStorage.GetStorage("Level_1").AddStream("Level2Stream");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.GetType() == typeof(CFDuplicatedItemException));
            }

        }

        [TestMethod]
        [ExpectedException(typeof(OpenMcdf.CFCorruptedFileException))]
        public void Test_CORRUPTEDDOC_BUG36_SHOULD_THROW_CORRUPTED_FILE_EXCEPTION()
        {
            using (CompoundFile file = new CompoundFile("CorruptedDoc_bug36.doc", CFSUpdateMode.ReadOnly, CFSConfiguration.NoValidationException))
            {
                //Many thanks to theseus for bug reporting
            }
        }
    }
}
