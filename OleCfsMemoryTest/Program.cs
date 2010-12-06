using System;
using System.Collections.Generic;
using System.Text;
using OLECompoundFileStorage;

namespace OleCfsMemoryTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //for (int i = 0; i < 1000; i++)
            //{
                CompoundFile cf = new CompoundFile(@"C:\Documents and Settings\blaseotf\My Documents\Visual Studio 2008\Projects\openmcdf\TestResults\blaseotf_WS0030 2010-10-25 13_16_56\WRITE_MINISTREAM_READ_REWRITE_STREAM_2ND.cfs");
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
                String temp = target.Name + (target is CFStorage ? "" : " (" + target.Size + " bytes )");

                //Stream

                Console.WriteLine(temp);

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
