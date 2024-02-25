using BenchmarkDotNet.Running;

namespace OpenMcdf.Benchmark
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}