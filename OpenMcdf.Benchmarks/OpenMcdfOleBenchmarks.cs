using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using OpenMcdf.Ole;

namespace OpenMcdf.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[HideColumns(Column.AllocRatio)]
[MarkdownExporter]
public class OpenMcdfOleBenchmarks : IDisposable
{
    private RootStorage? rootStorage;
    private CfbStream? summaryInformationStream;
    private CfbStream? documentSummaryInformationStream;

    public void Dispose()
    {
        summaryInformationStream?.Dispose();
        documentSummaryInformationStream?.Dispose();
        rootStorage?.Dispose();
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        rootStorage = RootStorage.OpenRead("2custom.doc");
        summaryInformationStream = rootStorage.OpenStream(PropertySetNames.SummaryInformation);
        documentSummaryInformationStream = rootStorage.OpenStream(PropertySetNames.DocSummaryInformation);
    }

    [GlobalCleanup]
    public void GlobalCleanup() => Dispose();

    [Benchmark]
    public OlePropertiesContainer ReadSummaryInformation() => new(summaryInformationStream!);

    [Benchmark]
    public OlePropertiesContainer ReadDocumentSummaryInformation() => new(documentSummaryInformationStream!);
}
