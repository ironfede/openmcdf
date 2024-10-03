using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenMcdf.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class CFStreamTest

    {
        //const String TestContext.TestDir = "C:\\TestOutputFiles\\";

        public CFStreamTest()
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
        public void Test_READ_STREAM()
        {
            string filename = "report.xls";

            using CompoundFile cf = new(filename);
            CFStream foundStream = cf.RootStorage.GetStream("Workbook");

            byte[] temp = foundStream.GetData();

            Assert.IsNotNull(temp);
            Assert.IsTrue(temp.Length > 0);
        }

        [TestMethod]
        public void Test_WRITE_STREAM()
        {
            const int BUFFER_LENGTH = 10000;

            byte[] b = Helpers.GetBuffer(BUFFER_LENGTH);

            using CompoundFile cf = new();
            CFStream myStream = cf.RootStorage.AddStream("MyStream");

            Assert.IsNotNull(myStream);
            Assert.AreEqual(0, myStream.Size);

            myStream.SetData(b);

            Assert.AreEqual(BUFFER_LENGTH, myStream.Size, "Stream size differs from buffer size");
        }

        [TestMethod]
        public void Test_WRITE_MINI_STREAM()
        {
            const int BUFFER_LENGTH = 1023; // < 4096

            byte[] b = Helpers.GetBuffer(BUFFER_LENGTH);

            using CompoundFile cf = new();
            CFStream myStream = cf.RootStorage.AddStream("MyMiniStream");

            Assert.IsNotNull(myStream);
            Assert.AreEqual(0, myStream.Size);

            myStream.SetData(b);

            Assert.AreEqual(BUFFER_LENGTH, myStream.Size, "Mini Stream size differs from buffer size");
        }

        [TestMethod]
        public void Test_ZERO_LENGTH_WRITE_STREAM()
        {
            byte[] b = new byte[0];

            using CompoundFile cf = new();
            CFStream myStream = cf.RootStorage.AddStream("MyStream");

            Assert.IsNotNull(myStream);
            myStream.SetData(b);
            cf.SaveAs("ZERO_LENGTH_STREAM.cfs");

            File.Delete("ZERO_LENGTH_STREAM.cfs");
        }

        [TestMethod]
        public void Test_ZERO_LENGTH_RE_WRITE_STREAM()
        {
            byte[] b = new byte[0];

            using (CompoundFile cf = new())
            {
                CFStream myStream = cf.RootStorage.AddStream("MyStream");

                Assert.IsNotNull(myStream);

                myStream.SetData(b);

                cf.SaveAs("ZERO_LENGTH_STREAM_RE.cfs");
            }

            using (CompoundFile cfo = new("ZERO_LENGTH_STREAM_RE.cfs"))
            {
                CFStream oStream = cfo.RootStorage.GetStream("MyStream");

                Assert.IsNotNull(oStream);
                Assert.AreEqual(0, oStream.Size);

                oStream.SetData(Helpers.GetBuffer(30));
                cfo.SaveAs("ZERO_LENGTH_STREAM_RE2.cfs");
            }

            File.Delete("ZERO_LENGTH_STREAM_RE.cfs");

            File.Delete("ZERO_LENGTH_STREAM_RE2.cfs");
        }

        [TestMethod]
        public void Test_WRITE_STREAM_WITH_DIFAT()
        {
            //const int SIZE = 15388609; //Incredible condition of 'resonance' between FAT and DIFAT sec number
            const int SIZE = 15345665; // 64 -> 65 NOT working (in the past ;-)  )
            byte[] b = Helpers.GetBuffer(SIZE, 0);

            using (CompoundFile cf = new())
            {
                CFStream myStream = cf.RootStorage.AddStream("MyStream");
                Assert.IsNotNull(myStream);
                myStream.SetData(b);

                cf.SaveAs("WRITE_STREAM_WITH_DIFAT.cfs");
            }

            using (CompoundFile cf2 = new("WRITE_STREAM_WITH_DIFAT.cfs"))
            {
                CFStream st = cf2.RootStorage.GetStream("MyStream");

                Assert.IsNotNull(cf2);
                Assert.AreEqual(SIZE, st.Size);

                CollectionAssert.AreEqual(b, st.GetData());
            }

            File.Delete("WRITE_STREAM_WITH_DIFAT.cfs");
        }

        [TestMethod]
        public void Test_WRITE_MINISTREAM_READ_REWRITE_STREAM()
        {
            const int BIGGER_SIZE = 350;
            //const int SMALLER_SIZE = 290;
            const int MEGA_SIZE = 18000000;

            byte[] ba1 = Helpers.GetBuffer(BIGGER_SIZE, 1);
            byte[] ba2 = Helpers.GetBuffer(BIGGER_SIZE, 2);
            byte[] ba3 = Helpers.GetBuffer(BIGGER_SIZE, 3);
            byte[] ba4 = Helpers.GetBuffer(BIGGER_SIZE, 4);
            byte[] ba5 = Helpers.GetBuffer(BIGGER_SIZE, 5);

            //WRITE 5 (mini)streams in a compound file --

            using (CompoundFile cfa = new())
            {
                CFStream myStream = cfa.RootStorage.AddStream("MyFirstStream");
                Assert.IsNotNull(myStream);

                myStream.SetData(ba1);
                Assert.AreEqual(BIGGER_SIZE, myStream.Size);

                CFStream myStream2 = cfa.RootStorage.AddStream("MySecondStream");
                Assert.IsNotNull(myStream2);

                myStream2.SetData(ba2);
                Assert.AreEqual(BIGGER_SIZE, myStream2.Size);

                CFStream myStream3 = cfa.RootStorage.AddStream("MyThirdStream");
                Assert.IsNotNull(myStream3);

                myStream3.SetData(ba3);
                Assert.AreEqual(BIGGER_SIZE, myStream3.Size);

                CFStream myStream4 = cfa.RootStorage.AddStream("MyFourthStream");
                Assert.IsNotNull(myStream4);

                myStream4.SetData(ba4);
                Assert.AreEqual(BIGGER_SIZE, myStream4.Size);

                CFStream myStream5 = cfa.RootStorage.AddStream("MyFifthStream");
                Assert.IsNotNull(myStream5);

                myStream5.SetData(ba5);
                Assert.AreEqual(BIGGER_SIZE, myStream5.Size);

                cfa.SaveAs("WRITE_MINISTREAM_READ_REWRITE_STREAM.cfs");
            }

            // Now get the second stream and rewrite it smaller
            byte[] bb = Helpers.GetBuffer(MEGA_SIZE);
            using (CompoundFile cfb = new("WRITE_MINISTREAM_READ_REWRITE_STREAM.cfs"))
            {
                CFStream myStreamB = cfb.RootStorage.GetStream("MySecondStream");
                Assert.IsNotNull(myStreamB);
                myStreamB.SetData(bb);
                Assert.AreEqual(MEGA_SIZE, myStreamB.Size);

                byte[] bufferB = myStreamB.GetData();
                CollectionAssert.AreEqual(bb, bufferB);
                cfb.SaveAs("WRITE_MINISTREAM_READ_REWRITE_STREAM_2ND.cfs");
            }

            using (CompoundFile cfc = new("WRITE_MINISTREAM_READ_REWRITE_STREAM_2ND.cfs"))
            {
                CFStream myStreamC = cfc.RootStorage.GetStream("MySecondStream");
                Assert.AreEqual(MEGA_SIZE, myStreamC.Size, "DATA SIZE FAILED");

                byte[] bufferC = myStreamC.GetData();
                CollectionAssert.AreEqual(bb, bufferC);
            }

            File.Delete("WRITE_MINISTREAM_READ_REWRITE_STREAM.cfs");

            File.Delete("WRITE_MINISTREAM_READ_REWRITE_STREAM_2ND.cfs");
        }

        [TestMethod]
        public void Test_RE_WRITE_SMALLER_STREAM()
        {
            const int BUFFER_LENGTH = 8000;

            string filename = "report.xls";

            byte[] b = Helpers.GetBuffer(BUFFER_LENGTH);

            using (CompoundFile cf = new(filename))
            {
                CFStream foundStream = cf.RootStorage.GetStream("Workbook");
                foundStream.SetData(b);
                cf.SaveAs("reportRW_SMALL.xls");
            }

            using (CompoundFile cf = new("reportRW_SMALL.xls"))
            {
                byte[] c = cf.RootStorage.GetStream("Workbook").GetData();
                Assert.AreEqual(BUFFER_LENGTH, c.Length);
            }

            File.Delete("reportRW_SMALL.xls");
        }

        [TestMethod]
        public void Test_RE_WRITE_SMALLER_MINI_STREAM()
        {
            string filename = "report.xls";

            byte[] b;
            using (CompoundFile cf = new(filename))
            {
                CFStream foundStream = cf.RootStorage.GetStream("\x05SummaryInformation");
                int TEST_LENGTH = (int)foundStream.Size - 20;
                b = Helpers.GetBuffer(TEST_LENGTH);
                foundStream.SetData(b);

                cf.SaveAs("RE_WRITE_SMALLER_MINI_STREAM.xls");
            }

            using (CompoundFile cf = new("RE_WRITE_SMALLER_MINI_STREAM.xls"))
            {
                byte[] c = cf.RootStorage.GetStream("\x05SummaryInformation").GetData();
                CollectionAssert.AreEqual(b, c);
            }

            File.Delete("RE_WRITE_SMALLER_MINI_STREAM.xls");
        }

        [TestMethod]
        public void Test_TRANSACTED_ADD_STREAM_TO_EXISTING_FILE()
        {
            string srcFilename = "report.xls";
            string dstFilename = "reportOverwrite.xls";

            File.Copy(srcFilename, dstFilename, true);

            using CompoundFile cf = new(dstFilename, CFSUpdateMode.Update, CFSConfiguration.Default);

            byte[] buffer = Helpers.GetBuffer(5000);

            CFStream addedStream = cf.RootStorage.AddStream("MyNewStream");
            addedStream.SetData(buffer);

            cf.Commit();
            cf.Close();

            File.Delete("reportOverwrite.xls");
        }

        [TestMethod]
        public void Test_TRANSACTED_ADD_REMOVE_MULTIPLE_STREAM_TO_EXISTING_FILE()
        {
            string srcFilename = "report.xls";
            string dstFilename = "reportOverwriteMultiple.xls";

            File.Copy(srcFilename, dstFilename, true);

            using CompoundFile cf = new(dstFilename, CFSUpdateMode.ReadOnly, CFSConfiguration.SectorRecycle);

            //CompoundFile cf = new CompoundFile();

            Random r = new Random();

            for (int i = 0; i < 254; i++)
            {
                //byte[] buffer = Helpers.GetBuffer(r.Next(100, 3500), (byte)i);
                byte[] buffer = Helpers.GetBuffer(1995, 1);

                //if (i > 0)
                //{
                //    if (r.Next(0, 100) > 50)
                //    {
                //        cf.RootStorage.Delete("MyNewStream" + (i - 1).ToString());
                //    }
                //}

                CFStream addedStream = cf.RootStorage.AddStream("MyNewStream" + i.ToString());
                Assert.IsNotNull(addedStream, "Stream not found");
                addedStream.SetData(buffer);

                CollectionAssert.AreEqual(addedStream.GetData(), buffer);

                // Random commit, not on single addition
                //if (r.Next(0, 100) > 50)
                //    cf.UpdateFile();
            }

            cf.SaveAs(dstFilename + "PP");
            cf.Close();

            File.Delete("reportOverwriteMultiple.xls");

            File.Delete("reportOverwriteMultiple.xlsPP");
        }

        [TestMethod]
        public void Test_TRANSACTED_ADD_MINISTREAM_TO_EXISTING_FILE()
        {
            string srcFilename = "report.xls";
            string dstFilename = "reportOverwriteMultiple.xls";

            File.Copy(srcFilename, dstFilename, true);

            using CompoundFile cf = new(dstFilename, CFSUpdateMode.Update, CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors);

            Random r = new Random();

            byte[] buffer = Helpers.GetBuffer(31, 0x0A);

            cf.RootStorage.AddStream("MyStream").SetData(buffer);
            cf.Commit();
            cf.Close();

            using FileStream larger = new(dstFilename, FileMode.Open);
            using FileStream smaller = new(srcFilename, FileMode.Open);

            // Equal condition if minisector can be "allocated"
            // within the existing standard sector border
            Assert.IsTrue(larger.Length >= smaller.Length);

            larger.Close();
            smaller.Close();

            File.Delete("reportOverwriteMultiple.xlsPP");
        }

        [TestMethod]
        public void Test_TRANSACTED_REMOVE_MINI_STREAM_ADD_MINISTREAM_TO_EXISTING_FILE()
        {
            string srcFilename = "report.xls";
            string dstFilename = "reportOverwrite2.xls";

            File.Copy(srcFilename, dstFilename, true);

            using CompoundFile cf = new(dstFilename, CFSUpdateMode.Update, CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors);

            cf.RootStorage.Delete("\x05SummaryInformation");

            byte[] buffer = Helpers.GetBuffer(2000);

            CFStream addedStream = cf.RootStorage.AddStream("MyNewStream");
            addedStream.SetData(buffer);

            cf.Commit();
            cf.Close();

            File.Delete("reportOverwrite2.xlsPP");
        }

        [TestMethod]
        public void Test_DELETE_STREAM_1()
        {
            string filename = "MultipleStorage.cfs";

            using CompoundFile cf = new(filename);
            CFStorage cfs = cf.RootStorage.GetStorage("MyStorage");
            cfs.Delete("MySecondStream");

            cf.SaveAs(TestContext + "MultipleStorage_REMOVED_STREAM_1.cfs");
        }

        [TestMethod]
        public void Test_DELETE_STREAM_2()
        {
            string filename = "MultipleStorage.cfs";

            using CompoundFile cf = new(filename);
            CFStorage cfs = cf.RootStorage.GetStorage("MyStorage").GetStorage("AnotherStorage");

            cfs.Delete("AnotherStream");

            cf.SaveAs(TestContext + "MultipleStorage_REMOVED_STREAM_2.cfs");
        }

        [TestMethod]
        public void Test_WRITE_AND_READ_CFS()
        {
            string filename = "WRITE_AND_READ_CFS.cfs";

            using CompoundFile cf = new();

            CFStorage st = cf.RootStorage.AddStorage("MyStorage");
            CFStream sm = st.AddStream("MyStream");
            byte[] b = Helpers.GetBuffer(220, 0x0A);
            sm.SetData(b);

            cf.SaveAs(filename);
            cf.Close();

            using CompoundFile cf2 = new(filename);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");
            cf2.Close();

            Assert.IsNotNull(sm2);
            Assert.AreEqual(220, sm2.Size);

            File.Delete(filename);
        }

        [TestMethod]
        public void Test_INCREMENTAL_SIZE_MULTIPLE_WRITE_AND_READ_CFS()
        {
            Random r = new Random();

            for (int i = r.Next(1, 100); i < 1024 * 1024 * 70; i = i << 1)
            {
                SingleWriteReadMatching(i + r.Next(0, 3));
            }
        }

        [TestMethod]
        public void Test_INCREMENTAL_SIZE_MULTIPLE_WRITE_AND_READ_CFS_STREAM()
        {
            Random r = new Random();

            for (int i = r.Next(1, 100); i < 1024 * 1024 * 70; i = i << 1)
            {
                SingleWriteReadMatchingSTREAMED(i + r.Next(0, 3));
            }
        }

        [TestMethod]
        public void Test_DELETE_ZERO_LENGTH_STREAM()
        {
            string zeroLengthName = "MyZeroStream";
            string filename = "DeleteZeroLengthStream.cfs";
            byte[] b = new byte[0];

            using (CompoundFile cf = new())
            {
                CFStream myStream = cf.RootStorage.AddStream(zeroLengthName);
                Assert.IsNotNull(myStream);
                myStream.SetData(b);
                cf.SaveAs(filename);
            }

            using CompoundFile cf2 = new(filename);
            cf2.RootStorage.Delete(zeroLengthName);
            Assert.ThrowsException<CFItemNotFound>(() => cf2.RootStorage.GetStream(zeroLengthName));
            cf2.SaveAs("MultipleDeleteMiniStream.cfs");
        }

        //[TestMethod]
        //public void Test_INCREMENTAL_TRANSACTED_CHANGE_CFS()
        //{

        //    Random r = new Random();

        //    for (int i = r.Next(1, 100); i < 1024 * 1024 * 70; i = i << 1)
        //    {
        //        SingleTransactedChange(i + r.Next(0, 3));
        //    }

        //}

        public static void SingleTransactedChange(int size)
        {
            string filename = "INCREMENTAL_SIZE_MULTIPLE_WRITE_AND_READ_CFS.cfs";

            File.Delete(filename);

            byte[] b = Helpers.GetBuffer(size);

            using (CompoundFile cf = new())
            {
                CFStorage st = cf.RootStorage.AddStorage("MyStorage");
                CFStream sm = st.AddStream("MyStream");
                sm.SetData(b);
                cf.SaveAs(filename);
            }

            using CompoundFile cf2 = new(filename);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");

            Assert.IsNotNull(sm2);
            Assert.AreEqual(size, sm2.Size);
            CollectionAssert.AreEqual(b, sm2.GetData());
        }

        private static void SingleWriteReadMatching(int size)
        {
            string filename = "INCREMENTAL_SIZE_MULTIPLE_WRITE_AND_READ_CFS.cfs";

            File.Delete(filename);

            byte[] b = Helpers.GetBuffer(size);

            using (CompoundFile cf = new())
            {
                CFStorage st = cf.RootStorage.AddStorage("MyStorage");
                CFStream sm = st.AddStream("MyStream");

                sm.SetData(b);
                cf.SaveAs(filename);
            }

            using CompoundFile cf2 = new(filename);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");

            Assert.IsNotNull(sm2);
            Assert.AreEqual(size, sm2.Size);
            CollectionAssert.AreEqual(b, sm2.GetData());
        }

        private static void SingleWriteReadMatchingSTREAMED(int size)
        {
            byte[] b = Helpers.GetBuffer(size);

            using MemoryStream ms = new(size);

            using (CompoundFile cf = new())
            {
                CFStorage st = cf.RootStorage.AddStorage("MyStorage");
                CFStream sm = st.AddStream("MyStream");
                sm.SetData(b);
                cf.Save(ms);
            }

            using CompoundFile cf2 = new(ms);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");

            Assert.IsNotNull(sm2);
            CollectionAssert.AreEqual(b, sm2.GetData());
        }

        [TestMethod]
        public void Test_APPEND_DATA_TO_STREAM()
        {
            using MemoryStream ms = new();

            byte[] b = new byte[] { 0x0, 0x1, 0x2, 0x3 };
            byte[] b2 = new byte[] { 0x4, 0x5, 0x6, 0x7 };

            using (CompoundFile cf = new())
            {
                CFStream st = cf.RootStorage.AddStream("MyMiniStream");
                st.SetData(b);
                st.Append(b2);
                cf.Save(ms);
            }

            byte[] cmp = new byte[] { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7 };
            using (CompoundFile cf = new(ms))
            {
                byte[] data = cf.RootStorage.GetStream("MyMiniStream").GetData();
                CollectionAssert.AreEqual(cmp, data);
            }
        }

        [TestMethod]
        public void Test_COPY_FROM_STREAM()
        {
            byte[] b = Helpers.GetBuffer(100);

            using (CompoundFile cf = new())
            {
                CFStream st = cf.RootStorage.AddStream("MyImportedStream");
                using MemoryStream ms = new(b);
                st.CopyFrom(ms);
                ms.Close();
                cf.SaveAs("COPY_FROM_STREAM.cfs");
            }

            using (CompoundFile cf = new("COPY_FROM_STREAM.cfs"))
            {
                byte[] data = cf.RootStorage.GetStream("MyImportedStream").GetData();
                CollectionAssert.AreEqual(b, data);
            }
        }

#if LARGETEST

        [TestMethod]
        public void Test_APPEND_DATA_TO_CREATE_LARGE_STREAM()
        {
            byte[] b = Helpers.GetBuffer(1024 * 1024 * 50); //2GB buffer
            byte[] cmp = new byte[] { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7 };

            CompoundFile cf = new CompoundFile(CFSVersion.Ver_4, false, false);
            CFStream st = cf.RootStorage.AddStream("MySuperLargeStream");
            cf.Save("MEGALARGESSIMUSFILE.cfs");
            cf.Close();


            cf = new CompoundFile("MEGALARGESSIMUSFILE.cfs", UpdateMode.Update, false, false);
            CFStream cfst = cf.RootStorage.GetStream("MySuperLargeStream");
            for (int i = 0; i < 42; i++)
            {
                cfst.AppendData(b);
                cf.Commit(true);
            }

            cfst.AppendData(cmp);
            cf.Commit(true);

            cf.Close();


            cf = new CompoundFile("MEGALARGESSIMUSFILE.cfs");
            int count = 8;
            byte[] data = cf.RootStorage.GetStream("MySuperLargeStream").GetData((long)b.Length * 42L, ref count);
            CollectionAssert.AreEqual(cmp, data);
            cf.Close();

        }
#endif

        [TestMethod]
        public void Test_RESIZE_STREAM_NO_TRANSITION()
        {
            int INITIAL_SIZE = 1024 * 1024 * 2;
            int DELTA_SIZE = 300;
            //CFStream st = null;
            byte[] b = Helpers.GetBuffer(INITIAL_SIZE); //2MB buffer

            using (CompoundFile cf = new(CFSVersion.Ver_3, CFSConfiguration.Default))
            {
                cf.RootStorage.AddStream("AStream").SetData(b);
                cf.SaveAs("$Test_RESIZE_STREAM.cfs");
            }

            using (CompoundFile cf = new("$Test_RESIZE_STREAM.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle))
            {
                CFStream item = cf.RootStorage.GetStream("AStream");
                item.Resize(INITIAL_SIZE - DELTA_SIZE);
                //cf.RootStorage.AddStream("BStream").SetData(b);
                cf.Commit(true);
            }

            using (CompoundFile cf = new("$Test_RESIZE_STREAM.cfs", CFSUpdateMode.ReadOnly, CFSConfiguration.Default))
            {
                CFStream item = cf.RootStorage.GetStream("AStream");
                Assert.IsNotNull(item);
                Assert.AreEqual(INITIAL_SIZE - DELTA_SIZE, item.Size);

                byte[] buffer = new byte[INITIAL_SIZE - DELTA_SIZE];
                item.Read(buffer, 0, buffer.Length);
                CollectionAssert.AreEqual(b.Take(buffer.Length).ToList(), buffer);
            }
        }

        [TestMethod]
        public void Test_RESIZE_STREAM_TRANSITION_TO_MINI()
        {
            string FILE_NAME = "$Test_RESIZE_STREAM_TRANSITION_TO_MINI.cfs";
            byte[] b = Helpers.GetBuffer(1024 * 1024 * 2); //2MB buffer
            byte[] b100 = new byte[100];

            for (int i = 0; i < 100; i++)
            {
                b100[i] = b[i];
            }

            using (CompoundFile cf = new(CFSVersion.Ver_3, CFSConfiguration.Default))
            {
                cf.RootStorage.AddStream("AStream").SetData(b);
                cf.SaveAs(FILE_NAME);
            }

            using (CompoundFile cf = new(FILE_NAME, CFSUpdateMode.Update, CFSConfiguration.SectorRecycle))
            {
                CFStream item = cf.RootStorage.GetStream("AStream");
                item.Resize(100);
                cf.Commit();
            }

            using (CompoundFile cf = new(FILE_NAME, CFSUpdateMode.ReadOnly, CFSConfiguration.Default))
            {
                CollectionAssert.AreEqual(b100, cf.RootStorage.GetStream("AStream").GetData());
            }

            File.Delete(FILE_NAME);
        }

        [TestMethod]
        public void Test_RESIZE_STREAM_TRANSITION_TO_NORMAL()
        {
            byte[] b = Helpers.GetBuffer(1024 * 2, 0xAA); //2MB buffer

            using (CompoundFile cf = new(CFSVersion.Ver_3, CFSConfiguration.Default))
            {
                cf.RootStorage.AddStream("AStream").SetData(b);
                cf.SaveAs("$Test_RESIZE_STREAM_TRANSITION_TO_NORMAL.cfs");
                cf.SaveAs("$Test_RESIZE_STREAM_TRANSITION_TO_NORMAL2.cfs");
            }

            using (CompoundFile cf = new("$Test_RESIZE_STREAM_TRANSITION_TO_NORMAL.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors))
            {
                CFStream item = cf.RootStorage.GetStream("AStream");
                item.Resize(5000);
                cf.Commit();
            }

            using (CompoundFile cf = new("$Test_RESIZE_STREAM_TRANSITION_TO_NORMAL.cfs", CFSUpdateMode.ReadOnly, CFSConfiguration.Default))
            {
                CFStream item = cf.RootStorage.GetStream("AStream");
                Assert.IsNotNull(item);
                Assert.AreEqual(5000, item.Size);

                byte[] buffer = new byte[2048];
                item.Read(buffer, 0, 2048);
                CollectionAssert.AreEqual(b, buffer);
            }
        }

        [TestMethod]
        public void Test_RESIZE_MINISTREAM_NO_TRANSITION_MOD()
        {
            int INITIAL_SIZE = 1024 * 2;
            int SIZE_DELTA = 148;

            byte[] b = Helpers.GetBuffer(INITIAL_SIZE);

            using (CompoundFile cf = new(CFSVersion.Ver_3, CFSConfiguration.Default))
            {
                cf.RootStorage.AddStream("MiniStream").SetData(b);
                cf.SaveAs("$Test_RESIZE_MINISTREAM.cfs");
            }

            using (CompoundFile cf = new("$Test_RESIZE_MINISTREAM.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors))
            {
                CFStream item = cf.RootStorage.GetStream("MiniStream");
                item.Resize(item.Size - SIZE_DELTA);
                cf.Commit();
            }

            using (CompoundFile cf = new("$Test_RESIZE_MINISTREAM.cfs", CFSUpdateMode.ReadOnly, CFSConfiguration.Default))
            {
                CFStream st = cf.RootStorage.GetStream("MiniStream");
                Assert.IsNotNull(st);
                Assert.AreEqual(INITIAL_SIZE - SIZE_DELTA, st.Size);

                byte[] buffer = new byte[INITIAL_SIZE - SIZE_DELTA];
                st.Read(buffer, 0, buffer.Length);
                CollectionAssert.AreEqual(b.Take(buffer.Length).ToList(), buffer);
            }
        }

        [TestMethod]
        public void Test_RESIZE_MINISTREAM_SECTOR_RECYCLE()
        {
            byte[] b = Helpers.GetBuffer(1024 * 2);

            using (CompoundFile cf = new(CFSVersion.Ver_3, CFSConfiguration.Default))
            {
                cf.RootStorage.AddStream("MiniStream").SetData(b);
                cf.SaveAs("$Test_RESIZE_MINISTREAM_RECYCLE.cfs");
            }

            using (CompoundFile cf = new("$Test_RESIZE_MINISTREAM_RECYCLE.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors))
            {
                CFStream item = cf.RootStorage.GetStream("MiniStream");
                item.Resize(item.Size / 2);
                cf.Commit();
            }

            using (CompoundFile cf = new("$Test_RESIZE_MINISTREAM_RECYCLE.cfs", CFSUpdateMode.ReadOnly, CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors))
            {
                CFStream st = cf.RootStorage.AddStream("ANewStream");
                st.SetData(Helpers.GetBuffer(400));
                cf.SaveAs("$Test_RESIZE_MINISTREAM_RECYCLE2.cfs");
            }

            Assert.AreEqual(
                new FileInfo("$Test_RESIZE_MINISTREAM_RECYCLE.cfs").Length,
                new FileInfo("$Test_RESIZE_MINISTREAM_RECYCLE2.cfs").Length);
        }

        [TestMethod]
        public void Test_DELETE_STREAM_SECTOR_REUSE()
        {
            byte[] b = Helpers.GetBuffer(1024 * 1024 * 2); // 2MB buffer

            using (CompoundFile cf = new(CFSVersion.Ver_4, CFSConfiguration.Default))
            {
                CFStream st = cf.RootStorage.AddStream("AStream");
                st.Append(b);
                cf.SaveAs("SectorRecycle.cfs");
            }

            using (CompoundFile cf = new("SectorRecycle.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle))
            {
                cf.RootStorage.Delete("AStream");
                cf.Commit(true);
            }

            using (CompoundFile cf = new("SectorRecycle.cfs", CFSUpdateMode.ReadOnly, CFSConfiguration.Default)) // No sector recycle
            {
                CFStream st = cf.RootStorage.AddStream("BStream");
                st.Append(Helpers.GetBuffer(1024 * 1024 * 1));
                cf.SaveAs("SectorRecycleLarger.cfs");
            }

            Assert.IsFalse(new FileInfo("SectorRecycle.cfs").Length >= new FileInfo("SectorRecycleLarger.cfs").Length);

            using (CompoundFile cf = new("SectorRecycle.cfs", CFSUpdateMode.ReadOnly, CFSConfiguration.SectorRecycle))
            {
                CFStream st = cf.RootStorage.AddStream("BStream");
                st.Append(Helpers.GetBuffer(1024 * 1024 * 1));
                cf.SaveAs("SectorRecycleSmaller.cfs");
            }

            long larger = new FileInfo("SectorRecycle.cfs").Length;
            long smaller = new FileInfo("SectorRecycleSmaller.cfs").Length;

            Assert.IsTrue(larger >= smaller, $"Larger size: {larger} - Smaller size: {smaller}");
        }

        [TestMethod]
        public void TEST_STREAM_VIEW()
        {
            List<Sector> temp = new List<Sector>();
            Sector s = new Sector(512);
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, s.GetData(), 0, 4);
            temp.Add(s);

            using StreamView sv = new(temp, 512, 4, null, null);
            using BinaryReader br = new(sv);
            int t = br.ReadInt32();

            Assert.AreEqual(1, t);
        }

        [TestMethod]
        public void Test_STREAM_VIEW_2()
        {
            List<Sector> temp = new List<Sector>();

            using StreamView sv = new(temp, 512, null);
            sv.Write(BitConverter.GetBytes(1), 0, 4);
            sv.Seek(0, SeekOrigin.Begin);
            using BinaryReader br = new(sv);
            int t = br.ReadInt32();

            Assert.AreEqual(1, t);
        }

        /// <summary>
        /// Write a sequence of Int32 greater than sector size,
        /// read and compare.
        /// </summary>
        [TestMethod]
        public void Test_STREAM_VIEW_3()
        {
            List<Sector> temp = new List<Sector>();

            using StreamView sv = new(temp, 512, null);
            for (int i = 0; i < 200; i++)
            {
                sv.Write(BitConverter.GetBytes(i), 0, 4);
            }

            sv.Seek(0, SeekOrigin.Begin);
            using BinaryReader br = new(sv);
            for (int i = 0; i < 200; i++)
            {
                Assert.AreEqual(i, br.ReadInt32(), "Failed with " + i.ToString());
            }
        }

        /// <summary>
        /// Write a sequence of Int32 greater than sector size,
        /// read and compare.
        /// </summary>
        [TestMethod]
        public void Test_STREAM_VIEW_LARGE_DATA()
        {
            List<Sector> temp = new List<Sector>();

            using StreamView sv = new(temp, 512, null);
            for (int i = 0; i < 200; i++)
            {
                sv.Write(BitConverter.GetBytes(i), 0, 4);
            }

            sv.Seek(0, SeekOrigin.Begin);
            using BinaryReader br = new(sv);
            for (int i = 0; i < 200; i++)
            {
                Assert.AreEqual(i, br.ReadInt32(), "Failed with " + i.ToString());
            }
        }

        /// <summary>
        /// Write a sequence of Int32 greater than sector size,
        /// read and compare.
        /// </summary>
        [TestMethod]
        public void Test_CHANGE_STREAM_NAME_FIX_54()
        {
            using (CompoundFile cf = new("report.xls", CFSUpdateMode.ReadOnly, CFSConfiguration.Default))
            {
                cf.RootStorage.RenameItem("Workbook", "Workbuk");
                cf.SaveAs("report_n.xls");
            }

            using (CompoundFile cf2 = new("report_n.xls", CFSUpdateMode.Update, CFSConfiguration.Default))
            {
                cf2.RootStorage.RenameItem("Workbuk", "Workbook");
                cf2.Commit();
            }

            using (CompoundFile cf3 = new("MultipleStorage.cfs", CFSUpdateMode.Update, CFSConfiguration.Default))
            {
                cf3.RootStorage.RenameItem("MyStorage", "MyNewStorage");
                cf3.Commit();
            }

            using CompoundFile cf4 = new("MultipleStorage.cfs", CFSUpdateMode.Update, CFSConfiguration.Default);
            cf4.RootStorage.RenameItem("MyNewStorage", "MyStorage");
            cf4.Commit();
        }

        /// <summary>
        /// Resize without transition to smaller chain has a wrong behavior
        /// </summary>
        [TestMethod]
        public void TEST_RESIZE_STREAM_BUG_119()
        {
            const string DATA = "data";
            const int size = 10;
            using MemoryStream ms = new();

            using (CompoundFile cf = new())
            {
                CFStream st = cf.RootStorage.AddStream(DATA);
                var data = Enumerable.Range(0, size).Select(v => (byte)v).ToArray();
                st.SetData(data);
                cf.Save(ms);
            }

            ms.Position = 0;
            using (CompoundFile cf = new(ms, CFSUpdateMode.Update, CFSConfiguration.Default))
            {
                CFStream st = cf.RootStorage.GetStream(DATA);
                byte[] buffer = new byte[size];
                st.Read(buffer, 0, size);
                st.Resize(5);   // <- can be any number smaller than the current size
                st.Write(new byte[] { 0 }, 0);
                cf.Commit();
            }
        }
    }
}
