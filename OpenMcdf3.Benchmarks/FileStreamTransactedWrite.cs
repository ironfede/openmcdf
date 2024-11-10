using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using OpenMcdf3.Benchmarks;

namespace OpenMcdf3.Benchmark;

[MediumRunJob]
[MemoryDiagnoser]
[HideColumns(Column.AllocRatio)]
[MarkdownExporter]
public class FileStreamTransactedWrite : IDisposable
{
    const Version version = Version.V3;

    private string writeFileName = "";

    private byte[] buffer = Array.Empty<byte>();

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
    public void WriteTransacted() => OpenMcdfBenchmarks.WriteStream(writeFileName!, version, StorageModeFlags.None | StorageModeFlags.Transacted, buffer, StreamLength);

#if WINDOWS

    [Benchmark(Baseline = true)]
    public void WriteStructuredStorageTransacted() => StructuredStorageBenchmarks.WriteStream(writeFileName, version, StorageModeFlags.Transacted, buffer, StreamLength);
#endif
}
