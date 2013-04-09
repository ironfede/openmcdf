using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMcdf;
using System.IO;

namespace OpenMcdfTest
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class CFSStreamTest
    {

        //const String TestContext.TestDir = "C:\\TestOutputFiles\\";

        public CFSStreamTest()
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
        public void Test_READ_STREAM()
        {
            String filename = "report.xls";

            CompoundFile cf = new CompoundFile(filename);
            CFStream foundStream = cf.RootStorage.GetStream("Workbook");

            byte[] temp = foundStream.GetData();

            Assert.IsNotNull(temp);
            Assert.IsTrue(temp.Length > 0);

            cf.Close();
        }

        [TestMethod]
        public void Test_WRITE_STREAM()
        {
            const int BUFFER_LENGTH = 10000;

            byte[] b = Helpers.GetBuffer(BUFFER_LENGTH);

            CompoundFile cf = new CompoundFile();
            CFStream myStream = cf.RootStorage.AddStream("MyStream");

            Assert.IsNotNull(myStream);
            Assert.IsTrue(myStream.Size == 0);

            myStream.SetData(b);

            Assert.IsTrue(myStream.Size == BUFFER_LENGTH, "Stream size differs from buffer size");

            cf.Close();
        }

        [TestMethod]
        public void Test_WRITE_MINI_STREAM()
        {
            const int BUFFER_LENGTH = 1023; // < 4096

            byte[] b = Helpers.GetBuffer(BUFFER_LENGTH);

            CompoundFile cf = new CompoundFile();
            CFStream myStream = cf.RootStorage.AddStream("MyMiniStream");

            Assert.IsNotNull(myStream);
            Assert.IsTrue(myStream.Size == 0);

            myStream.SetData(b);

            Assert.IsTrue(myStream.Size == BUFFER_LENGTH, "Mini Stream size differs from buffer size");

            cf.Close();
        }

        [TestMethod]
        public void Test_ZERO_LENGTH_WRITE_STREAM()
        {
            byte[] b = new byte[0];

            CompoundFile cf = new CompoundFile();
            CFStream myStream = cf.RootStorage.AddStream("MyStream");

            Assert.IsNotNull(myStream);

            try
            {
                myStream.SetData(b); cf.Save("ZERO_LENGTH_STREAM.cfs");
            }
            catch
            {
                Assert.Fail("Failed setting zero length stream");
            }
            finally
            {
                if (cf != null)
                    cf.Close();
            }

            if (File.Exists("ZERO_LENGTH_STREAM.cfs"))
                File.Delete("ZERO_LENGTH_STREAM.cfs");

        }

        [TestMethod]
        public void Test_ZERO_LENGTH_RE_WRITE_STREAM()
        {
            byte[] b = new byte[0];

            CompoundFile cf = new CompoundFile();
            CFStream myStream = cf.RootStorage.AddStream("MyStream");

            Assert.IsNotNull(myStream);

            try
            {
                myStream.SetData(b);
            }
            catch
            {
                Assert.Fail("Failed setting zero length stream");
            }

            cf.Save("ZERO_LENGTH_STREAM_RE.cfs");
            cf.Close();

            CompoundFile cfo = new CompoundFile("ZERO_LENGTH_STREAM_RE.cfs");
            CFStream oStream = cfo.RootStorage.GetStream("MyStream");

            Assert.IsNotNull(oStream);
            Assert.IsTrue(oStream.Size == 0);

            try
            {
                oStream.SetData(Helpers.GetBuffer(30));
                cfo.Save("ZERO_LENGTH_STREAM_RE2.cfs");
            }
            catch
            {
                Assert.Fail("Failed re-writing zero length stream");
            }
            finally
            {
                cfo.Close();
            }

            if (File.Exists("ZERO_LENGTH_STREAM_RE.cfs"))
                File.Delete("ZERO_LENGTH_STREAM_RE.cfs");

            if (File.Exists("ZERO_LENGTH_STREAM_RE2.cfs"))
                File.Delete("ZERO_LENGTH_STREAM_RE2.cfs");

        }


        [TestMethod]
        public void Test_WRITE_STREAM_WITH_DIFAT()
        {
            //const int SIZE = 15388609; //Incredible condition of 'resonance' between FAT and DIFAT sec number
            const int SIZE = 15345665; // 64 -> 65 NOT working (in the past ;-)  )
            byte[] b = Helpers.GetBuffer(SIZE, 0);

            CompoundFile cf = new CompoundFile();
            CFStream myStream = cf.RootStorage.AddStream("MyStream");
            Assert.IsNotNull(myStream);
            myStream.SetData(b);

            cf.Save("WRITE_STREAM_WITH_DIFAT.cfs");
            cf.Close();


            CompoundFile cf2 = new CompoundFile("WRITE_STREAM_WITH_DIFAT.cfs");
            CFStream st = cf2.RootStorage.GetStream("MyStream");

            Assert.IsNotNull(cf2);
            Assert.IsTrue(st.Size == SIZE);

            Assert.IsTrue(Helpers.CompareBuffer(b, st.GetData()));

            cf2.Close();

            if (File.Exists("WRITE_STREAM_WITH_DIFAT.cfs"))
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

            CompoundFile cfa = new CompoundFile();

            CFStream myStream = cfa.RootStorage.AddStream("MyFirstStream");
            Assert.IsNotNull(myStream);

            myStream.SetData(ba1);
            Assert.IsTrue(myStream.Size == BIGGER_SIZE);

            CFStream myStream2 = cfa.RootStorage.AddStream("MySecondStream");
            Assert.IsNotNull(myStream2);

            myStream2.SetData(ba2);
            Assert.IsTrue(myStream2.Size == BIGGER_SIZE);

            CFStream myStream3 = cfa.RootStorage.AddStream("MyThirdStream");
            Assert.IsNotNull(myStream3);

            myStream3.SetData(ba3);
            Assert.IsTrue(myStream3.Size == BIGGER_SIZE);

            CFStream myStream4 = cfa.RootStorage.AddStream("MyFourthStream");
            Assert.IsNotNull(myStream4);

            myStream4.SetData(ba4);
            Assert.IsTrue(myStream4.Size == BIGGER_SIZE);

            CFStream myStream5 = cfa.RootStorage.AddStream("MyFifthStream");
            Assert.IsNotNull(myStream5);

            myStream5.SetData(ba5);
            Assert.IsTrue(myStream5.Size == BIGGER_SIZE);

            cfa.Save("WRITE_MINISTREAM_READ_REWRITE_STREAM.cfs");

            cfa.Close();

            // Now get the second stream and rewrite it smaller
            byte[] bb = Helpers.GetBuffer(MEGA_SIZE);
            CompoundFile cfb = new CompoundFile("WRITE_MINISTREAM_READ_REWRITE_STREAM.cfs");
            CFStream myStreamB = cfb.RootStorage.GetStream("MySecondStream");
            Assert.IsNotNull(myStreamB);
            myStreamB.SetData(bb);
            Assert.IsTrue(myStreamB.Size == MEGA_SIZE);

            byte[] bufferB = myStreamB.GetData();
            cfb.Save("WRITE_MINISTREAM_READ_REWRITE_STREAM_2ND.cfs");
            cfb.Close();

            CompoundFile cfc = new CompoundFile("WRITE_MINISTREAM_READ_REWRITE_STREAM_2ND.cfs");
            CFStream myStreamC = cfc.RootStorage.GetStream("MySecondStream");
            Assert.IsTrue(myStreamC.Size == MEGA_SIZE, "DATA SIZE FAILED");

            byte[] bufferC = myStreamC.GetData();
            Assert.IsTrue(Helpers.CompareBuffer(bufferB, bufferC), "DATA INTEGRITY FAILED");

            cfc.Close();

            if (File.Exists("WRITE_MINISTREAM_READ_REWRITE_STREAM.cfs"))
                File.Delete("WRITE_MINISTREAM_READ_REWRITE_STREAM.cfs");


            if (File.Exists("WRITE_MINISTREAM_READ_REWRITE_STREAM_2ND.cfs"))
                File.Delete("WRITE_MINISTREAM_READ_REWRITE_STREAM_2ND.cfs");

        }

        [TestMethod]
        public void Test_RE_WRITE_SMALLER_STREAM()
        {
            const int BUFFER_LENGTH = 8000;

            String filename = "report.xls";

            byte[] b = Helpers.GetBuffer(BUFFER_LENGTH);

            CompoundFile cf = new CompoundFile(filename);
            CFStream foundStream = cf.RootStorage.GetStream("Workbook");
            foundStream.SetData(b);
            cf.Save("reportRW_SMALL.xls");
            cf.Close();

            cf = new CompoundFile("reportRW_SMALL.xls");
            byte[] c = cf.RootStorage.GetStream("Workbook").GetData();
            Assert.IsTrue(c.Length == BUFFER_LENGTH);
            cf.Close();

            if (File.Exists("reportRW_SMALL.xls"))
                File.Delete("reportRW_SMALL.xls");

        }

        [TestMethod]
        public void Test_RE_WRITE_SMALLER_MINI_STREAM()
        {
            String filename = "report.xls";

            CompoundFile cf = new CompoundFile(filename);
            CFStream foundStream = cf.RootStorage.GetStream("\x05SummaryInformation");
            int TEST_LENGTH = (int)foundStream.Size - 20;
            byte[] b = Helpers.GetBuffer(TEST_LENGTH);
            foundStream.SetData(b);

            cf.Save("RE_WRITE_SMALLER_MINI_STREAM.xls");
            cf.Close();

            cf = new CompoundFile("RE_WRITE_SMALLER_MINI_STREAM.xls");
            byte[] c = cf.RootStorage.GetStream("\x05SummaryInformation").GetData();
            Assert.IsTrue(c.Length == TEST_LENGTH);
            cf.Close();

            if (File.Exists("RE_WRITE_SMALLER_MINI_STREAM.xls"))
                File.Delete("RE_WRITE_SMALLER_MINI_STREAM.xls");

        }

        [TestMethod]
        public void Test_TRANSACTED_ADD_STREAM_TO_EXISTING_FILE()
        {
            String srcFilename = "report.xls";
            String dstFilename = "reportOverwrite.xls";

            File.Copy(srcFilename, dstFilename, true);

            CompoundFile cf = new CompoundFile(dstFilename, UpdateMode.Update, false, false);

            byte[] buffer = Helpers.GetBuffer(5000);

            CFStream addedStream = cf.RootStorage.AddStream("MyNewStream");
            addedStream.SetData(buffer);

            cf.Commit();
            cf.Close();


            if (File.Exists("reportOverwrite.xls"))
                File.Delete("reportOverwrite.xls");

        }

        [TestMethod]
        public void Test_TRANSACTED_ADD_REMOVE_MULTIPLE_STREAM_TO_EXISTING_FILE()
        {
            String srcFilename = "report.xls";
            String dstFilename = "reportOverwriteMultiple.xls";

            File.Copy(srcFilename, dstFilename, true);

            CompoundFile cf = new CompoundFile(dstFilename, UpdateMode.ReadOnly, true, false);

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

                Assert.IsTrue(Helpers.CompareBuffer(addedStream.GetData(), buffer), "Data buffer corrupted");

                // Random commit, not on single addition
                //if (r.Next(0, 100) > 50)
                //    cf.UpdateFile();

            }

            cf.Save(dstFilename + "PP");
            cf.Close();

            if (File.Exists("reportOverwriteMultiple.xls"))
                File.Delete("reportOverwriteMultiple.xls");

            if (File.Exists("reportOverwriteMultiple.xlsPP"))
                File.Delete("reportOverwriteMultiple.xlsPP");


        }

        [TestMethod]
        public void Test_TRANSACTED_ADD_MINISTREAM_TO_EXISTING_FILE()
        {
            String srcFilename = "report.xls";
            String dstFilename = "reportOverwriteMultiple.xls";

            File.Copy(srcFilename, dstFilename, true);

            CompoundFile cf = new CompoundFile(dstFilename, UpdateMode.Update, true, true);

            Random r = new Random();

            byte[] buffer = Helpers.GetBuffer(31, 0x0A);

            cf.RootStorage.AddStream("MyStream").SetData(buffer);
            cf.Commit();
            cf.Close();
            FileStream larger = new FileStream(dstFilename, FileMode.Open);
            FileStream smaller = new FileStream(srcFilename, FileMode.Open);

            // Equal condition if minisector can be "allocated"
            // within the existing standard sector border
            Assert.IsTrue(larger.Length >= smaller.Length);

            larger.Close();
            smaller.Close();

            if (File.Exists("reportOverwriteMultiple.xlsPP"))
                File.Delete("reportOverwriteMultiple.xlsPP");


        }

        [TestMethod]
        public void Test_TRANSACTED_REMOVE_MINI_STREAM_ADD_MINISTREAM_TO_EXISTING_FILE()
        {
            String srcFilename = "report.xls";
            String dstFilename = "reportOverwrite2.xls";

            File.Copy(srcFilename, dstFilename, true);

            CompoundFile cf = new CompoundFile(dstFilename, UpdateMode.Update, true, true);

            cf.RootStorage.Delete("\x05SummaryInformation");

            byte[] buffer = Helpers.GetBuffer(2000);

            CFStream addedStream = cf.RootStorage.AddStream("MyNewStream");
            addedStream.SetData(buffer);

            cf.Commit();
            cf.Close();

            if (File.Exists("reportOverwrite2.xlsPP"))
                File.Delete("reportOverwrite2.xlsPP");


        }



        //[TestMethod]
        //public void Test_DELETE_STREAM_1()
        //{
        //    String filename = "MultipleStorage.cfs";

        //    CompoundFile cf = new CompoundFile(filename);
        //    CFStorage cfs = cf.RootStorage.GetStorage("MyStorage");
        //    cfs.DeleteStream("MySecondStream");

        //    cf.Save(TestContext.TestDir + "MultipleStorage_REMOVED_STREAM_1.cfs");
        //    cf.Close();
        //}

        //[TestMethod]
        //public void Test_DELETE_STREAM_2()
        //{
        //    String filename = "MultipleStorage.cfs";

        //    CompoundFile cf = new CompoundFile(filename);
        //    CFStorage cfs = cf.RootStorage.GetStorage("MyStorage").GetStorage("AnotherStorage");

        //    cfs.DeleteStream("AnotherStream");

        //    cf.Save(TestContext.TestDir + "MultipleStorage_REMOVED_STREAM_2.cfs");

        //    cf.Close();
        //}


        [TestMethod]
        public void Test_WRITE_AND_READ_CFS()
        {
            String filename = "WRITE_AND_READ_CFS.cfs";

            CompoundFile cf = new CompoundFile();

            CFStorage st = cf.RootStorage.AddStorage("MyStorage");
            CFStream sm = st.AddStream("MyStream");
            byte[] b = Helpers.GetBuffer(220, 0x0A);
            sm.SetData(b);

            cf.Save(filename);
            cf.Close();

            CompoundFile cf2 = new CompoundFile(filename);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");
            cf2.Close();

            Assert.IsNotNull(sm2);
            Assert.IsTrue(sm2.Size == 220);


            if (File.Exists(filename))
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
            byte[] b = new byte[0];

            CompoundFile cf = new CompoundFile();

            string zeroLengthName = "MyZeroStream";
            CFStream myStream = cf.RootStorage.AddStream(zeroLengthName);

            Assert.IsNotNull(myStream);

            try
            {
                myStream.SetData(b);
            }
            catch
            {
                Assert.Fail("Failed setting zero length stream");
            }

            string filename = "DeleteZeroLengthStream.cfs";
            cf.Save(filename);
            cf.Close();

            CompoundFile cf2 = new CompoundFile(filename);

            // Execption in next line!
            cf2.RootStorage.Delete(zeroLengthName);

            CFStream zeroStream2 = null;

            try
            {
                zeroStream2 = cf2.RootStorage.GetStream(zeroLengthName);
            }
            catch (Exception ex)
            {
                Assert.IsNull(zeroStream2);
                Assert.IsInstanceOfType(ex, typeof(CFItemNotFound));
            }

            cf2.Save("MultipleDeleteMiniStream.cfs");
            cf2.Close();
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

        private void SingleTransactedChange(int size)
        {

            String filename = "INCREMENTAL_SIZE_MULTIPLE_WRITE_AND_READ_CFS.cfs";

            if (File.Exists(filename))
                File.Delete(filename);

            CompoundFile cf = new CompoundFile();
            CFStorage st = cf.RootStorage.AddStorage("MyStorage");
            CFStream sm = st.AddStream("MyStream");

            byte[] b = Helpers.GetBuffer(size);

            sm.SetData(b);
            cf.Save(filename);
            cf.Close();

            CompoundFile cf2 = new CompoundFile(filename);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");

            Assert.IsNotNull(sm2);
            Assert.IsTrue(sm2.Size == size);
            Assert.IsTrue(Helpers.CompareBuffer(sm2.GetData(), b));

            cf2.Close();
        }

        private void SingleWriteReadMatching(int size)
        {

            String filename = "INCREMENTAL_SIZE_MULTIPLE_WRITE_AND_READ_CFS.cfs";

            if (File.Exists(filename))
                File.Delete(filename);

            CompoundFile cf = new CompoundFile();
            CFStorage st = cf.RootStorage.AddStorage("MyStorage");
            CFStream sm = st.AddStream("MyStream");

            byte[] b = Helpers.GetBuffer(size);

            sm.SetData(b);
            cf.Save(filename);
            cf.Close();

            CompoundFile cf2 = new CompoundFile(filename);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");

            Assert.IsNotNull(sm2);
            Assert.IsTrue(sm2.Size == size);
            Assert.IsTrue(Helpers.CompareBuffer(sm2.GetData(), b));

            cf2.Close();
        }

        private void SingleWriteReadMatchingSTREAMED(int size)
        {
            MemoryStream ms = new MemoryStream(size);

            CompoundFile cf = new CompoundFile();
            CFStorage st = cf.RootStorage.AddStorage("MyStorage");
            CFStream sm = st.AddStream("MyStream");

            byte[] b = Helpers.GetBuffer(size);

            sm.SetData(b);
            cf.Save(ms);
            cf.Close();

            CompoundFile cf2 = new CompoundFile(ms);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");

            Assert.IsNotNull(sm2);
            Assert.IsTrue(sm2.Size == size);
            Assert.IsTrue(Helpers.CompareBuffer(sm2.GetData(), b));

            cf2.Close();
        }


        [TestMethod]
        public void Test_APPEND_DATA_TO_STREAM()
        {
            MemoryStream ms = new MemoryStream();

            byte[] b = new byte[] { 0x0, 0x1, 0x2, 0x3 };
            byte[] b2 = new byte[] { 0x4, 0x5, 0x6, 0x7 };

            CompoundFile cf = new CompoundFile();
            CFStream st = cf.RootStorage.AddStream("MyLargeStream");
            st.SetData(b);
            st.AppendData(b2);

            cf.Save(ms);
            cf.Close();

            byte[] cmp = new byte[] { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7 };
            cf = new CompoundFile(ms);
            byte[] data = cf.RootStorage.GetStream("MyLargeStream").GetData();
            Assert.IsTrue(Helpers.CompareBuffer(cmp, data));

        }

        [TestMethod]
        public void Test_COPY_FROM_STREAM()
        {
            byte[] b = Helpers.GetBuffer(100);
            MemoryStream ms = new MemoryStream(b);

            CompoundFile cf = new CompoundFile();
            CFStream st = cf.RootStorage.AddStream("MyImportedStream");
            st.CopyFrom(ms);
            ms.Close();
            cf.Save("COPY_FROM_STREAM.cfs");
            cf.Close();

            cf = new CompoundFile("COPY_FROM_STREAM.cfs");
            byte[] data = cf.RootStorage.GetStream("MyImportedStream").GetData();

            Assert.IsTrue(Helpers.CompareBuffer(b, data));

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
            Assert.IsTrue(Helpers.CompareBuffer(cmp, data));
            cf.Close();

        }
#endif

        [TestMethod]
        public void Test_DELETE_STREAM_SECTOR_REUSE()
        {
            CompoundFile cf = null;
            CFStream st = null;

            byte[] b = Helpers.GetBuffer(1024 * 1024 * 2); //2MB buffer
            //byte[] b = Helpers.GetBuffer(5000); 
            byte[] cmp = new byte[] { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7 };

            cf = new CompoundFile(CFSVersion.Ver_4, false, false);
            st = cf.RootStorage.AddStream("AStream");
            st.AppendData(b);
            cf.Save("SectorRecycle.cfs");
            cf.Close();


            cf = new CompoundFile("SectorRecycle.cfs", UpdateMode.Update, true, false);
            cf.RootStorage.Delete("AStream");
            cf.Commit(true);
            cf.Close();

            cf = new CompoundFile("SectorRecycle.cfs", UpdateMode.ReadOnly, false, false); //No sector recycle
            st = cf.RootStorage.AddStream("BStream");
            st.AppendData(Helpers.GetBuffer(1024 * 1024 * 1));
            cf.Save("SectorRecycleLarger.cfs");
            cf.Close();

            Assert.IsFalse((new FileInfo("SectorRecycle.cfs").Length) >= (new FileInfo("SectorRecycleLarger.cfs").Length));

            cf = new CompoundFile("SectorRecycle.cfs", UpdateMode.ReadOnly, true, false);
            st = cf.RootStorage.AddStream("BStream");
            st.AppendData(Helpers.GetBuffer(1024 * 1024 * 1));
            cf.Save("SectorRecycleSmaller.cfs");
            cf.Close();

            Assert.IsTrue((new FileInfo("SectorRecycle.cfs").Length) >= (new FileInfo("SectorRecycleSmaller.cfs").Length));

        }



        [TestMethod]
        public void TEST_STREAM_VIEW()
        {
            Stream a = null;
            List<Sector> temp = new List<Sector>();
            Sector s = new Sector(512);
            Buffer.BlockCopy(BitConverter.GetBytes((int)1), 0, s.GetData(), 0, 4);
            temp.Add(s);

            StreamView sv = new StreamView(temp, 512, 0, a);
            BinaryReader br = new BinaryReader(sv);
            Int32 t = br.ReadInt32();

            Assert.IsTrue(t == 1);
        }


        [TestMethod]
        public void Test_STREAM_VIEW_2()
        {
            Stream b = null;
            List<Sector> temp = new List<Sector>();

            StreamView sv = new StreamView(temp, 512, b);
            sv.Write(BitConverter.GetBytes(1), 0, 4);
            sv.Seek(0, SeekOrigin.Begin);
            BinaryReader br = new BinaryReader(sv);
            Int32 t = br.ReadInt32();

            Assert.IsTrue(t == 1);
        }


        /// <summary>
        /// Write a sequence of Int32 greater than sector size,
        /// read and compare.
        /// </summary>
        [TestMethod]
        public void Test_STREAM_VIEW_3()
        {
            Stream b = null;
            List<Sector> temp = new List<Sector>();

            StreamView sv = new StreamView(temp, 512, b);

            for (int i = 0; i < 200; i++)
            {
                sv.Write(BitConverter.GetBytes(i), 0, 4);
            }

            sv.Seek(0, SeekOrigin.Begin);
            BinaryReader br = new BinaryReader(sv);

            for (int i = 0; i < 200; i++)
            {
                Assert.IsTrue(i == br.ReadInt32(), "Failed with " + i.ToString());
            }
        }

        /// <summary>
        /// Write a sequence of Int32 greater than sector size,
        /// read and compare.
        /// </summary>
        [TestMethod]
        public void Test_STREAM_VIEW_LARGE_DATA()
        {
            Stream b = null;
            List<Sector> temp = new List<Sector>();

            StreamView sv = new StreamView(temp, 512, b);

            for (int i = 0; i < 200; i++)
            {
                sv.Write(BitConverter.GetBytes(i), 0, 4);
            }

            sv.Seek(0, SeekOrigin.Begin);
            BinaryReader br = new BinaryReader(sv);

            for (int i = 0; i < 200; i++)
            {
                Assert.IsTrue(i == br.ReadInt32(), "Failed with " + i.ToString());
            }
        }



    }
}
