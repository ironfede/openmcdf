namespace OpenMcdf3.Tests;

[TestClass]
public sealed class StorageTests
{
    [TestMethod]
    [DataRow("MultipleStorage.cfs", 1)]
    [DataRow("MultipleStorage2.cfs", 1)]
    [DataRow("MultipleStorage3.cfs", 1)]
    [DataRow("MultipleStorage4.cfs", 1)]
    public void Read(string fileName, long storageCount)
    {
        using var rootStorage = RootStorage.OpenRead(fileName);
        IEnumerable<EntryInfo> storageEntries = rootStorage.EnumerateEntries(StorageType.Storage);
        Assert.AreEqual(storageCount, storageEntries.Count());
    }
}
