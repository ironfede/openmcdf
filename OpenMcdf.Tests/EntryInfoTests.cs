namespace OpenMcdf.Tests;

[TestClass]
public sealed class EntryInfoTests
{
    [TestMethod]
    [DataRow("MultipleStorage.cfs", 1)]
    [DataRow("TestStream_v3_0.cfs", 1)]
    [DataRow("TestStream_v4_0.cfs", 1)]
    public void EnumerateEntryInfos(string fileName, int count)
    {
        using var rootStorage = RootStorage.OpenRead(fileName);
        IEnumerable<EntryInfo> entries = rootStorage.EnumerateEntries();
        Assert.AreEqual(count, entries.Count());
    }
}
