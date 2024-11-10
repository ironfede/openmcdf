using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using OpenMcdf3.Benchmarks;

namespace OpenMcdf3.Benchmark;

[ShortRunJob]
[MemoryDiagnoser]
[HideColumns(Column.AllocRatio)]
[MarkdownExporter]
public class MemoryStreamWrite : IDisposable
{
    const Version version = Version.V3;

    private MemoryStream? writeStream;

    private byte[] buffer = Array.Empty<byte>();

    [Params(512, 1024 * 1024)]
    public int BufferSize { get; set; }

    [Params(1024 * 1024)]
    public int StreamLength { get; set; }

    public void Dispose()
    {
        writeStream?.Dispose();
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        buffer = new byte[BufferSize];
        writeStream = new MemoryStream(2 * StreamLength);
    }

    [GlobalCleanup]
    public void GlobalCleanup() => Dispose();

    [Benchmark]
    public void Write() => OpenMcdfBenchmarks.WriteStream(writeStream!, version, StorageModeFlags.LeaveOpen, buffer, StreamLength);

#if WINDOWS
    [Benchmark(Baseline = true)]
    public void WriteStructuredStorage() => StructuredStorageBenchmarks.WriteInMemory(version, StorageModeFlags.LeaveOpen, buffer, StreamLength);
#endif
}
