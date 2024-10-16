namespace OpenMcdf3.Tests;

[TestClass]
public sealed class StreamTests
{
    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 63)]
    [DataRow(Version.V3, 64)]
    [DataRow(Version.V3, 65)]
    [DataRow(Version.V3, 511)]
    [DataRow(Version.V3, 512)]
    [DataRow(Version.V3, 513)]
    [DataRow(Version.V3, 4095)]
    [DataRow(Version.V3, 4096)]
    [DataRow(Version.V3, 4097)]
    [DataRow(Version.V4, 0)]
    [DataRow(Version.V4, 63)]
    [DataRow(Version.V4, 64)]
    [DataRow(Version.V4, 65)]
    [DataRow(Version.V4, 511)]
    [DataRow(Version.V4, 512)]
    [DataRow(Version.V4, 513)]
    [DataRow(Version.V4, 4095)]
    [DataRow(Version.V4, 4096)]
    [DataRow(Version.V4, 4097)]
    public void Read(Version version, int length)
    {
        string fileName = $"TestStream_v{(int)version}_{length}.cfs";
        using var rootStorage = RootStorage.OpenRead(fileName);
        using Stream stream = rootStorage.OpenStream("TestStream");
        Assert.AreEqual(length, stream.Length);

        // Test files are filled with bytes equal to their position modulo 256
        using MemoryStream expectedStream = new(length);
        for (int i = 0; i < length; i++)
            expectedStream.WriteByte((byte)i);

        using MemoryStream actualStream = new();
        stream.CopyTo(actualStream);

        StreamAssert.AreEqual(expectedStream, actualStream);
    }
}
