using BenchmarkDotNet.Attributes;
using OpenMcdf3.Benchmarks;

namespace OpenMcdf3.Benchmark;

[ShortRunJob]
[MemoryDiagnoser]
public class MemoryStreamIO : IDisposable
{
    const Version version = Version.V3;

    private MemoryStream? readStream;
    private MemoryStream? writeStream;

    private byte[] buffer = Array.Empty<byte>();

    [Params(512, 1024 * 1024)]
    public int BufferSize { get; set; }

    [Params(1024 * 1024)]
    public int StreamLength { get; set; }

    public void Dispose()
    {
        readStream?.Dispose();
        writeStream?.Dispose();
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        buffer = new byte[BufferSize];
        readStream = new MemoryStream(2 * StreamLength);
        writeStream = new MemoryStream(2 * StreamLength);

        using var storage = RootStorage.Create(readStream, version, StorageModeFlags.LeaveOpen);
        using CfbStream stream = storage.CreateStream("Test");

        int iterationCount = StreamLength / BufferSize;
        for (int iteration = 0; iteration < iterationCount; ++iteration)
            stream.Write(buffer);
    }

    [Benchmark]
    public void Read() => OpenMcdfBenchmarks.ReadStream(readStream!, buffer);

    [Benchmark]
    public void Write() => OpenMcdfBenchmarks.WriteStream(writeStream!, version, StorageModeFlags.LeaveOpen, buffer, StreamLength);

    [Benchmark]
    public void WriteTransacted() => OpenMcdfBenchmarks.WriteStream(writeStream!, version, StorageModeFlags.LeaveOpen | StorageModeFlags.Transacted, buffer, StreamLength);

#if WINDOWS
    [Benchmark]
    public void ReadStructuredStorage() => StructuredStorageBenchmarks.ReadStream(readStream!, buffer);

    [Benchmark]
    public void WriteStructuredStorage() => StructuredStorageBenchmarks.WriteInMemory(version, StorageModeFlags.LeaveOpen, buffer, StreamLength);
#endif
}
