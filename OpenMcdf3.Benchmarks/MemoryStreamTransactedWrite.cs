using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using OpenMcdf3.Benchmarks;

namespace OpenMcdf3.Benchmark;

[ShortRunJob]
[MemoryDiagnoser]
[HideColumns(Column.AllocRatio)]
[MarkdownExporter]
public class MemoryStreamTransactedWrite : IDisposable
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
    public void WriteTransacted() => OpenMcdfBenchmarks.WriteStream(writeStream!, version, StorageModeFlags.LeaveOpen | StorageModeFlags.Transacted, buffer, StreamLength);
}
