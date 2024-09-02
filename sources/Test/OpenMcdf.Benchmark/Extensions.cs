using System.IO;
using BenchmarkDotNet.Attributes;
using OpenMcdf.Extensions;

namespace OpenMcdf.Benchmark
{
    // Simple benchmarks for reading OLE Properties with OpenMcdf.Extensions.
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.NetCoreApp31)]
    [MemoryDiagnoser]
    public class ExtensionsRead
    {
        // Keep the Storage as a member, as we're only testing the OLEProperties bits, not the underlying CompoundFile
        private readonly CompoundFile udTestFile;

        // Load the test file on creation
        public ExtensionsRead()
        {
            string testFile = Path.Combine("TestFiles", "winUnicodeDictionary.doc");
            this.udTestFile = new CompoundFile(testFile);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            udTestFile.Close();
        }

        [Benchmark]
        public void TestReadSummaryInformation()
        {
            this.udTestFile.RootStorage.GetStream("\u0005SummaryInformation").AsOLEPropertiesContainer();
        }

        [Benchmark]
        public void TestReadDocumentSummaryInformation()
        {
            this.udTestFile.RootStorage.GetStream("\u0005DocumentSummaryInformation").AsOLEPropertiesContainer();
        }
    }
}