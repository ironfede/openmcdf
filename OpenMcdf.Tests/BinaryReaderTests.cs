namespace OpenMcdf.Tests;

[TestClass]
public sealed class BinaryReaderTests
{
    [TestMethod]
    public void ReadGuid()
    {
        byte[] bytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        using MemoryStream stream = new(bytes);
        using CfbBinaryReader reader = new(stream);
        Guid guid = reader.ReadGuid();
        Assert.AreEqual(new Guid(bytes), guid);
    }

    [TestMethod]
    public void ReadFileTime()
    {
        byte[] bytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        using MemoryStream stream = new(bytes);
        using CfbBinaryReader reader = new(stream);
        DateTime actual = reader.ReadFileTime();
        Assert.AreEqual(DirectoryEntry.ZeroFileTime, actual);
    }

    [TestMethod]
    [DataRow("TestStream_v3_0.cfs")]
    [DataRow("TestStream_v4_0.cfs")]
    public void ReadHeader(string fileName)
    {
        using FileStream stream = File.OpenRead(fileName);
        using CfbBinaryReader reader = new(stream);
        Header header = reader.ReadHeader();
    }
}
