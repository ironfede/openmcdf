using System;
using System.Collections.Generic;
using System.Text;
using OleCompoundFileStorage;
using System.IO;

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
            StressMemory();

            //Console.WriteLine("CLOSED");
            //Console.ReadKey();
        }

        private static void StressMemory()
        {
            byte[] b = GetBuffer(1024 * 1024 * 5); //2GB buffer
            byte[] cmp = new byte[] { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7 };

            CompoundFile cf = new CompoundFile(CFSVersion.Ver_4, false, false);
            CFStream st = cf.RootStorage.AddStream("MySuperLargeStream");
            cf.Save("MEGALARGESSIMUSFILE.cfs");
            cf.Close();

            //Console.WriteLine("Closed save");
            //Console.ReadKey();

            cf = new CompoundFile("MEGALARGESSIMUSFILE.cfs", UpdateMode.Transacted, false, false);
            CFStream cfst = cf.RootStorage.GetStream("MySuperLargeStream");
            for (int i = 0; i < 100; i++)
            {
                cfst.AppendData(b);
                if ((i % 20) == 0) cf.UpdateFile(true);
                //Console.WriteLine("     Updated " + i.ToString());
                //Console.ReadKey();
            }

            cfst.AppendData(cmp);
            cf.UpdateFile(true);

            //Console.WriteLine("Before Closed Transacted");
            //Console.ReadKey();

            cf.Close();

            //Console.WriteLine("Closed Transacted");
            //Console.ReadKey();

            cf = new CompoundFile("MEGALARGESSIMUSFILE.cfs");
            int count = 8;
            byte[] data = cf.RootStorage.GetStream("MySuperLargeStream").GetData(b.Length * 10, ref count);

            cf.Close();

            Console.WriteLine("Closed Final");
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

            CompoundFile cf = new CompoundFile(dstFilename, UpdateMode.Transacted, true, false);

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
                    cf.UpdateFile();
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
