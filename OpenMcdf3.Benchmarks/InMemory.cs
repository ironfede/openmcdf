using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace OpenMcdf3.Benchmark;

[SimpleJob]
[CsvExporter]
[HtmlExporter]
[MarkdownExporter]
//[DryCoreJob] // I always forget this attribute, so please leave it commented out
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class InMemory : IDisposable
{
    private const int Kb = 1024;
    private const int Mb = Kb * Kb;
    private const string storageName = "MyStorage";
    private const string streamName = "MyStream";

    private MemoryStream readStream = new();
    private MemoryStream writeStream = new();

    private byte[] buffer = Array.Empty<byte>();

    [Params(512, Mb /*Kb, 4 * Kb, 128 * Kb, 256 * Kb, 512 * Kb,*/)]
    public int BufferSize { get; set; }

    [Params(Mb /*, 8 * Mb, 64 * Mb, 128 * Mb*/)]
    public int TotalStreamSize { get; set; }

    public void Dispose()
    {
        readStream?.Dispose();
        writeStream?.Dispose();
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        buffer = new byte[BufferSize];
        readStream = new MemoryStream(2 * TotalStreamSize);
        writeStream = new MemoryStream(2 * TotalStreamSize);

        using var storage = RootStorage.Create(readStream, Version.V3, StorageModeFlags.LeaveOpen);
        using CfbStream stream = storage.CreateStream(streamName);

        int iterationCount = TotalStreamSize / BufferSize;
        for (int iteration = 0; iteration < iterationCount; ++iteration)
            stream.Write(buffer);
    }

    [Benchmark]
    public void Read()
    {
        using var compoundFile = RootStorage.Open(readStream);
        using CfbStream cfStream = compoundFile.OpenStream(streamName);
        long streamSize = cfStream.Length;
        long position = 0L;
        while (position < streamSize)
        {
            int read = cfStream.Read(buffer, 0, buffer.Length);
            if (read <= 0)
                throw new EndOfStreamException();
            position += read;
        }
    }

    [Benchmark]
    public void Write() => WriteCore(StorageModeFlags.None);

    [Benchmark]
    public void WriteTransacted() => WriteCore(StorageModeFlags.Transacted);

    void WriteCore(StorageModeFlags flags)
    {
        using var storage = RootStorage.Create(writeStream, Version.V3, flags);
        Storage subStorage = storage.CreateStorage(storageName);
        CfbStream stream = subStorage.CreateStream(streamName + 0);

        while (stream.Length < TotalStreamSize)
        {
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
