namespace OpenMcdf.Tests;

[TestClass]
public sealed class RootStorageTests
{
    [TestMethod]
    [DoNotParallelize] // Test sharing
    [DataRow("MultipleStorage.cfs")]
    public void Open(string fileName)
    {
        using var rootStorage = RootStorage.OpenRead(fileName);
        using var rootStorage2 = RootStorage.OpenRead(fileName);

        Assert.ThrowsException<IOException>(() => RootStorage.Open(fileName, FileMode.Open));
        Assert.ThrowsException<IOException>(() => RootStorage.OpenWrite(fileName));
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
