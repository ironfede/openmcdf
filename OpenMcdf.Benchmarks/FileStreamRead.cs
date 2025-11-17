using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using OpenMcdf.Benchmarks;

namespace OpenMcdf.Benchmark;

[MediumRunJob]
[MemoryDiagnoser]
[HideColumns(Column.AllocRatio)]
[MarkdownExporter]
public class FileStreamRead : IDisposable
{
    private string readFileName = "";
    private byte[] buffer = Array.Empty<byte>();

    [Params(Version.V3, Version.V4)]
    public Version Version { get; set; }

    [Params(512, 1024 * 1024)]
    public int BufferSize { get; set; }

    [Params(1024 * 1024)]
    public int StreamLength { get; set; }

    public void Dispose()
    {
        File.Delete(readFileName);
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        readFileName = Path.GetTempFileName();

        buffer = new byte[BufferSize];

        using var storage = RootStorage.Create(readFileName, Version);
        using CfbStream stream = storage.CreateStream("Test");

        int iterationCount = StreamLength / BufferSize;
        for (int iteration = 0; iteration < iterationCount; ++iteration)
            stream.Write(buffer);
    }

    [GlobalCleanup]
    public void GlobalCleanup() => Dispose();

    [Benchmark]
    public void Read() => OpenMcdfBenchmarks.ReadStream(readFileName, buffer);

#if WINDOWS
    [Benchmark(Baseline = true)]
    public void ReadStructuredStorage() => StructuredStorageBenchmarks.ReadStream(readFileName, buffer);
#endif
}
