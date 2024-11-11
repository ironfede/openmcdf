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
        using (var rootStorage = RootStorage.OpenRead(fileName, StorageModeFlags.LeaveOpen))
        {
            IEnumerable<EntryInfo> storageEntries = rootStorage.EnumerateEntries(StorageType.Storage);
            Assert.AreEqual(storageCount, storageEntries.Count());
        }

#if WINDOWS
        using (var rootStorage = StructuredStorage.Storage.Open(fileName))
        {
            IEnumerable<string> entries = rootStorage.EnumerateEntries();
            Assert.AreEqual(storageCount, entries.Count());
        }
#endif
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
    public void CreateStorage(Version version, int subStorageCount)
    {
        using MemoryStream memoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            for (int i = 0; i < subStorageCount; i++)
                rootStorage.CreateStorage($"Test{i}");
            rootStorage.TraceDirectoryEntries(DebugWriter.Default);
        }

        memoryStream.Position = 0;
        using (var rootStorage = RootStorage.Open(memoryStream, StorageModeFlags.LeaveOpen))
        {
            IEnumerable<EntryInfo> entries = rootStorage.EnumerateEntries();
            Assert.AreEqual(subStorageCount, entries.Count());

            for (int i = 0; i < subStorageCount; i++)
                rootStorage.OpenStorage($"Test{i}");
        }

#if WINDOWS
        using (var rootStorage = StructuredStorage.Storage.Open(memoryStream))
        {
            IEnumerable<string> entries = rootStorage.EnumerateEntries();
            Assert.AreEqual(subStorageCount, entries.Count());

            for (int i = 0; i < subStorageCount; i++)
            {
                using StructuredStorage.Storage storage = rootStorage.OpenStorage($"Test{i}");
            }
        }
#endif
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void CreateDuplicateStorageThrowsException(Version version)
    {
        using MemoryStream memoryStream = new();
        using var rootStorage = RootStorage.Create(memoryStream, version);
        rootStorage.CreateStorage("Test");
        Assert.ThrowsException<IOException>(() => rootStorage.CreateStorage("Test"));
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void DeleteSingleStorage(Version version)
    {
        using MemoryStream memoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            rootStorage.CreateStorage("Test");
            Assert.AreEqual(1, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream, StorageModeFlags.LeaveOpen))
        {
            rootStorage.Delete("Test");
            Assert.AreEqual(0, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
        {
            Assert.AreEqual(0, rootStorage.EnumerateEntries().Count());
        }
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void DeleteRedBlackTreeChildLeaf(Version version)
    {
        using MemoryStream memoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            rootStorage.CreateStorage("Test1");
            rootStorage.CreateStorage("Test2");
            Assert.AreEqual(2, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream, StorageModeFlags.LeaveOpen))
        {
            rootStorage.Delete("Test1");
            Assert.AreEqual(1, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
        {
            Assert.AreEqual(1, rootStorage.EnumerateEntries().Count());
        }
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void DeleteRedBlackTreeSiblingLeaf(Version version)
    {
        using MemoryStream memoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            rootStorage.CreateStorage("Test1");
            rootStorage.CreateStorage("Test2");
            Assert.AreEqual(2, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream, StorageModeFlags.LeaveOpen))
        {
            rootStorage.Delete("Test2");
            Assert.AreEqual(1, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
        {
            Assert.AreEqual(1, rootStorage.EnumerateEntries().Count());
        }
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void DeleteRedBlackTreeSibling(Version version)
    {
        using MemoryStream memoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            rootStorage.CreateStorage("Test1");
            rootStorage.CreateStorage("Test2");
            rootStorage.CreateStorage("Test3");
            Assert.AreEqual(3, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream, StorageModeFlags.LeaveOpen))
        {
            rootStorage.Delete("Test2");
            Assert.AreEqual(2, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
        {
            Assert.AreEqual(2, rootStorage.EnumerateEntries().Count());
        }
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void DeleteStorageRecursively(Version version)
    {
        using MemoryStream memoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            Storage storage = rootStorage.CreateStorage("Test");
            Assert.AreEqual(1, rootStorage.EnumerateEntries().Count());

            using CfbStream stream = storage.CreateStream("Test");
        }

        using (var rootStorage = RootStorage.Open(memoryStream, StorageModeFlags.LeaveOpen))
        {
            rootStorage.Delete("Test");
            Assert.AreEqual(0, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
        {
            Assert.AreEqual(0, rootStorage.EnumerateEntries().Count());
        }
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void DeleteStream(Version version)
    {
        using MemoryStream memoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            rootStorage.CreateStream("Test");
            Assert.AreEqual(1, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
        {
            rootStorage.Delete("Test");
            Assert.AreEqual(0, rootStorage.EnumerateEntries().Count());
        }
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void Consolidate(Version version)
    {
        byte[] buffer = new byte[4096];

        using MemoryStream memoryStream = new();
        using var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen);
        CfbStream stream = rootStorage.CreateStream("Test");
        Assert.AreEqual(1, rootStorage.EnumerateEntries().Count());

        stream.Write(buffer, 0, buffer.Length);
        rootStorage.Flush();

        int originalMemoryStreamLength = (int)memoryStream.Length;

        rootStorage.Delete("Test");

        rootStorage.Flush(true);

        Assert.IsTrue(originalMemoryStreamLength > memoryStream.Length);
    }
}
