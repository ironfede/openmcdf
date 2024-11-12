using BenchmarkDotNet.Running;

namespace OpenMcdf.Benchmarks;

internal sealed class Program
{
    private static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run();
    }
}
