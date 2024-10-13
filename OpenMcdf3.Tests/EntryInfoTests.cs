namespace OpenMcdf3.Tests;

[TestClass]
public sealed class EntryInfoTests
{
    [TestMethod]
    [DataRow("_Test.ppt", 5)]
    [DataRow("test.cfb", 2)]
    public void EnumerateEntryInfos(string fileName, int count)
    {
        using var rootStorage = RootStorage.OpenRead(fileName);
        IEnumerable<EntryInfo> entries = rootStorage.EnumerateEntries();
        Assert.AreEqual(count, entries.Count());
    }
}
