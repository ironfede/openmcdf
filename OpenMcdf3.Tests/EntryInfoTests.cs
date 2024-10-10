namespace OpenMcdf3.Tests;

[TestClass]
public sealed class EntryInfoTests
{
    [TestMethod]
    [DataRow("_Test.ppt", 5)]
    public void EnumerateEntryInfos(string fileName, int count)
    {
        using var rootStorage = RootStorage.Open(fileName, FileMode.Open);
        Assert.AreEqual(count, rootStorage.EnumerateEntries().Count());
    }
}
