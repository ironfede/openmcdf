using System;
using System.Collections.Generic;
using System.Text;
using OpenMcdf;
using System.IO;
using System.Diagnostics;
using System.Threading;

//This project is used for profiling memory and performances of OpenMCDF .

namespace OpenMcdfMemTest
{
    class Program
    {
        static void Main(string[] args)
        {

            //TestMultipleStreamCommit();
            TestCode();
            //StressMemory();
            //DummyFile();
            //Console.WriteLine("CLOSED");
            //Console.ReadKey();
        }

        private static void TestCode()
        {
            const int N_FACTOR = 1;

            byte[] bA = GetBuffer(20 * 1024 * N_FACTOR, 0x0A);
            byte[] bB = GetBuffer(5 * 1024, 0x0B);
            byte[] bC = GetBuffer(5 * 1024, 0x0C);
            byte[] bD = GetBuffer(5 * 1024, 0x0D);
            byte[] bE = GetBuffer(8 * 1024 * N_FACTOR + 1, 0x1A);
            byte[] bF = GetBuffer(16 * 1024 * N_FACTOR, 0x1B);
            byte[] bG = GetBuffer(14 * 1024 * N_FACTOR, 0x1C);
            byte[] bH = GetBuffer(12 * 1024 * N_FACTOR, 0x1D);
            byte[] bE2 = GetBuffer(8 * 1024 * N_FACTOR, 0x2A);
            byte[] bMini = GetBuffer(1027, 0xEE);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var cf = new CompoundFile(CFSVersion.Ver_3, true, false);
            cf.RootStorage.AddStream("A").SetData(bA);
            cf.Save("OneStream.cfs");

            cf.Close();

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

            File.Copy("8_Streams.cfs", "6_Streams.cfs", true);

            cf = new CompoundFile("6_Streams.cfs", UpdateMode.Update, true, true);
            cf.RootStorage.Delete("D");
            cf.RootStorage.Delete("G");
            cf.Commit();

            cf.Close();

            File.Copy("6_Streams.cfs", "6_Streams_Shrinked.cfs", true);

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

            CompoundFile.ShrinkCompoundFile("6_Streams_Shrinked.cfs");

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
            cf.Save("LARGE.cfs");
            cf.Close();

            //Console.WriteLine("Closed save");
            //Console.ReadKey();

            cf = new CompoundFile("LARGE.cfs", UpdateMode.Update, false, false);
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

            cf = new CompoundFile("LARGE.cfs");
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
