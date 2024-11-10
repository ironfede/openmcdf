using BenchmarkDotNet.Running;

namespace OpenMcdf3.Benchmarks;

internal sealed class Program
{
    private static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run();
    }
}
