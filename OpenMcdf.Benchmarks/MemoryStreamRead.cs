using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using OpenMcdf.Benchmarks;

namespace OpenMcdf.Benchmark;

[ShortRunJob]
[MemoryDiagnoser]
[HideColumns(Column.AllocRatio)]
[MarkdownExporter]
public class MemoryStreamRead : IDisposable
{
    private MemoryStream? readStream;
    private byte[] buffer = Array.Empty<byte>();

    [Params(Version.V3, Version.V4)]
    public Version Version { get; set; }

    [Params(512, 1024 * 1024)]
    public int BufferSize { get; set; }

    [Params(1024 * 1024)]
    public int StreamLength { get; set; }

    public void Dispose()
    {
        readStream?.Dispose();
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        buffer = new byte[BufferSize];
        readStream = new MemoryStream(2 * StreamLength);

        using var storage = RootStorage.Create(readStream, Version, StorageModeFlags.LeaveOpen);
        using CfbStream stream = storage.CreateStream("Test");

        int iterationCount = StreamLength / BufferSize;
        for (int iteration = 0; iteration < iterationCount; ++iteration)
            stream.Write(buffer);
    }

    [GlobalCleanup]
    public void GlobalCleanup() => Dispose();

    [Benchmark]
    public void Read() => OpenMcdfBenchmarks.ReadStream(readStream!, buffer);

#if WINDOWS
    [Benchmark(Baseline = true)]
    public void ReadStructuredStorage() => StructuredStorageBenchmarks.ReadStream(readStream!, buffer);
#endif
}
