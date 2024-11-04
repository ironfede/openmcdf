using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using OpenMcdf;

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

    private byte[] readBuffer;

    private readonly MemoryStream readStream = new();
    private readonly MemoryStream writeStream = new();

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
        readBuffer = new byte[BufferSize];
        CreateFile(1);
    }

    [Benchmark]
    public void Read()
    {
        using var compoundFile = RootStorage.Open(readStream);
        using CfbStream cfStream = compoundFile.OpenStream(streamName + 0);
        long streamSize = cfStream.Length;
        long position = 0L;
        while (true)
        {
            if (position >= streamSize)
                break;
            int read = cfStream.Read(readBuffer, 0, readBuffer.Length);
            position += read;
            if (read <= 0) break;
        }
    }

    [Benchmark]
    public void Write()
    {
        MemoryStream memoryStream = writeStream;
        using var storage = RootStorage.Create(memoryStream);
        Storage subStorage = storage.CreateStorage(storageName);
        CfbStream stream = subStorage.CreateStream(streamName + 0);

        while (stream.Length < TotalStreamSize)
        {
            stream.Write(readBuffer, 0, readBuffer.Length);
        }
    }

    private void CreateFile(int streamCount)
    {
        int iterationCount = TotalStreamSize / BufferSize;

        byte[] buffer = new byte[BufferSize];
        buffer.AsSpan().Fill(byte.MaxValue);
        const CFSConfiguration flags = CFSConfiguration.Default | CFSConfiguration.LeaveOpen;
        using var compoundFile = new CompoundFile(CFSVersion.Ver_3, flags);
        CFStorage st = compoundFile.RootStorage;
        for (int streamId = 0; streamId < streamCount; ++streamId)
        {
            CFStream sm = st.AddStream(streamName + streamId);
            for (int iteration = 0; iteration < iterationCount; ++iteration)
                sm.Append(buffer);
        }

        compoundFile.Save(readStream);
        compoundFile.Close();
    }
}
