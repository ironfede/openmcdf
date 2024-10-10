namespace OpenMcdf3.Tests;

[TestClass]
public sealed class CfbStreamTests
{
    [TestMethod]
    [DataRow("_Test.ppt", "Current User", 62)]
    [DataRow("test.cfb", "MyStream0", 1048576)]
    public void CfbStreamTest(string fileName, string streamName, long length)
    {
        using var rootStorage = RootStorage.Open(fileName, FileMode.Open);
        using var stream = rootStorage.OpenStream(streamName);
        Assert.AreEqual(length, stream.Length);

        using MemoryStream memoryStream = new();
        stream.CopyTo(memoryStream);
        Assert.AreEqual(length, memoryStream.Length);
    }
}
