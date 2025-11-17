using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using OpenMcdf.Benchmarks;

namespace OpenMcdf.Benchmark;

[MediumRunJob]
[MemoryDiagnoser]
[HideColumns(Column.AllocRatio)]
[MarkdownExporter]
public class FileStreamWrite : IDisposable
{
    private string writeFileName = "";
    private byte[] buffer = Array.Empty<byte>();

    [Params(Version.V3, Version.V4)]
    public Version Version { get; set; }

    [Params(512, 1024 * 1024)]
    public int BufferSize { get; set; }

    [Params(1024 * 1024)]
    public int StreamLength { get; set; }

    public void Dispose()
    {
        File.Delete(writeFileName);
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        writeFileName = Path.GetTempFileName();

        buffer = new byte[BufferSize];
    }

    [GlobalCleanup]
    public void GlobalCleanup() => Dispose();

    [Benchmark]
    public void Write() => OpenMcdfBenchmarks.WriteStream(writeFileName, Version, StorageModeFlags.None, buffer, StreamLength);

#if WINDOWS
    [Benchmark(Baseline = true)]
    public void WriteStructuredStorage() => StructuredStorageBenchmarks.WriteStream(writeFileName, Version, StorageModeFlags.None, buffer, StreamLength);
#endif
}
