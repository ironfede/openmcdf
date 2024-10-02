using System;
using System.Diagnostics;
using System.IO;

namespace OpenMcdf.PerfTest
{
    class Program
    {
        const int MAX_STREAM_COUNT = 5000;
        const string fileName = "PerfLoad.cfs";

        static void Main(string[] args)
        {
            File.Delete(fileName);
            if (!File.Exists(fileName))
            {
                CreateFile(fileName);
            }

            CompoundFile cf = new CompoundFile(fileName);
            var stopwatch = Stopwatch.StartNew();
            CFStream s = cf.RootStorage.GetStream("Test1");
            Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");
            Console.Read();
        }

        private static void CreateFile(string fn)
        {
            CompoundFile cf = new CompoundFile();
            for (int i = 0; i < MAX_STREAM_COUNT; i++)
            {
                cf.RootStorage.AddStream("Test" + i.ToString()).SetData(Helpers.GetBuffer(300));
            }

            cf.SaveAs(fileName);
            cf.Close();
        }
    }
}
