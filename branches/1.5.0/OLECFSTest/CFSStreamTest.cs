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

            cf.Close();
        }

        [TestMethod]
        public void Test_WRITE_STREAM()
        {
            byte[] b = new byte[10000];
            for (int i = 0; i < 10000; i++)
            {
                b[i % 120] = (byte)i;
            }

            CompoundFile cf = new CompoundFile();
            CFStream myStream = cf.RootStorage.AddStream("MyStream");

            Assert.IsNotNull(myStream);
            myStream.SetData(b);
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
                myStream.SetData(b);
            }
            catch
            {
                Assert.Fail("Failed setting zero length stream");
            }

            cf.Save("ZERO_LENGTH_STREAM.cfs");
            cf.Close();
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

        }


        [TestMethod]
        public void Test_WRITE_STREAM_WITH_DIFAT()
        {
            const int SIZE = 15388609; //Incredible condition of 'resonance' between FAT and DIFAT sec number
            //const int SIZE = 15345665; // 64 -> 65 NOT working
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
        }


        [TestMethod]
        public void Test_WRITE_MINISTREAM_READ_REWRITE_STREAM()
        {
            const int BIGGER_SIZE = 350;
            //const int SMALLER_SIZE = 290;
            const int MEGA_SIZE = 18000000;

            byte[] ba = Helpers.GetBuffer(BIGGER_SIZE, 10);
            byte[] ba2 = Helpers.GetBuffer(BIGGER_SIZE, 2);
            byte[] ba3 = Helpers.GetBuffer(BIGGER_SIZE, 3);
            byte[] ba4 = Helpers.GetBuffer(BIGGER_SIZE, 4);
            byte[] ba5 = Helpers.GetBuffer(BIGGER_SIZE, 5);

            //WRITE 5 (mini)streams in a compound file --

            CompoundFile cfa = new CompoundFile();

            CFStream myStream = cfa.RootStorage.AddStream("MyFirstStream");
            Assert.IsNotNull(myStream);

            myStream.SetData(ba);
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
        }

        [TestMethod]
        public void Test_RE_WRITE_SMALLER_STREAM()
        {
            String filename = "report.xls";

            byte[] b = new byte[8000];
            for (int i = 0; i < 8000; i++)
            {
                b[i % 120] = (byte)i;
            }

            CompoundFile cf = new CompoundFile(filename);
            CFStream foundStream = cf.RootStorage.GetStream("Workbook");
            foundStream.SetData(b);

            cf.Close();

        }

        [TestMethod]
        public void Test_RE_WRITE_SMALLER_MINI_STREAM()
        {
            String filename = "report.xls";

            CompoundFile cf = new CompoundFile(filename);
            CFStream foundStream = cf.RootStorage.GetStream("\x05SummaryInformation");
            byte[] b = new byte[foundStream.Size];
            foundStream.SetData(b);

            cf.Save("RE_WRITE_SMALLER_MINI_STREAM.xls");
            cf.Close();
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
        }

        [TestMethod]
        public void Test_TRANSACTED_ADD_MINISTREAM_TO_EXISTING_FILE()
        {
            String srcFilename = "report.xls";
            String dstFilename = "reportOverwriteMultiple.xls";

            File.Copy(srcFilename, dstFilename, true);

            CompoundFile cf = new CompoundFile(dstFilename, UpdateMode.Update, true, true);

            Random r = new Random();

            byte[] buffer = Helpers.GetBuffer(r.Next(3, 4095), 0x0A);

            cf.RootStorage.AddStream("MyStream").SetData(buffer);
            cf.Commit();
            cf.Close();
            FileStream larger = new FileStream(dstFilename, FileMode.Open);
            FileStream smaller = new FileStream(srcFilename, FileMode.Open);

            Assert.IsTrue(larger.Length > smaller.Length);
            larger.Close();
            smaller.Close();

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

            CompoundFile cf2 = new CompoundFile(filename);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");

            Assert.IsNotNull(sm2);
            Assert.IsTrue(sm2.Size == 220);
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
            for (int i = 0; i < 4; i++)
            {
                cfst.AppendData(b);
                cf.Commit(true);
                //Console.WriteLine("     Updated " + i.ToString());
                //Console.ReadKey();
            }

            //cfst.AppendData(cmp);
            //cf.Commit(true);

            //cf.Close();


            //cf = new CompoundFile("MEGALARGESSIMUSFILE.cfs");
            //int count = 8;
            //byte[] data = cf.RootStorage.GetStream("MySuperLargeStream").GetData((long)b.Length * 5L, ref count);

            cf.Close();

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
