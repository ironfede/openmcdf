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
    private MemoryStream? writeStream;
    private byte[] buffer = Array.Empty<byte>();

    [Params(Version.V3, Version.V4)]
    public Version Version { get; set; }

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
    public void Write() => OpenMcdfBenchmarks.WriteStream(writeStream!, Version, StorageModeFlags.LeaveOpen, buffer, StreamLength);

#if WINDOWS
    [Benchmark(Baseline = true)]
    public void WriteStructuredStorage() => StructuredStorageBenchmarks.WriteInMemory(Version, StorageModeFlags.LeaveOpen, buffer, StreamLength);
#endif
}
