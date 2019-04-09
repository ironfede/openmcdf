using BenchmarkDotNet.Running;

namespace OpenMcdf.Benchmark
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<InMemory>();
        }
    }
}