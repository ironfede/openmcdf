using System;
using System.Collections.Generic;
using System.Text;
using OleCompoundFileStorage;

namespace OleCfsMemoryTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //for (int i = 0; i < 1000; i++)
            //{
            CompoundFile cf = new CompoundFile(@"C:\Documents and Settings\Federico\Desktop\openmcdf\OleCfsMemoryTest\testfile\INCREMENTAL_SIZE_MULTIPLE_WRITE_AND_READ_CFS.cfs");
            AddNodes("", cf.RootStorage);


            //}
            Console.WriteLine("TRAVERSED");
            Console.ReadKey();

            cf.Save(@"C:\OUIOIUIO");
            Console.WriteLine("SAVED");
            Console.ReadKey();

            cf.Close();
            Console.WriteLine("CLOSED");
            Console.ReadKey();
        }


        private static void AddNodes(String depth, CFStorage cfs)
        {

            VisitedEntryAction va = delegate(CFItem target)
            {
                target.Name = "";
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
    }
}
