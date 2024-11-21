namespace OpenMcdf.Tests;

[TestClass]
public sealed class RootStorageTests
{
    [TestMethod]
    [DoNotParallelize] // Test sharing
    [DataRow("TestStream_v3_0.cfs")]
    public void Open(string fileName)
    {
        using var rootStorage = RootStorage.OpenRead(fileName);
        using var rootStorage2 = RootStorage.OpenRead(fileName);

        Assert.ThrowsException<IOException>(() => RootStorage.Open(fileName, FileMode.Open));
        Assert.ThrowsException<IOException>(() => RootStorage.Open(fileName, FileMode.Open, FileAccess.ReadWrite));

        using CfbStream stream = rootStorage.OpenStream("TestStream");
        Assert.ThrowsException<NotSupportedException>(() => stream.WriteByte(0));

        Assert.ThrowsException<NotSupportedException>(() => rootStorage.CreateStream("TestStream2"));
        Assert.ThrowsException<NotSupportedException>(() => rootStorage.CreateStorage("TestStream2"));
        Assert.ThrowsException<NotSupportedException>(() => rootStorage.Delete("TestStream"));
        Assert.ThrowsException<NotSupportedException>(() => rootStorage.Commit());
        Assert.ThrowsException<NotSupportedException>(() => rootStorage.Revert());
        Assert.ThrowsException<NotSupportedException>(() => rootStorage.CreationTime = DateTime.MinValue);
        Assert.ThrowsException<NotSupportedException>(() => rootStorage.ModifiedTime = DateTime.MinValue);
        Assert.ThrowsException<NotSupportedException>(() => rootStorage.CLISD = Guid.Empty);
        Assert.ThrowsException<NotSupportedException>(() => rootStorage.StateBits = 0);
    }

    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 1)]
    [DataRow(Version.V3, 2)]
    [DataRow(Version.V3, 4)] // Required 2 sectors including root
    [DataRow(Version.V4, 0)]
    [DataRow(Version.V4, 1)]
    [DataRow(Version.V4, 2)]
    [DataRow(Version.V4, 32)] // Required 2 sectors including root
    public void SwitchStream(Version version, int subStorageCount)
    {
        using MemoryStream memoryStream = new();
        using MemoryStream switchedMemoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            for (int i = 0; i < subStorageCount; i++)
                rootStorage.CreateStorage($"Test{i}");

            rootStorage.SwitchTo(switchedMemoryStream);
        }

        memoryStream.Position = 0;
        using (var rootStorage = RootStorage.Open(switchedMemoryStream, StorageModeFlags.LeaveOpen))
        {
            IEnumerable<EntryInfo> entries = rootStorage.EnumerateEntries();
            Assert.AreEqual(subStorageCount, entries.Count());

            for (int i = 0; i < subStorageCount; i++)
                rootStorage.OpenStorage($"Test{i}");
        }
    }

    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 1)]
    [DataRow(Version.V3, 2)]
    [DataRow(Version.V3, 4)] // Required 2 sectors including root
    [DataRow(Version.V4, 0)]
    [DataRow(Version.V4, 1)]
    [DataRow(Version.V4, 2)]
    [DataRow(Version.V4, 32)] // Required 2 sectors including root
    public void SwitchFile(Version version, int subStorageCount)
    {
        string fileName = Path.GetTempFileName();

        try
        {
            using (var rootStorage = RootStorage.CreateInMemory(version))
            {
                for (int i = 0; i < subStorageCount; i++)
                    rootStorage.CreateStorage($"Test{i}");

                rootStorage.SwitchTo(fileName);
            }

            using (var rootStorage = RootStorage.OpenRead(fileName))
            {
                IEnumerable<EntryInfo> entries = rootStorage.EnumerateEntries();
                Assert.AreEqual(subStorageCount, entries.Count());

                for (int i = 0; i < subStorageCount; i++)
                    rootStorage.OpenStorage($"Test{i}");
            }
        }
        finally
        {
            File.Delete(fileName);
        }
    }
}
