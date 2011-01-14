using System;
using System.Collections.Generic;
using System.Text;
using OleCompoundFileStorage;
using System.IO;
using System.Diagnostics;
using System.Threading;

//This project is used for profiling memory and performances of OpenMCDF .

namespace OleCfsMemoryTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ////for (int i = 0; i < 1000; i++)
            ////{
            //CompoundFile cf = new CompoundFile(@"C:\Documents and Settings\Federico\Desktop\openmcdf\OleCfsMemoryTest\testfile\INCREMENTAL_SIZE_MULTIPLE_WRITE_AND_READ_CFS.cfs");
            //AddNodes("", cf.RootStorage);


            ////}
            //Console.WriteLine("TRAVERSED");
            //Console.ReadKey();

            //cf.Save(@"C:\OUIOIUIO");
            //Console.WriteLine("SAVED");
            //Console.ReadKey();

            //cf.Close();

            //TestMultipleStreamCommit();
            TestCode();
            //StressMemory();
            //DummyFile();
            //Console.WriteLine("CLOSED");
            //Console.ReadKey();
        }

        private static void TestCode2()
        {
            byte[] bA = GetBuffer(5000, 0x0A);
            byte[] bB = GetBuffer(25000, 0x0B);

            var cf = new CompoundFile(CFSVersion.Ver_3, true, false);
            var myStream = cf.RootStorage.AddStream("A");
            myStream.SetData(bA);

            myStream = cf.RootStorage.AddStream("B");
            myStream.SetData(bB);

            cf.Save("a.cfs");
            cf.Close();

            cf = new CompoundFile("a.cfs", UpdateMode.Update, true, true);
            cf.RootStorage.Delete("B");
            cf.CompressFreeSpace();
            cf.Save("b.cfs");
        }

        private static void TestCode()
        {
            byte[] bA = GetBuffer(5000, 0x0A);
            byte[] bB = GetBuffer(5000, 0x0B);
            byte[] bC = GetBuffer(6000, 0x0C);
            byte[] bD = GetBuffer(4500, 0x0D);

            var cf = new CompoundFile(CFSVersion.Ver_3, true, false);
            var myStream = cf.RootStorage.AddStream("A");

            myStream.SetData(bA);
            cf.Save("a.cfs");
            cf.Close();

            cf = new CompoundFile("a.cfs");
            cf.Save("b.cfs");
            cf.Close();

            cf = new CompoundFile("b.cfs", UpdateMode.Update, true, false);
            myStream = cf.RootStorage.AddStream("B");
            myStream.SetData(bB);
            myStream = cf.RootStorage.AddStream("C");
            myStream.SetData(bC);
            myStream = cf.RootStorage.AddStream("D");
            myStream.SetData(bD);
            cf.Commit();

            cf.RootStorage.Delete("A");
            cf.Commit();

            cf.Save("c.cfs");
            cf.Save("d.cfs");

            cf.Close();
           
            cf = new CompoundFile("d.cfs", UpdateMode.Update, true, false);
            cf.CompressFreeSpace();
            cf.Close();

            cf = new CompoundFile("c.cfs", UpdateMode.Update, true, false);
            myStream = cf.RootStorage.GetStream("C");
            var data = myStream.GetData();
            Console.WriteLine(data[0] + " : " + data[data.Length - 1]);

            myStream = cf.RootStorage.GetStream("B");
            data = myStream.GetData();
            Console.WriteLine(data[0] + " : " + data[data.Length - 1]);

            Console.ReadKey();
        }

        private static void StressMemory()
        {
            const int N_LOOP = 20;
            const int MB_SIZE = 10;

            byte[] b = GetBuffer(1024 * 1024 * MB_SIZE); //2GB buffer
            byte[] cmp = new byte[] { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7 };

            CompoundFile cf = new CompoundFile(CFSVersion.Ver_4, false, false);
            CFStream st = cf.RootStorage.AddStream("MySuperLargeStream");
            cf.Save("MEGALARGESSIMUSFILE.cfs");
            cf.Close();

            //Console.WriteLine("Closed save");
            //Console.ReadKey();

            cf = new CompoundFile("MEGALARGESSIMUSFILE.cfs", UpdateMode.Update, false, false);
            CFStream cfst = cf.RootStorage.GetStream("MySuperLargeStream");

            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < N_LOOP; i++)
            {

                cfst.AppendData(b);
                cf.Commit(true);

                Console.WriteLine("     Updated " + i.ToString());
                //Console.ReadKey();
            }

            cfst.AppendData(cmp);
            cf.Commit(true);
            sw.Stop();


            cf.Close();

            Console.WriteLine(sw.Elapsed.TotalMilliseconds);
            sw.Reset();

            //Console.WriteLine(sw.Elapsed.TotalMilliseconds);

            //Console.WriteLine("Closed Transacted");
            //Console.ReadKey();

            cf = new CompoundFile("MEGALARGESSIMUSFILE.cfs");
            int count = 8;
            sw.Reset();
            sw.Start();
            byte[] data = cf.RootStorage.GetStream("MySuperLargeStream").GetData(b.Length * (long)N_LOOP, ref count);
            sw.Stop();
            Console.Write(data.Length);
            cf.Close();

            Console.WriteLine("Closed Final " + sw.ElapsedMilliseconds);
            Console.ReadKey();

        }

        private static void DummyFile()
        {
            Console.WriteLine("Start");
            FileStream fs = new FileStream("myDummyFile", FileMode.Create);
            fs.Close();

            Stopwatch sw = new Stopwatch();

            byte[] b = GetBuffer(1024 * 1024 * 50); //2GB buffer

            fs = new FileStream("myDummyFile", FileMode.Open);
            sw.Start();
            for (int i = 0; i < 42; i++)
            {

                fs.Seek(b.Length * i, SeekOrigin.Begin);
                fs.Write(b, 0, b.Length);

            }

            fs.Close();
            sw.Stop();
            Console.WriteLine("Stop - " + sw.ElapsedMilliseconds);
            sw.Reset();

            Console.ReadKey();
        }

        private static void AddNodes(String depth, CFStorage cfs)
        {

            VisitedEntryAction va = delegate(CFItem target)
            {

                String temp = target.Name + (target is CFStorage ? "" : " (" + target.Size + " bytes )");

                //Stream

                Console.WriteLine(depth + temp);

                if (target is CFStorage)
                {  //Storage

                    String newDepth = depth + "    ";

                    //Recursion into the storage
                    AddNodes(newDepth, (CFStorage)target);

                }
            };

            //Visit NON-recursively (first level only)
            cfs.VisitEntries(va, false);
        }

        public static void TestMultipleStreamCommit()
        {
            String srcFilename = Directory.GetCurrentDirectory() + @"\testfile\report.xls";
            String dstFilename = Directory.GetCurrentDirectory() + @"\testfile\reportOverwriteMultiple.xls";
            //Console.WriteLine(Directory.GetCurrentDirectory());
            //Console.ReadKey(); 
            File.Copy(srcFilename, dstFilename, true);

            CompoundFile cf = new CompoundFile(dstFilename, UpdateMode.Update, true, false);

            Random r = new Random();

            DateTime start = DateTime.Now;

            for (int i = 0; i < 1000; i++)
            {
                byte[] buffer = GetBuffer(r.Next(100, 3500), 0x0A);

                if (i > 0)
                {
                    if (r.Next(0, 100) > 50)
                    {
                        cf.RootStorage.Delete("MyNewStream" + (i - 1).ToString());
                    }
                }

                CFStream addedStream = cf.RootStorage.AddStream("MyNewStream" + i.ToString());

                addedStream.SetData(buffer);

                // Random commit, not on single addition
                if (r.Next(0, 100) > 50)
                    cf.Commit();
            }

            cf.Close();

            TimeSpan sp = (DateTime.Now - start);
            Console.WriteLine(sp.TotalMilliseconds);

        }

        private static byte[] GetBuffer(int count)
        {
            Random r = new Random();
            byte[] b = new byte[count];
            r.NextBytes(b);
            return b;
        }

        private static byte[] GetBuffer(int count, byte c)
        {
            byte[] b = new byte[count];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = c;
            }

            return b;
        }

        private static bool CompareBuffer(byte[] b, byte[] p)
        {
            if (b == null && p == null)
                throw new Exception("Null buffers");

            if (b == null && p != null) return false;
            if (b != null && p == null) return false;

            if (b.Length != p.Length)
                return false;

            for (int i = 0; i < b.Length; i++)
            {
                if (b[i] != p[i])
                    return false;
            }

            return true;
        }
    }
}
