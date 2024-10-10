﻿using BenchmarkDotNet.Attributes;
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

    private byte[] _readBuffer;

    private MemoryStream _stream;

    [Params(Kb / 2, Kb, 4 * Kb, 128 * Kb, 256 * Kb, 512 * Kb, Kb * Kb)]
    public int BufferSize { get; set; }

    [Params(Mb /*, 8 * Mb, 64 * Mb, 128 * Mb*/)]
    public int TotalStreamSize { get; set; }

    public void Dispose()
    {
        _stream?.Dispose();
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _stream = new MemoryStream();
        //_stream = File.Create("D:\\test.cfb");
        _readBuffer = new byte[BufferSize];
        CreateFile(1);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _stream.Dispose();
        _stream = null;
        _readBuffer = null;
    }

    [Benchmark]
    public void Test()
    {
        //
        _stream.Seek(0L, SeekOrigin.Begin);
        //
        using var compoundFile = RootStorage.Open(_stream);
        using CfbStream cfStream = compoundFile.OpenStream(streamName + 0);
        long streamSize = cfStream.Length;
        long position = 0L;
        while (true)
        {
            if (position >= streamSize)
                break;
            int read = cfStream.Read(_readBuffer, 0, _readBuffer.Length);
            position += read;
            if (read <= 0) break;
        }

        //compoundFile.Close();
    }

    private void CreateFile(int streamCount)
    {
        var iterationCount = TotalStreamSize / BufferSize;

        var buffer = new byte[BufferSize];
        Array.Fill(buffer, byte.MaxValue);
        const CFSConfiguration flags = CFSConfiguration.Default | CFSConfiguration.LeaveOpen;
        using (var compoundFile = new CompoundFile(CFSVersion.Ver_3, flags))
        {
            //var st = compoundFile.RootStorage.AddStorage(storageName);
            var st = compoundFile.RootStorage;
            for (var streamId = 0; streamId < streamCount; ++streamId)
            {
                var sm = st.AddStream(streamName + streamId);

                for (var iteration = 0; iteration < iterationCount; ++iteration) sm.Append(buffer);
            }

            compoundFile.Save(_stream);
            compoundFile.Close();
        }
    }
}
