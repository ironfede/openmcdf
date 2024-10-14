namespace OpenMcdf3.Tests;

[TestClass]
public sealed class CfbStreamTests
{
    [TestMethod]
    [DataRow("_Test.ppt", "Current User", 62)]
    [DataRow("test.cfb", "MyStream0", 1048576)]
    public void Read(string fileName, string streamName, long length)
    {
        using var rootStorage = RootStorage.OpenRead(fileName);
        using CfbStream stream = rootStorage.OpenStream(streamName);
        Assert.AreEqual(length, stream.Length);

        using MemoryStream memoryStream = new();
        stream.CopyTo(memoryStream);
        Assert.AreEqual(length, memoryStream.Length);
    }

    [TestMethod]
    [DataRow("_Test.ppt")]
    [DataRow("test.cfb")]
    public void ReadAllStreams(string fileName)
    {
        using var rootStorage = RootStorage.OpenRead(fileName);
        foreach (EntryInfo entryInfo in rootStorage.EnumerateEntries(StorageType.Stream))
        {
            using CfbStream stream = rootStorage.OpenStream(entryInfo.Name);
            using MemoryStream memoryStream = new();
            stream.CopyTo(memoryStream);
            //Assert.AreEqual(entryInfo.Length, memoryStream.Length);
        }
    }
}
