using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace OpenMcdf.Test
{
    /// <summary>
    /// Summary description for CompoundFileTest
    /// </summary>
    [TestClass]
    public class CompoundFileTest
    {
        public CompoundFileTest()
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
        public void Test_COMPRESS_SPACE()
        {
            string FILENAME = "MultipleStorage3.cfs"; // 22Kb

            FileInfo srcFile = new FileInfo(FILENAME);

            File.Copy(FILENAME, "MultipleStorage_Deleted_Compress.cfs", true);

            CompoundFile cf = new CompoundFile("MultipleStorage_Deleted_Compress.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors);

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
            Assert.AreEqual(maxCharactersStorageName, strg.Name);

            // Try Stream entry name with max characters.
            Assert.IsNotNull(cf.RootStorage.AddStream(maxCharactersStreamName));
            CFStream strm = cf.RootStorage.GetStream(maxCharactersStreamName);
            Assert.IsNotNull(strm);
            Assert.AreEqual(maxCharactersStreamName, strm.Name);

            string tooManyCharactersEntryName = "12345678901234567890123456789012"; // 32 chars

            Assert.ThrowsException<CFException>(() => cf.RootStorage.AddStorage(tooManyCharactersEntryName));

            Assert.ThrowsException<CFException>(() => cf.RootStorage.AddStream(tooManyCharactersEntryName));

            cf.SaveAs("EntryNameLength");
            cf.Close();
        }

        [TestMethod]
        public void Test_DELETE_WITHOUT_COMPRESSION()
        {
            string FILENAME = "MultipleStorage3.cfs";

            FileInfo srcFile = new FileInfo(FILENAME);

            CompoundFile cf = new CompoundFile(FILENAME);

            CFStorage st = cf.RootStorage.GetStorage("MyStorage");
            st = st.GetStorage("AnotherStorage");

            Assert.IsNotNull(st);

            st.Delete("Another2Stream"); //17Kb

            //cf.CompressFreeSpace();
            cf.SaveAs("MultipleStorage_Deleted_Compress.cfs");

            cf.Close();
            FileInfo dstFile = new FileInfo("MultipleStorage_Deleted_Compress.cfs");

            Assert.IsFalse(srcFile.Length > dstFile.Length);
        }

        [TestMethod]
        public void Test_WRITE_AND_READ_CFS_VERSION_4()
        {
            string filename = "WRITE_AND_READ_CFS_V4.cfs";

            CompoundFile cf = new CompoundFile(CFSVersion.Ver_4, CFSConfiguration.EraseFreeSectors | CFSConfiguration.SectorRecycle);

            CFStorage st = cf.RootStorage.AddStorage("MyStorage");
            CFStream sm = st.AddStream("MyStream");
            byte[] b = new byte[220];
            sm.SetData(b);

            cf.SaveAs(filename);
            cf.Close();

            CompoundFile cf2 = new CompoundFile(filename);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");

            Assert.IsNotNull(sm2);
            Assert.AreEqual(220, sm2.Size);

            cf2.Close();
        }

        [TestMethod]
        public void Test_WRITE_READ_CFS_VERSION_4_STREAM()
        {
            string filename = "WRITE_COMMIT_READ_CFS_V4.cfs";

            CompoundFile cf = new CompoundFile(CFSVersion.Ver_4, CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors);

            CFStorage st = cf.RootStorage.AddStorage("MyStorage");
            CFStream sm = st.AddStream("MyStream");
            byte[] b = Helpers.GetBuffer(227);
            sm.SetData(b);

            cf.SaveAs(filename);
            cf.Close();

            CompoundFile cf2 = new CompoundFile(filename);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");

            Assert.IsNotNull(sm2);
            Assert.AreEqual(b.Length, sm2.Size);

            cf2.Close();
        }

        [TestMethod]
        public void Test_OPEN_FROM_STREAM()
        {
            const string filename = "reportREAD.xls";

            using (var fs = new FileStream(filename, FileMode.Open))
            {
                using (var cf = new CompoundFile(fs))
                {
                    var foundStream = cf.RootStorage.GetStream("Workbook");
                    var temp = foundStream.GetData();
                    Assert.IsNotNull(temp);
                    cf.Close();
                }
            }
        }

        [TestMethod]
        public void Test_MULTIPLE_SAVE()
        {
            var file = new CompoundFile();

            file.SaveAs("test.mdf");

            var meta = file.
                RootStorage.
                AddStream("meta");

            meta.Append(BitConverter.GetBytes(DateTime.Now.ToBinary()));
            meta.Append(BitConverter.GetBytes(DateTime.Now.ToBinary()));

            file.SaveAs("test.mdf");
        }

        [TestMethod]
        public void Test_OPEN_COMPOUND_BUG_FIX_133()
        {
            var f = new CompoundFile("testbad.ole");
            CFStream cfs = f.RootStorage.GetStream("\x01Ole10Native");
            byte[] data = cfs.GetData();
            Assert.AreEqual(18140, data.Length);
        }

        [TestMethod]
        public void Test_COMPARE_DIR_ENTRY_NAME_BUG_FIX_ID_3487353()
        {
            var f = new CompoundFile("report_name_fix.xls", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors);
            CFStream cfs = f.RootStorage.AddStream("Poorbook");
            cfs.Append(Helpers.GetBuffer(20));
            f.Commit();
            f.Close();

            f = new CompoundFile("report_name_fix.xls", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors);
            cfs = f.RootStorage.GetStream("Workbook");
            Assert.AreEqual("Workbook", cfs.Name);
            f.RootStorage.Delete("PoorBook");
            f.Commit();
            f.Close();
        }

        [TestMethod]
        public void Test_GET_COMPOUND_VERSION()
        {
            var f = new CompoundFile("report_name_fix.xls");
            Assert.AreEqual(CFSVersion.Ver_3, f.Version);
            f.Close();
        }

        [TestMethod]
        public void Test_FUNCTIONAL_BEHAVIOUR()
        {
            //System.Diagnostics.Trace.Listeners.Add(new ConsoleTraceListener());

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
            var cf = new CompoundFile(CFSVersion.Ver_3, CFSConfiguration.SectorRecycle);
            cf.RootStorage.AddStream("A").SetData(bA);
            cf.SaveAs("OneStream.cfs");
            cf.Close();

            // Test Phase 1
            var cfTest = new CompoundFile("OneStream.cfs");
            CFStream testSt = cfTest.RootStorage.GetStream("A");

            Assert.IsNotNull(testSt);
            CollectionAssert.AreEqual(bA, testSt.GetData());

            cfTest.Close();

            //###########

            //Phase 2
            cf = new CompoundFile("OneStream.cfs", CFSUpdateMode.ReadOnly, CFSConfiguration.SectorRecycle);

            cf.RootStorage.AddStream("B").SetData(bB);
            cf.RootStorage.AddStream("C").SetData(bC);
            cf.RootStorage.AddStream("D").SetData(bD);
            cf.RootStorage.AddStream("E").SetData(bE);
            cf.RootStorage.AddStream("F").SetData(bF);
            cf.RootStorage.AddStream("G").SetData(bG);
            cf.RootStorage.AddStream("H").SetData(bH);

            cf.SaveAs("8_Streams.cfs");
            cf.Close();

            // Test Phase 2

            cfTest = new CompoundFile("8_Streams.cfs");

            testSt = cfTest.RootStorage.GetStream("B");
            Assert.IsNotNull(testSt);
            CollectionAssert.AreEqual(bB, testSt.GetData());

            testSt = cfTest.RootStorage.GetStream("C");
            Assert.IsNotNull(testSt);
            CollectionAssert.AreEqual(bC, testSt.GetData());

            testSt = cfTest.RootStorage.GetStream("D");
            Assert.IsNotNull(testSt);
            CollectionAssert.AreEqual(bD, testSt.GetData());

            testSt = cfTest.RootStorage.GetStream("E");
            Assert.IsNotNull(testSt);
            CollectionAssert.AreEqual(bE, testSt.GetData());

            testSt = cfTest.RootStorage.GetStream("F");
            Assert.IsNotNull(testSt);
            CollectionAssert.AreEqual(bF, testSt.GetData());

            testSt = cfTest.RootStorage.GetStream("G");
            Assert.IsNotNull(testSt);
            CollectionAssert.AreEqual(bG, testSt.GetData());

            testSt = cfTest.RootStorage.GetStream("H");
            Assert.IsNotNull(testSt);
            CollectionAssert.AreEqual(bH, testSt.GetData());

            cfTest.Close();

            File.Copy("8_Streams.cfs", "6_Streams.cfs", true);
            File.Delete("8_Streams.cfs");

            //###########
            //
#if !NETCOREAPP2_0
            Trace.Listeners.Add(new ConsoleTraceListener());
#endif
            // Phase 3
            cf = new CompoundFile("6_Streams.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors);
            cf.RootStorage.Delete("D");
            cf.RootStorage.Delete("G");
            cf.Commit();

            cf.Close();

            //Test Phase 3

            cfTest = new CompoundFile("6_Streams.cfs");
            Assert.ThrowsException<CFItemNotFound>(() => cfTest.RootStorage.GetStream("D"));
            Assert.ThrowsException<CFItemNotFound>(() => cfTest.RootStorage.GetStream("G"));

            cfTest.Close();

            //##########

            // Phase 4

            File.Copy("6_Streams.cfs", "6_Streams_Shrinked.cfs", true);
            CompoundFile.ShrinkCompoundFile("6_Streams_Shrinked.cfs");

            // Test Phase 4

            Assert.IsTrue(new FileInfo("6_Streams_Shrinked.cfs").Length < new FileInfo("6_Streams.cfs").Length);

            cfTest = new CompoundFile("6_Streams_Shrinked.cfs");
            Action<CFItem> va = delegate (CFItem item)
            {
                if (item.IsStream)
                {
                    CFStream ia = item as CFStream;
                    Assert.IsNotNull(ia);
                    Assert.IsTrue(ia.Size > 0);
                    byte[] d = ia.GetData();
                    Assert.IsNotNull(d);
                    Assert.IsTrue(d.Length > 0);
                    Assert.AreEqual(ia.Size, d.Length);
                }
            };

            cfTest.RootStorage.VisitEntries(va, true);
            cfTest.Close();

            //##########

            //Phase 5

            cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle);
            cf.RootStorage.AddStream("ZZZ").SetData(bF);
            cf.RootStorage.GetStream("E").Append(bE2);
            cf.Commit();
            cf.Close();

            cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle);
            cf.RootStorage.CLSID = new Guid("EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE");
            cf.Commit();
            cf.Close();

            cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle);
            cf.RootStorage.AddStorage("MyStorage").AddStream("ZIP").Append(bE);
            cf.Commit();
            cf.Close();

            cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle);
            cf.RootStorage.AddStorage("AnotherStorage").AddStream("ANS").Append(bE);
            cf.RootStorage.Delete("MyStorage");

            cf.Commit();
            cf.Close();

            //Test Phase 5

            //#####

            //Phase 6

            cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle);
            CFStorage root = cf.RootStorage;

            root.AddStorage("MiniStorage").AddStream("miniSt").Append(bMini);

            cf.RootStorage.GetStorage("MiniStorage").AddStream("miniSt2").Append(bMini);
            cf.Commit();
            cf.Close();

            cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle);
            cf.RootStorage.GetStorage("MiniStorage").Delete("miniSt");

            cf.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2").Append(bE);

            cf.Commit();
            cf.Close();

            //Test Phase 6

            cfTest = new CompoundFile("6_Streams_Shrinked.cfs");
            byte[] d2 = cfTest.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2").GetData();
            Assert.AreEqual(bE.Length + bMini.Length, d2.Length);

            int cnt = 1;
            byte[] buf = new byte[cnt];
            cnt = cfTest.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2").Read(buf, bMini.Length, cnt);

            Assert.AreEqual(1, cnt);
            Assert.AreEqual(0x1A, buf[0]);

            cnt = 1;
            cnt = cfTest.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2").Read(buf, bMini.Length - 1, cnt);
            Assert.AreEqual(1, cnt);
            Assert.AreEqual(0xEE, buf[0]);

            Assert.ThrowsException<CFItemNotFound>(() => cfTest.RootStorage.GetStorage("MiniStorage").GetStream("miniSt"));

            cfTest.Close();

            //##############

            //Phase 7

            cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle);

            cf.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2").SetData(bA);
            cf.Commit();
            cf.Close();

            //Test Phase 7

            cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update, CFSConfiguration.SectorRecycle);
            d2 = cf.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2").GetData();
            Assert.IsNotNull(d2);
            Assert.AreEqual(bA.Length, d2.Length);

            cf.Close();

            //##############

            cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.ReadOnly, CFSConfiguration.SectorRecycle);

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

            Assert.AreEqual(3, result.Count);
        }

        [TestMethod]
        public void Test_CORRUPTED_CYCLIC_FAT_CHECK()
        {
            Assert.ThrowsException<CFCorruptedFileException>(() =>
            {
                using CompoundFile cf = new("CyclicFAT.cfs");
            });
        }

        [TestMethod]
        public void Test_DIFAT_CHECK()
        {
            CompoundFile f = null;
            try
            {
                f = new CompoundFile();
                CFStream st = f.RootStorage.AddStream("LargeStream");
                st.Append(Helpers.GetBuffer(20000000, 0x0A));       //Forcing creation of two DIFAT sectors
                byte[] b1 = Helpers.GetBuffer(3, 0x0B);
                st.Append(b1);                                      //Forcing creation of two DIFAT sectors

                f.SaveAs("$OpenMcdf$LargeFile.cfs");

                f.Close();

                int cnt = 3;
                f = new CompoundFile("$OpenMcdf$LargeFile.cfs");

                byte[] b2 = new byte[cnt];
                cnt = f.RootStorage.GetStream("LargeStream").Read(b2, 20000000, cnt);
                f.Close();
                CollectionAssert.AreEqual(b1, b2);
            }
            finally
            {
                if (f != null)
                    f.Close();

                if (File.Exists("$OpenMcdf$LargeFile.cfs"))
                    File.Delete("$OpenMcdf$LargeFile.cfs");
            }
        }

        [TestMethod]
        public void Test_ADD_LARGE_NUMBER_OF_ITEMS()
        {
            int ITEM_NUMBER = 10000;
            byte[] buffer = Helpers.GetBuffer(10, 0x0A);
            try
            {
                CompoundFile f = new CompoundFile();

                for (int i = 0; i < ITEM_NUMBER; i++)
                {
                    CFStream st = f.RootStorage.AddStream("Stream" + i.ToString());
                    st.Append(buffer);
                }

                if (File.Exists("$ItemsLargeNumber.cfs"))
                    File.Delete("$ItemsLargeNumber.cfs");

                f.SaveAs("$ItemsLargeNumber.cfs");
                f.Close();

                f = new CompoundFile("$ItemsLargeNumber.cfs");
                CFStream cfs = f.RootStorage.GetStream("Stream" + (ITEM_NUMBER / 2).ToString());

                Assert.IsNotNull(cfs, "Item is null");
                CollectionAssert.AreEqual(buffer, cfs.GetData());
                f.Close();
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
            finally
            {
                //if (File.Exists("$ItemsLargeNumber.cfs"))
                //    File.Delete("$ItemsLargeNumber.cfs");
            }
        }

        [TestMethod]
        public void Test_FIX_BUG_16_CORRUPTED_AFTER_RESIZE()
        {
            const string FILE_PATH = @"BUG_16_.xls";

            CompoundFile cf = new CompoundFile(FILE_PATH);

            CFStream dirStream = cf.RootStorage.GetStorage("_VBA_PROJECT_CUR").GetStorage("VBA").GetStream("dir");

            byte[] currentData = dirStream.GetData();

            Array.Resize(ref currentData, currentData.Length - 50);

            dirStream.SetData(currentData);

            cf.SaveAs(FILE_PATH + ".edited");
            cf.Close();
        }

        [TestMethod]
        public void Test_FIX_BUG_17_CORRUPTED_PPT_FILE()
        {
            const string FILE_PATH = @"2_MB-W.ppt";

            using (CompoundFile file = new CompoundFile(FILE_PATH))
            {
                //CFStorage dataSpaceInfo = file.RootStorage.GetStorage("\u0006DataSpaces").GetStorage("DataSpaceInfo");
                CFItem dsiItem = file.GetAllNamedEntries("DataSpaceInfo").FirstOrDefault();
            }
        }

        [TestMethod]
        public void Test_FIX_BUG_24_CORRUPTED_THUMBS_DB_FILE()
        {
            try
            {
                using (var cf = new CompoundFile("_thumbs_bug_24.db"))
                {
                    cf.RootStorage.VisitEntries(item => Console.WriteLine(item.Name), recursive: false);
                }
            }
            catch (Exception exc)
            {
                Assert.IsInstanceOfType(exc, typeof(CFCorruptedFileException));
            }

            using (var cf = new CompoundFile("report.xls"))
            {
                cf.RootStorage.VisitEntries(item => Console.WriteLine(item.Name), recursive: false);
            }
        }

        [TestMethod]
        public void Test_FIX_BUG_28_CompoundFile_Delete_ChildElementMaintainsFiles()
        {
            using (var compoundFile = new CompoundFile())
            {
                var storage1 = compoundFile.RootStorage.AddStorage("A");
                var storage2 = compoundFile.RootStorage.AddStorage("B");
                var storage3 = compoundFile.RootStorage.AddStorage("C");
                storage1.AddStream("A.1");
                compoundFile.RootStorage.Delete("B");
                storage1 = compoundFile.RootStorage.GetStorage("A");
                storage1.GetStream("A.1");
            }
        }

        [TestMethod]
        public void Test_CORRUPTEDDOC_BUG36_SHOULD_THROW_CORRUPTED_FILE_EXCEPTION()
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream("CorruptedDoc_bug36.doc", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                CompoundFile file = new CompoundFile(fs, CFSUpdateMode.ReadOnly, CFSConfiguration.LeaveOpen);
            }
            catch (Exception)
            {
                Assert.IsTrue(fs.CanRead && fs.CanSeek && fs.CanWrite);
            }
        }

        [TestMethod]
        public void Test_ISSUE_2_WRONG_CUTOFF_SIZE()
        {
            FileStream fs = null;
            try
            {
                if (File.Exists("TEST_ISSUE_2"))
                {
                    File.Delete("TEST_ISSUE_2");
                }

                CompoundFile cf = new CompoundFile(CFSVersion.Ver_3, CFSConfiguration.Default);
                var s = cf.RootStorage.AddStream("miniToNormal");
                s.Append(Helpers.GetBuffer(4090, 0xAA));

                cf.SaveAs("TEST_ISSUE_2");
                cf.Close();
                var cf2 = new CompoundFile("TEST_ISSUE_2", CFSUpdateMode.Update, CFSConfiguration.Default);
                cf2.RootStorage.GetStream("miniToNormal").Append(Helpers.GetBuffer(6, 0xBB));
                cf2.Commit();
                cf2.Close();
            }
            catch (Exception)
            {
                Assert.IsTrue(fs.CanRead && fs.CanSeek && fs.CanWrite);
            }
        }

        [TestMethod]
        public void Test_PR_13()
        {
            CompoundFile cf = new CompoundFile("report.xls");
            Guid g = cf.getGuidBySID(0);
            Assert.IsNotNull(g);
            g = cf.getGuidForStream(3);
            Assert.IsNotNull(g);
            Assert.IsTrue(!string.IsNullOrEmpty(cf.GetNameDirEntry(2)));
            Assert.IsTrue(cf.GetNumDirectories() > 0);
        }

        //[TestMethod]
        //public void Test_CORRUPTED_CYCLIC_DIFAT_VALIDATION_CHECK()
        //{

        //    CompoundFile cf = null;
        //    try
        //    {
        //        cf = new CompoundFile("CiclycDFAT.cfs");
        //        CFStorage s = cf.RootStorage.GetStorage("MyStorage");
        //        CFStream st = s.GetStream("MyStream");
        //        Assert.IsTrue(st.Size > 0);
        //    }
        //    catch (Exception ex)
        //    {
        //        Assert.IsTrue(ex is CFCorruptedFileException);
        //    }
        //    finally
        //    {
        //        if (cf != null)
        //        {
        //            cf.Close();
        //        }
        //    }
        //}
        //[TestMethod]
        //public void Test_REM()
        //{
        //    var f = new CompoundFile();

        //    byte[] bB = Helpers.GetBuffer(5 * 1024, 0x0B);
        //    f.RootStorage.AddStream("Test").AppendData(bB);
        //    f.Save("Astorage.cfs");
        //}

        //}

        [TestMethod]
        public void Test_COPY_ENTRIES_FROM_TO_STORAGE()
        {
            CompoundFile cfDst = new CompoundFile();
            CompoundFile cfSrc = new CompoundFile("MultipleStorage4.cfs");

            Copy(cfSrc.RootStorage, cfDst.RootStorage);

            cfDst.SaveAs("MultipleStorage4Copy.cfs");

            cfDst.Close();
            cfSrc.Close();
        }

        #region Copy helper method
        /// <summary>
        /// Copies the given <paramref name="source"/> to the given <paramref name="destination"/>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static void Copy(CFStorage source, CFStorage destination)
        {
            source.VisitEntries(action =>
            {
                if (action.IsStorage)
                {
                    var destionationStorage = destination.AddStorage(action.Name);
                    destionationStorage.CLSID = action.CLSID;
                    destionationStorage.CreationDate = action.CreationDate;
                    destionationStorage.ModifyDate = action.ModifyDate;
                    Copy(action as CFStorage, destionationStorage);
                }
                else
                {
                    var sourceStream = action as CFStream;
                    var destinationStream = destination.AddStream(action.Name);
                    if (sourceStream != null) destinationStream.SetData(sourceStream.GetData());
                }
            }, false);
        }
        #endregion
        private const int Mb = 1024 * 1024;
        [TestMethod]
        public void Test_FIX_BUG_GH_14()
        {
            string filename = "MyFile.dat";
            string storageName = "MyStorage";
            string streamName = "MyStream";
            int BUFFER_SIZE = 800 * Mb;
            int iterationCount = 1;
            int streamCount = 3;

            CompoundFile compoundFileInit = new CompoundFile(CFSVersion.Ver_4, CFSConfiguration.Default);
            compoundFileInit.SaveAs(filename);
            compoundFileInit.Close();

            CompoundFile compoundFile = new CompoundFile(filename, CFSUpdateMode.Update, CFSConfiguration.Default);
            CFStorage st = compoundFile.RootStorage.AddStorage(storageName);
            byte b = 0X0A;

            for (int streamId = 0; streamId < streamCount; ++streamId)
            {
                CFStream sm = st.AddStream(streamName + streamId);
                for (int iteration = 0; iteration < iterationCount; ++iteration)
                {
                    sm.Append(Helpers.GetBuffer(BUFFER_SIZE, b));
                    compoundFile.Commit();
                }

                b++;
            }

            compoundFile.Close();

            compoundFile = new CompoundFile(filename, CFSUpdateMode.ReadOnly, CFSConfiguration.Default);
            byte[] testBuffer = new byte[100];
            byte t = 0x0A;

            for (int streamId = 0; streamId < streamCount; ++streamId)
            {
                compoundFile.RootStorage.GetStorage(storageName).GetStream(streamName + streamId).Read(testBuffer, BUFFER_SIZE / 2, 100);
                Assert.IsTrue(testBuffer.All(g => g == t));
                compoundFile.RootStorage.GetStorage(storageName).GetStream(streamName + streamId).Read(testBuffer, BUFFER_SIZE - 101, 100);
                Assert.IsTrue(testBuffer.All(g => g == t));
                compoundFile.RootStorage.GetStorage(storageName).GetStream(streamName + streamId).Read(testBuffer, 0, 100);
                Assert.IsTrue(testBuffer.All(g => g == t));
                t++;
            }

            compoundFile.Close();
        }

        [TestMethod]
        public void Test_FIX_BUG_GH_15()
        {
            string filename = "MyFile.dat";
            string storageName = "MyStorage";
            string streamName = "MyStream";
            int BUFFER_SIZE = 800 * Mb;
            int iterationCount = 1;
            int streamCount = 1;

            CompoundFile compoundFile = new CompoundFile(CFSVersion.Ver_4, CFSConfiguration.Default);
            CFStorage st = compoundFile.RootStorage.AddStorage(storageName);

            for (int streamId = 0; streamId < streamCount; ++streamId)
            {
                CFStream sm = st.AddStream(streamName + streamId);
                for (int iteration = 0; iteration < iterationCount; ++iteration)
                {
                    byte b = (byte)(0x0A + iteration);
                    sm.Append(Helpers.GetBuffer(BUFFER_SIZE, b));
                }
            }

            compoundFile.SaveAs(filename);
            compoundFile.Close();

            byte[] readBuffer = new byte[15];
            compoundFile = new CompoundFile(filename);

            byte c = 0x0A;
            for (int i = 0; i < iterationCount; i++)
            {
                compoundFile.RootStorage.GetStorage(storageName).GetStream(streamName + 0.ToString()).Read(readBuffer, BUFFER_SIZE + ((long)BUFFER_SIZE * i) - 15, 15);
                Assert.IsTrue(readBuffer.All(by => by == c));
                c++;
            }

            compoundFile.Close();
        }

        [TestMethod]
        public void Test_PR_GH_18()
        {
            try
            {
                var f = new CompoundFile("MultipleStorage4.cfs", CFSUpdateMode.Update, CFSConfiguration.Default);
                var st = f.RootStorage.GetStorage("MyStorage").GetStorage("AnotherStorage").GetStream("MyStream");
                st.Write(Helpers.GetBuffer(100, 0x02), 100);
                f.Commit(true);
                Assert.AreEqual(31220, st.GetData().Length);
                f.Close();
            }
            catch (Exception)
            {
                Assert.Fail("Release Memory flag caused error");
            }
        }

        [TestMethod]
        public void Test_FIX_GH_38()
        {
            CompoundFile f = null;
            try
            {
                f = new CompoundFile("empty_directory_chain.doc", CFSUpdateMode.Update, CFSConfiguration.Default);

                f.Close();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(CFCorruptedFileException));
                if (f != null)
                    f.Close();
            }
        }

        [TestMethod]
        public void Test_FIX_GH_38_B()
        {
            CompoundFile f = null;
            try
            {
                f = new CompoundFile("no_sectors.doc", CFSUpdateMode.Update, CFSConfiguration.Default);

                f.Close();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(CFException));
                if (f != null)
                    f.Close();
            }
        }

        [TestMethod]
        public void Test_FIX_GH_50()
        {
            Assert.ThrowsException<CFFileFormatException>(() =>
            {
                using CompoundFile f = new("64-67.numberOfMiniFATSectors.docx", CFSUpdateMode.Update, CFSConfiguration.Default);
            });
        }

        [TestMethod]
        public void Test_FIX_GH_83()
        {
            try
            {
                byte[] bigDataBuffer = Helpers.GetBuffer(1024 * 1024 * 260);

                using (FileStream fTest = new FileStream("BigFile.data", FileMode.Create))
                {
                    fTest.Write(bigDataBuffer, 0, 1024 * 1024 * 260);
                    fTest.Flush();
                    fTest.Close();

                    var f = new CompoundFile();
                    var cfStream = f.RootStorage.AddStream("NewStream");
                    using (FileStream fs = new FileStream("BigFile.data", FileMode.Open))
                    {
                        cfStream.CopyFrom(fs);
                    }

                    f.SaveAs("BigFile.cfs");
                    f.Close();
                }
            }
            catch (Exception)
            {
                Assert.Fail();
            }
            finally
            {
                if (File.Exists("BigFile.data"))
                    File.Delete("BigFile.data");

                if (File.Exists("BigFile.cfs"))
                    File.Delete("BigFile.cfs");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(CFCorruptedFileException))]
        public void Test_CorruptedSectorChain_Doc()
        {
            var f = new CompoundFile("corrupted-sector-chain.doc");

            f.Close();
        }

        [TestMethod]
        [ExpectedException(typeof(CFCorruptedFileException))]
        public void Test_CorruptedSectorChain_Cfs()
        {
            var f = new CompoundFile("corrupted-sector-chain.cfs");

            f.Close();
        }

        [TestMethod]
        public void Test_WRONG_CORRUPTED_EXCEPTION()
        {
            var cf = new CompoundFile();

            for (int i = 0; i < 100; i++)
            {
                cf.RootStorage.AddStream("Stream" + i).SetData(Helpers.GetBuffer(100000, 0xAA));
            }

            cf.RootStorage.AddStream("BigStream").SetData(Helpers.GetBuffer(5250000, 0xAA));

            using (var stream = new MemoryStream())
            {
                cf.Save(stream);
            }

            cf.Close();
        }

        [TestMethod]
        [ExpectedException(typeof(CFCorruptedFileException))]
        public void Test_CorruptedSectorChain_Doc2()
        {
            var f = new CompoundFile("corrupted-sector-chain-2.doc");

            f.Close();
        }

        //[TestMethod]
        //public void Test_CORRUPTED_CYCLIC_DIFAT_VALIDATION_CHECK()
        //{

        //    CompoundFile cf = null;
        //    try
        //    {
        //        cf = new CompoundFile("CiclycDFAT.cfs");
        //        CFStorage s = cf.RootStorage.GetStorage("MyStorage");
        //        CFStream st = s.GetStream("MyStream");
        //        Assert.IsTrue(st.Size > 0);
        //    }
        //    catch (Exception ex)
        //    {
        //        Assert.IsTrue(ex is CFCorruptedFileException);
        //    }
        //    finally
        //    {
        //        if (cf != null)
        //        {
        //            cf.Close();
        //        }
        //    }
        //}
        //[TestMethod]
        //public void Test_REM()
        //{
        //    var f = new CompoundFile();

        //    byte[] bB = Helpers.GetBuffer(5 * 1024, 0x0B);
        //    f.RootStorage.AddStream("Test").AppendData(bB);
        //    f.Save("Astorage.cfs");
        //}

        [TestMethod]
        public void Test_FIX_BUG_90_CompoundFile_Delete_Storages()
        {
            using (var compoundFile = new CompoundFile())
            {
                var root = compoundFile.RootStorage;
                var storageNames = new HashSet<string>();

                // add 99 storages to root
                for (int i = 1; i <= 99; i++)
                {
                    var name = "Storage " + i;
                    root.AddStorage(name);
                    storageNames.Add(name);
                }

                // remove storages until tree becomes unbalanced and its Root changes
                var rootChild = root.DirEntry.Child;
                var newChild = rootChild;
                var j = 1;
                while (newChild == rootChild && j <= 99)
                {
                    var name = "Storage " + j;
                    root.Delete(name);
                    storageNames.Remove(name);
                    newChild = ((DirectoryEntry)root.Children.Root).SID; // stop as soon as root.Children has a new Root
                    j++;
                }

                // check if all remaining storages are still there
                foreach (var storageName in storageNames)
                {
                    Assert.IsTrue(root.TryGetStorage(storageName, out var storage)); // <- no problem here
                }

                // write CompundFile into MemoryStream...
                using (var memStream = new MemoryStream())
                {
                    compoundFile.Save(memStream);

                    // ....and read new CompundFile from that stream
                    using (var newCf = new CompoundFile(memStream))
                    {
                        // check if all storages can be found in to copied CompundFile
                        foreach (var storageName in storageNames)
                        {
                            Assert.IsTrue(newCf.RootStorage.TryGetStorage(storageName, out var storage)); //<- we will see some missing storages here
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void Test_FIX_BUG_75_ForeverLoop()
        {
            try
            {
                var cf = new CompoundFile("mediationform.doc", CFSUpdateMode.ReadOnly, CFSConfiguration.Default & ~CFSConfiguration.NoValidationException);
                var s = cf.RootStorage.GetStream("\u0001CompObj");
                byte[] data = s.GetData();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(CFCorruptedFileException));
            }
        }

        [TestMethod]
        public void Test_FIX_BUG_94_GrowingSizeSave()
        {
            string filename = "_Test.ppt";
            string filename2 = "MyFile4.dat";

            if (File.Exists(filename2))
                File.Delete(filename2);

            if (File.Exists(filename))
            {
                File.Copy(filename, filename2);
            }

            var cf = new CompoundFile(filename2, CFSUpdateMode.Update, CFSConfiguration.EraseFreeSectors);
            cf.RootStorage.Delete("PowerPoint Document");
            cf.Commit();
            cf.Close();

            CompoundFile.ShrinkCompoundFile(filename2);

            long length = new FileInfo(filename).Length;
            long length2 = new FileInfo(filename2).Length;

            Assert.IsTrue(length > length2);

            if (File.Exists(filename2))
                File.Delete(filename2);
        }

        [TestMethod]
        public void Test_FIX_BUG_96_CompoundFile_SaveOverwrite()
        {
            string filename = "MultipleStorage.cfs";
            string filename2 = "MyFile2.dat";
            string storageName = "MyStorage";
            string streamName = "MyStream";

            if (File.Exists(filename2))
                File.Delete(filename2);

            if (File.Exists(filename))
            {
                File.Copy(filename, filename2);
            }

            Assert.ThrowsException<CFException>(() =>
            {
                using CompoundFile compoundFile = new(filename2);
                var s = compoundFile.RootStorage.GetStorage(storageName).GetStream(streamName);
                s.Write(new byte[] { 0x0A, 0x0A }, 0);
                compoundFile.SaveAs(filename2);
                compoundFile.Close();
            });

            Assert.ThrowsException<CFException>(() =>
            {
                string rootedPath = Path.GetFullPath(filename2);
                using CompoundFile compoundFile = new(rootedPath);
                var s = compoundFile.RootStorage.GetStorage(storageName).GetStream(streamName);
                s.Write(new byte[] { 0x0A, 0x0A }, 0);
                compoundFile.SaveAs(rootedPath);
                compoundFile.Close();
            });

            Assert.ThrowsException<CFException>(() =>
            {
                using CompoundFile compoundFile = new CompoundFile(filename2);
                using FileStream fs = new(filename2, FileMode.Open);
                var s = compoundFile.RootStorage.GetStorage(storageName).GetStream(streamName);
                s.Write(new byte[] { 0x0A, 0x0A }, 0);
                compoundFile.Save(fs);
                compoundFile.Close();
            });


            if (File.Exists(filename2))
                File.Delete(filename2);
        }
    }
}
