namespace OpenMcdf.Tests;

[TestClass]
public sealed class StorageTests
{
    [TestMethod]
    [DataRow("MultipleStorage.cfs", 1)]
    [DataRow("MultipleStorage2.cfs", 1)]
    [DataRow("MultipleStorage3.cfs", 1)]
    [DataRow("MultipleStorage4.cfs", 1)]
    public void EnumerateEntries(string fileName, long storageCount)
    {
        using var rootStorage = RootStorage.OpenRead(fileName);
        IEnumerable<EntryInfo> storageEntries = rootStorage.EnumerateEntries();
        Assert.AreEqual(storageCount, storageEntries.Count());
    }

    [TestMethod]
    [DataRow("MultipleStorage.cfs")]
    public void OpenStorage(string fileName)
    {
        using var rootStorage = RootStorage.OpenRead(fileName);
        Assert.IsTrue(rootStorage.TryOpenStorage("MyStorage", out Storage? _));
        Assert.IsFalse(rootStorage.TryOpenStorage("", out Storage? _));

        Assert.ThrowsExactly<DirectoryNotFoundException>(() => rootStorage.OpenStorage(""));

        Assert.IsTrue(rootStorage.ContainsEntry("MyStorage"));
        Assert.IsFalse(rootStorage.ContainsEntry("NonExistentStorage"));

        bool found = rootStorage.TryGetEntryInfo("MyStorage", out EntryInfo entryInfo);
        Assert.IsTrue(found);
        Assert.IsNotNull(entryInfo);
        Assert.AreEqual("MyStorage", entryInfo.Name);

        Storage storage = rootStorage.OpenStorage("MyStorage");
        Assert.AreEqual("MyStorage", storage.EntryInfo.Name);
    }

    [TestMethod]
    [DataRow("FatChainLoop_v3.cfs")]
    [Ignore("Test file has multiple validation errors")]
    public void FatChainLoop(string fileName)
    {
        using var rootStorage = RootStorage.OpenRead(fileName);
        Assert.ThrowsExactly<FileFormatException>(() => rootStorage.OpenStorage("Anything"));
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

        // Test adding right sibling
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            for (int i = 0; i < subStorageCount; i++)
                rootStorage.CreateStorage($"Test{i}");
        }

        using (var rootStorage = RootStorage.Open(memoryStream, StorageModeFlags.LeaveOpen))
        {
            IEnumerable<EntryInfo> entries = rootStorage.EnumerateEntries();
            Assert.AreEqual(subStorageCount, entries.Count());

            for (int i = 0; i < subStorageCount; i++)
                rootStorage.OpenStorage($"Test{i}");
        }

        // Test adding left sibling
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            for (int i = 0; i < subStorageCount; i++)
                rootStorage.CreateStorage($"Test{subStorageCount - i}");
        }

        using (var rootStorage = RootStorage.Open(memoryStream, StorageModeFlags.LeaveOpen))
        {
            IEnumerable<EntryInfo> entries = rootStorage.EnumerateEntries();
            Assert.AreEqual(subStorageCount, entries.Count());

            for (int i = 0; i < subStorageCount; i++)
                rootStorage.OpenStorage($"Test{subStorageCount - i}");
        }

#if WINDOWS
        using (var rootStorage = StructuredStorage.Storage.Open(memoryStream))
        {
            IEnumerable<string> entries = rootStorage.EnumerateEntries();
            Assert.AreEqual(subStorageCount, entries.Count());

            for (int i = 0; i < subStorageCount; i++)
            {
                using StructuredStorage.Storage storage = rootStorage.OpenStorage($"Test{subStorageCount - i}");
            }
        }
