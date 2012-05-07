using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMcdf;
using System.IO;
using System.Diagnostics;

namespace OpenMcdfTest
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
        public void Test_COMPRESS_SPACE()
        {
            String FILENAME = "MultipleStorage3.cfs"; // 22Kb

            FileInfo srcFile = new FileInfo(FILENAME);

            File.Copy(FILENAME, "MultipleStorage_Deleted_Compress.cfs", true);

            CompoundFile cf = new CompoundFile("MultipleStorage_Deleted_Compress.cfs", UpdateMode.Update, true, true);

            CFStorage st = cf.RootStorage.GetStorage("MyStorage");
            st = st.GetStorage("AnotherStorage");

            Assert.IsNotNull(st);
            st.Delete("Another2Stream");
            cf.Commit();
            cf.Close();

            CompoundFile.ShrinkCompoundFile("MultipleStorage_Deleted_Compress.cfs"); // -> 7Kb

            FileInfo dstFile = new FileInfo("MultipleStorage_Deleted_Compress.cfs");

            Assert.IsTrue(srcFile.Length > dstFile.Length);

        }

        [TestMethod]
        public void Test_ENTRY_NAME_LENGTH()
        {
            //Thanks to Mark Bosold for bug fix and unit

            CompoundFile cf = new CompoundFile();

            // Cannot be equal.
            string maxCharactersStreamName = "1234567890123456789A12345678901"; // 31 chars
            string maxCharactersStorageName = "1234567890123456789012345678901"; // 31 chars

            // Try Storage entry name with max characters.
            Assert.IsNotNull(cf.RootStorage.AddStorage(maxCharactersStorageName));
            CFStorage strg = cf.RootStorage.GetStorage(maxCharactersStorageName);
            Assert.IsNotNull(strg);
            Assert.IsTrue(strg.Name == maxCharactersStorageName);


            // Try Stream entry name with max characters.
            Assert.IsNotNull(cf.RootStorage.AddStream(maxCharactersStreamName));
            CFStream strm = cf.RootStorage.GetStream(maxCharactersStreamName);
            Assert.IsNotNull(strm);
            Assert.IsTrue(strm.Name == maxCharactersStreamName);

            string tooManyCharactersEntryName = "12345678901234567890123456789012"; // 32 chars

            try
            {
                // Try Storage entry name with too many characters.
                cf.RootStorage.AddStorage(tooManyCharactersEntryName);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CFException);
            }

            try
            {
                // Try Stream entry name with too many characters.
                cf.RootStorage.AddStream(tooManyCharactersEntryName);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CFException);
            }

            cf.Save("EntryNameLength");
            cf.Close();
        }

        [TestMethod]
        public void Test_DELETE_WITHOUT_COMPRESSION()
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

        [TestMethod]
        public void Test_WRITE_AND_READ_CFS_VERSION_4()
        {
            String filename = "WRITE_AND_READ_CFS_V4.cfs";

            CompoundFile cf = new CompoundFile(CFSVersion.Ver_4, true, true);

            CFStorage st = cf.RootStorage.AddStorage("MyStorage");
            CFStream sm = st.AddStream("MyStream");
            byte[] b = new byte[220];
            sm.SetData(b);

            cf.Save(filename);
            cf.Close();

            CompoundFile cf2 = new CompoundFile(filename);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");

            Assert.IsNotNull(sm2);
            Assert.IsTrue(sm2.Size == 220);

            cf2.Close();
        }

        [TestMethod]
        public void Test_WRITE_READ_CFS_VERSION_4_STREAM()
        {
            String filename = "WRITE_COMMIT_READ_CFS_V4.cfs";

            CompoundFile cf = new CompoundFile(CFSVersion.Ver_4, true, true);

            CFStorage st = cf.RootStorage.AddStorage("MyStorage");
            CFStream sm = st.AddStream("MyStream");
            byte[] b = Helpers.GetBuffer(227);
            sm.SetData(b);

            cf.Save(filename);
            cf.Close();

            CompoundFile cf2 = new CompoundFile(filename);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");

            Assert.IsNotNull(sm2);
            Assert.IsTrue(sm2.Size == b.Length);

            cf2.Close();
        }

        [TestMethod]
        public void Test_OPEN_FROM_STREAM()
        {
            String filename = "reportREAD.xls";
            File.Copy(filename, "reportOPENFROMSTREAM.xls");
            FileStream fs = new FileStream(filename, FileMode.Open);
            CompoundFile cf = new CompoundFile(fs);
            CFStream foundStream = cf.RootStorage.GetStream("Workbook");

            byte[] temp = foundStream.GetData();

            Assert.IsNotNull(temp);

            cf.Close();
        }

        [TestMethod]
        public void Test_MULTIPLE_SAVE()
        {
            var file = new CompoundFile();

            file.Save("test.mdf");

            var meta = file.
                RootStorage.
                AddStream("meta");

            meta.AppendData(BitConverter.GetBytes(DateTime.Now.ToBinary()));
            meta.AppendData(BitConverter.GetBytes(DateTime.Now.ToBinary()));

            file.Save("test.mdf");
        }

        [TestMethod]
        public void Test_OPEN_COMPOUND_BUG_FIX_133()
        {
            var f = new CompoundFile("testbad.ole");
            CFStream cfs = f.RootStorage.GetStream("\x01Ole10Native");
            byte[] data = cfs.GetData();
            Assert.IsTrue(data.Length == 18140);
        }

        [TestMethod]
        public void Test_COMPARE_DIR_ENTRY_NAME_BUG_FIX_ID_3487353()
        {
            var f = new CompoundFile("report_name_fix.xls", UpdateMode.Update, true, true);
            CFStream cfs = f.RootStorage.AddStream("Poorbook");
            cfs.AppendData(Helpers.GetBuffer(20));
            f.Commit();
            f.Close();

            f = new CompoundFile("report_name_fix.xls", UpdateMode.Update, true, true);
            cfs = f.RootStorage.GetStream("Workbook");
            Assert.IsTrue(cfs.Name == "Workbook");
            f.RootStorage.Delete("PoorBook");
            f.Commit();
            f.Close();

        }

        [TestMethod]
        public void Test_GET_COMPOUND_VERSION()
        {
            var f = new CompoundFile("report_name_fix.xls");
            CFSVersion ver = f.Version;

            Assert.IsTrue(ver == CFSVersion.Ver_3);

            f.Close();
        }

        [TestMethod]
        public void Test_FUNCTIONAL_BEHAVIOUR()
        {
            const int N_FACTOR = 1;

            byte[] bA = Helpers.GetBuffer(20 * 1024 * N_FACTOR, 0x0A);
            byte[] bB = Helpers.GetBuffer(5 * 1024, 0x0B);
            byte[] bC = Helpers.GetBuffer(5 * 1024, 0x0C);
            byte[] bD = Helpers.GetBuffer(5 * 1024, 0x0D);
            byte[] bE = Helpers.GetBuffer(8 * 1024 * N_FACTOR + 1, 0x1A);
            byte[] bF = Helpers.GetBuffer(16 * 1024 * N_FACTOR, 0x1B);
            byte[] bG = Helpers.GetBuffer(14 * 1024 * N_FACTOR, 0x1C);
            byte[] bH = Helpers.GetBuffer(12 * 1024 * N_FACTOR, 0x1D);
            byte[] bE2 = Helpers.GetBuffer(8 * 1024 * N_FACTOR, 0x2A);
            byte[] bMini = Helpers.GetBuffer(1027, 0xEE);

            Stopwatch sw = new Stopwatch();
            sw.Start();


            //############

            // Phase 1
            var cf = new CompoundFile(CFSVersion.Ver_3, true, false);
            cf.RootStorage.AddStream("A").SetData(bA);
            cf.Save("OneStream.cfs");
            cf.Close();

            // Test Phase 1
            var cfTest = new CompoundFile("OneStream.cfs");
            CFStream testSt = cfTest.RootStorage.GetStream("A");

            Assert.IsNotNull(testSt);
            Assert.IsTrue(testSt.Size == bA.Length);
            Assert.IsTrue(Helpers.CompareBuffer(bA, testSt.GetData()));

            cfTest.Close();

            //###########

            //Phase 2
            cf = new CompoundFile("OneStream.cfs", UpdateMode.ReadOnly, true, false);

            cf.RootStorage.AddStream("B").SetData(bB);
            cf.RootStorage.AddStream("C").SetData(bC);
            cf.RootStorage.AddStream("D").SetData(bD);
            cf.RootStorage.AddStream("E").SetData(bE);
            cf.RootStorage.AddStream("F").SetData(bF);
            cf.RootStorage.AddStream("G").SetData(bG);
            cf.RootStorage.AddStream("H").SetData(bH);

            cf.Save("8_Streams.cfs");
            cf.Close();

            // Test Phase 2

            cfTest = new CompoundFile("8_Streams.cfs");

            testSt = cfTest.RootStorage.GetStream("B");
            Assert.IsNotNull(testSt);
            Assert.IsTrue(testSt.Size == bB.Length);
            Assert.IsTrue(Helpers.CompareBuffer(bB, testSt.GetData()));

            testSt = cfTest.RootStorage.GetStream("C");
            Assert.IsNotNull(testSt);
            Assert.IsTrue(testSt.Size == bC.Length);
            Assert.IsTrue(Helpers.CompareBuffer(bC, testSt.GetData()));

            testSt = cfTest.RootStorage.GetStream("D");
            Assert.IsNotNull(testSt);
            Assert.IsTrue(testSt.Size == bD.Length);
            Assert.IsTrue(Helpers.CompareBuffer(bD, testSt.GetData()));

            testSt = cfTest.RootStorage.GetStream("E");
            Assert.IsNotNull(testSt);
            Assert.IsTrue(testSt.Size == bE.Length);
            Assert.IsTrue(Helpers.CompareBuffer(bE, testSt.GetData()));

            testSt = cfTest.RootStorage.GetStream("F");
            Assert.IsNotNull(testSt);
            Assert.IsTrue(testSt.Size == bF.Length);
            Assert.IsTrue(Helpers.CompareBuffer(bF, testSt.GetData()));

            testSt = cfTest.RootStorage.GetStream("G");
            Assert.IsNotNull(testSt);
            Assert.IsTrue(testSt.Size == bG.Length);
            Assert.IsTrue(Helpers.CompareBuffer(bG, testSt.GetData()));

            testSt = cfTest.RootStorage.GetStream("H");
            Assert.IsNotNull(testSt);
            Assert.IsTrue(testSt.Size == bH.Length);
            Assert.IsTrue(Helpers.CompareBuffer(bH, testSt.GetData()));

            cfTest.Close();


            File.Copy("8_Streams.cfs", "6_Streams.cfs", true);

            //###########

            // Phase 3

            cf = new CompoundFile("6_Streams.cfs", UpdateMode.Update, true, true);
            cf.RootStorage.Delete("D");
            cf.RootStorage.Delete("G");
            cf.Commit();

            cf.Close();

            //Test Phase 3


            cfTest = new CompoundFile("6_Streams.cfs");

            bool catched = false;

            try
            {
                testSt = cfTest.RootStorage.GetStream("D");
            }
            catch (Exception ex)
            {
                if (ex is CFItemNotFound)
                    catched = true;
            }

            Assert.IsTrue(catched);

            catched = false;

            try
            {
                testSt = cfTest.RootStorage.GetStream("G");
            }
            catch (Exception ex)
            {
                if (ex is CFItemNotFound)
                    catched = true;
            }

            Assert.IsTrue(catched);

            cfTest.Close();

            //##########

            // Phase 4

            File.Copy("6_Streams.cfs", "6_Streams_Shrinked.cfs", true);
            CompoundFile.ShrinkCompoundFile("6_Streams_Shrinked.cfs");

            // Test Phase 4

            Assert.IsTrue(new FileInfo("6_Streams_Shrinked.cfs").Length < new FileInfo("6_Streams.cfs").Length);

            cfTest = new CompoundFile("6_Streams_Shrinked.cfs");
            VisitedEntryAction va = delegate(CFItem item)
            {
                if (item.IsStream)
                {
                    CFStream ia = item as CFStream;
                    Assert.IsNotNull(ia);
                    Assert.IsTrue(ia.Size > 0);
                    byte[] d = ia.GetData();
                    Assert.IsNotNull(d);
                    Assert.IsTrue(d.Length > 0);
                    Assert.IsTrue(d.Length == ia.Size);
                }
            };

            cfTest.RootStorage.VisitEntries(va, true);
            cfTest.Close();

            //##########

            //Phase 5

            cf = new CompoundFile("6_Streams_Shrinked.cfs", UpdateMode.Update, true, false);
            cf.RootStorage.AddStream("ZZZ").SetData(bF);
            cf.RootStorage.GetStream("E").AppendData(bE2);
            cf.Commit();
            cf.Close();

            cf = new CompoundFile("6_Streams_Shrinked.cfs", UpdateMode.Update, true, false);
            cf.RootStorage.CLSID = new Guid("EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE");
            cf.Commit();
            cf.Close();

            cf = new CompoundFile("6_Streams_Shrinked.cfs", UpdateMode.Update, true, false);
            cf.RootStorage.AddStorage("MyStorage").AddStream("ANS").AppendData(bE);
            cf.Commit();
            cf.Close();

            cf = new CompoundFile("6_Streams_Shrinked.cfs", UpdateMode.Update, true, false);
            cf.RootStorage.AddStorage("AnotherStorage").AddStream("ANS").AppendData(bE);
            cf.RootStorage.Delete("MyStorage");
            cf.Commit();
            cf.Close();

            //Test Phase 5

            //#####

            //Phase 6

            cf = new CompoundFile("6_Streams_Shrinked.cfs", UpdateMode.Update, true, false);
            cf.RootStorage.AddStorage("MiniStorage").AddStream("miniSt").AppendData(bMini);
            cf.RootStorage.GetStorage("MiniStorage").AddStream("miniSt2").AppendData(bMini);
            cf.Commit();
            cf.Close();

            cf = new CompoundFile("6_Streams_Shrinked.cfs", UpdateMode.Update, true, false);
            cf.RootStorage.GetStorage("MiniStorage").Delete("miniSt");


            cf.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2").AppendData(bE);

            cf.Commit();
            cf.Close();

            //Test Phase 6

            cfTest = new CompoundFile("6_Streams_Shrinked.cfs");
            byte[] d2 = cfTest.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2").GetData();
            Assert.IsTrue(d2.Length == (bE.Length + bMini.Length));

            int cnt = 1;
            d2 = cfTest.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2").GetData(bMini.Length, ref cnt);

            Assert.IsTrue(cnt == 1);
            Assert.IsTrue(d2.Length == 1);
            Assert.IsTrue(d2[0] == 0x1A);

            cnt = 1;
            d2 = cfTest.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2").GetData(bMini.Length - 1, ref cnt);
            Assert.IsTrue(cnt == 1);
            Assert.IsTrue(d2.Length == 1);
            Assert.IsTrue(d2[0] == 0xEE);

            try
            {
                cfTest.RootStorage.GetStorage("MiniStorage").GetStream("miniSt");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CFItemNotFound);
            }

            cfTest.Close();

            //##############

            //Phase 7

            cf = new CompoundFile("6_Streams_Shrinked.cfs", UpdateMode.Update, true, false);

            cf.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2").SetData(bA);
            cf.Commit();
            cf.Close();


            //Test Phase 7

            cf = new CompoundFile("6_Streams_Shrinked.cfs", UpdateMode.Update, true, false);
            d2 = cf.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2").GetData();
            Assert.IsNotNull(d2);
            Assert.IsTrue(d2.Length == bA.Length);

            cf.Close();

            //##############

            cf = new CompoundFile("6_Streams_Shrinked.cfs", UpdateMode.ReadOnly, true, false);

            var myStream = cf.RootStorage.GetStream("C");
            var data = myStream.GetData();
            Console.WriteLine(data[0] + " : " + data[data.Length - 1]);

            myStream = cf.RootStorage.GetStream("B");
            data = myStream.GetData();
            Console.WriteLine(data[0] + " : " + data[data.Length - 1]);

            cf.Close();

            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);

        }

        [TestMethod]
        public void Test_RETRIVE_ALL_NAMED_ENTRIES()
        {
            var f = new CompoundFile("MultipleStorage4.cfs");
            IList<CFItem> result = f.GetAllNamedEntries("MyStream");

            Assert.IsTrue(result.Count == 3);
        }

        //[TestMethod]
        //public void Test_REM()
        //{
        //    var f = new CompoundFile();

        //    byte[] bB = Helpers.GetBuffer(5 * 1024, 0x0B); 
        //    f.RootStorage.AddStream("Test").AppendData(bB);
        //    f.Save("Astorage.cfs");
        //}

    }
}
