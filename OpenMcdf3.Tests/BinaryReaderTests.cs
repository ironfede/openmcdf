namespace OpenMcdf3.Tests;

[TestClass]
public class BinaryReaderTests
{
    [TestMethod]
    public void Guid()
    {
        byte[] bytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        using MemoryStream stream = new(bytes);
        using McdfBinaryReader reader = new(stream);
        Guid guid = reader.ReadGuid();
        Assert.AreEqual(new Guid(bytes), guid);
    }
}