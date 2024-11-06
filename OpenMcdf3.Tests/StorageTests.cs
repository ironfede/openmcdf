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
        using (var rootStorage = RootStorage.Create(memoryStream, version))
        {
            for (int i = 0; i < subStorageCount; i++)
                rootStorage.CreateStorage($"Test{i}");
        }

        memoryStream.Position = 0;
        using (var rootStorage = RootStorage.Open(memoryStream))
        {
            IEnumerable<EntryInfo> entries = rootStorage.EnumerateEntries();
            Assert.AreEqual(subStorageCount, entries.Count());
        }
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
        using (var rootStorage = RootStorage.Create(memoryStream, version))
        {
            rootStorage.CreateStorage("Test");
            Assert.AreEqual(1, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
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
        using (var rootStorage = RootStorage.Create(memoryStream, version))
        {
            rootStorage.CreateStorage("Test1");
            rootStorage.CreateStorage("Test2");
            Assert.AreEqual(2, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
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
        using (var rootStorage = RootStorage.Create(memoryStream, version))
        {
            rootStorage.CreateStorage("Test1");
            rootStorage.CreateStorage("Test2");
            Assert.AreEqual(2, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
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
        using (var rootStorage = RootStorage.Create(memoryStream, version))
        {
            rootStorage.CreateStorage("Test1");
            rootStorage.CreateStorage("Test2");
            rootStorage.CreateStorage("Test3");
            Assert.AreEqual(3, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
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
        using (var rootStorage = RootStorage.Create(memoryStream, version))
        {
            Storage storage = rootStorage.CreateStorage("Test");
            Assert.AreEqual(1, rootStorage.EnumerateEntries().Count());

            using CfbStream stream = storage.CreateStream("Test");
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
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
        using (var rootStorage = RootStorage.Create(memoryStream, version))
        {
            rootStorage.CreateStream("Test");
            Assert.AreEqual(1, rootStorage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Create(memoryStream, version))
        {
            rootStorage.Delete("Test");
            Assert.AreEqual(0, rootStorage.EnumerateEntries().Count());
        }
    }
}