#endif
    }

    [TestMethod]
    public void CreateInvalidStorageName()
    {
        using MemoryStream memoryStream = new();
        using var rootStorage = RootStorage.Create(memoryStream);
        Assert.ThrowsExactly<ArgumentException>(() => rootStorage.CreateStorage("!"));
        Assert.ThrowsExactly<ArgumentException>(() => rootStorage.CreateStorage("/"));
        Assert.ThrowsExactly<ArgumentException>(() => rootStorage.CreateStorage(":"));
        Assert.ThrowsExactly<ArgumentException>(() => rootStorage.CreateStorage("\\"));
    }

    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 1)]
    [DataRow(Version.V4, 0)]
    [DataRow(Version.V4, 1)]
    public void CreateStorageFile(Version version, int subStorageCount)
    {
        string fileName = Path.GetTempFileName();

        try
        {
            using (var rootStorage = RootStorage.Create(fileName, version))
            {
                for (int i = 0; i < subStorageCount; i++)
                    rootStorage.CreateStorage($"Test{i}");
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
            try { File.Delete(fileName); } catch { }
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
        Assert.ThrowsExactly<IOException>(() => rootStorage.CreateStorage("Test"));
    }

    private static void CreateDeleteTest(Version version, MemoryStream memoryStream)
    {
        using var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen);
        rootStorage.CreateStorage("Test2");
        rootStorage.CreateStorage("Test1");
        rootStorage.CreateStorage("Test3");
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void DeleteStorageLeft(Version version)
    {
        using MemoryStream memoryStream = new();

        CreateDeleteTest(version, memoryStream);

        using var rootStorage = RootStorage.Open(memoryStream);
        rootStorage.Delete("NonExistentEntry");
        Assert.AreEqual(3, rootStorage.EnumerateEntries().Count());

        rootStorage.Delete("Test1");
        Assert.AreEqual(2, rootStorage.EnumerateEntries().Count());
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void DeleteStorageRoot(Version version)
    {
        using MemoryStream memoryStream = new();

        CreateDeleteTest(version, memoryStream);

        using var rootStorage = RootStorage.Open(memoryStream);
        rootStorage.Delete("Test2");
        Assert.AreEqual(2, rootStorage.EnumerateEntries().Count());
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void DeleteStorageRight(Version version)
    {
        using MemoryStream memoryStream = new();

        CreateDeleteTest(version, memoryStream);

        using var rootStorage = RootStorage.Open(memoryStream);
        rootStorage.Delete("Test3");
        Assert.AreEqual(2, rootStorage.EnumerateEntries().Count());
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void DeleteStorageAll(Version version)
    {
        using MemoryStream memoryStream = new();

        CreateDeleteTest(version, memoryStream);

        using var rootStorage = RootStorage.Open(memoryStream);
        rootStorage.Delete("Test1");
        rootStorage.Delete("Test2");
        rootStorage.Delete("Test3");
        Assert.AreEqual(0, rootStorage.EnumerateEntries().Count());
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void DeleteStorageRecursively(Version version)
    {
        using MemoryStream memoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            Storage storage = rootStorage.CreateStorage("Storage");
            Assert.AreEqual(1, rootStorage.EnumerateEntries().Count());

            Storage subStorage = storage.CreateStorage("SubStorage");
            using CfbStream subStream = subStorage.CreateStream("SubStream");
            Assert.AreEqual(1, storage.EnumerateEntries().Count());
        }

        using (var rootStorage = RootStorage.Open(memoryStream, StorageModeFlags.LeaveOpen))
        {
            rootStorage.Delete("Storage");
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
    public void GetAndSetMetadata(Version version)
    {
        using MemoryStream memoryStream = new();

        Guid guid = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            Storage storage = rootStorage.CreateStorage("Storage");

            Assert.AreEqual(Guid.Empty, storage.CLSID);
            Assert.AreNotEqual(FileTime.UtcZero, storage.CreationTime);
            Assert.AreNotEqual(FileTime.UtcZero, storage.ModifiedTime);
            Assert.AreEqual(0U, storage.StateBits);

            storage.CLSID = guid;
            storage.CreationTime = now;
            storage.ModifiedTime = now;
            storage.StateBits = 1U;
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
        {
            Storage storage = rootStorage.OpenStorage("Storage");

            Assert.AreEqual(guid, storage.CLSID);
            Assert.AreEqual(now, storage.CreationTime);
            Assert.AreEqual(now, storage.ModifiedTime);
            Assert.AreEqual(1U, storage.StateBits);
        }
    }
}
