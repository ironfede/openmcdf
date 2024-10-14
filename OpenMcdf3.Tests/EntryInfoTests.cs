namespace OpenMcdf3.Tests;

[TestClass]
public sealed class EntryInfoTests
{
    [TestMethod]
    [DataRow("_Test.ppt", 4)]
    [DataRow("test.cfb", 1)]
    public void EnumerateEntryInfos(string fileName, int count)
    {
        using var rootStorage = RootStorage.OpenRead(fileName);
        IEnumerable<EntryInfo> entries = rootStorage.EnumerateEntries();
        Assert.AreEqual(count, entries.Count());
    }
}
