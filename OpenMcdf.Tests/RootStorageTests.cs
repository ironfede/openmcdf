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
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void ConsolidateMemoryStream(Version version)
    {
        byte[] buffer = new byte[4096];

        using MemoryStream memoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            using (CfbStream stream = rootStorage.CreateStream("Test"))
                stream.Write(buffer, 0, buffer.Length);

            Assert.AreEqual(1, rootStorage.EnumerateEntries().Count());

            rootStorage.Flush(true);

            int originalMemoryStreamLength = (int)memoryStream.Length;

            rootStorage.Delete("Test");

            rootStorage.Flush(true);

            Assert.IsTrue(originalMemoryStreamLength > memoryStream.Length);
        }

        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            Assert.AreEqual(0, rootStorage.EnumerateEntries().Count());
        }
    }

    [TestMethod]
    [DataRow(Version.V3, StorageModeFlags.None)]
    [DataRow(Version.V4, StorageModeFlags.Transacted)]
    public void ConsolidateFile(Version version, StorageModeFlags flags)
    {
        byte[] buffer = new byte[4096];

        string fileName = Path.GetTempFileName();

        try
        {
            using (var rootStorage = RootStorage.Create(fileName, version, flags))
            {
                using (CfbStream stream = rootStorage.CreateStream("Test"))
                    stream.Write(buffer, 0, buffer.Length);

                Assert.AreEqual(1, rootStorage.EnumerateEntries().Count());

                if (flags.HasFlag(StorageModeFlags.Transacted))
                    rootStorage.Commit();
                rootStorage.Flush(true);

                long originalLength = new FileInfo(fileName).Length;

                rootStorage.Delete("Test");

                if (flags.HasFlag(StorageModeFlags.Transacted))
                    rootStorage.Commit();
                rootStorage.Flush(true);

                long consolidatedLength = new FileInfo(fileName).Length;
                Assert.IsTrue(originalLength > consolidatedLength);
            }

            using (var rootStorage = RootStorage.OpenRead(fileName))
            {
                Assert.AreEqual(0, rootStorage.EnumerateEntries().Count());
            }
        }
        finally
        {
            try { File.Delete(fileName); } catch { }
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
            try { File.Delete(fileName); } catch { }
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
    public void SwitchTransactedStream(Version version, int subStorageCount)
    {
        using MemoryStream originalMemoryStream = new();
        using MemoryStream switchedMemoryStream = new();

        using (var rootStorage = RootStorage.Create(originalMemoryStream, version, StorageModeFlags.Transacted | StorageModeFlags.LeaveOpen))
        {
            for (int i = 0; i < subStorageCount; i++)
                rootStorage.CreateStorage($"Test{i}");

            rootStorage.SwitchTo(switchedMemoryStream);
            rootStorage.Commit();
        }

        using (var rootStorage = RootStorage.Open(switchedMemoryStream))
        {
            IEnumerable<EntryInfo> entries = rootStorage.EnumerateEntries();
            Assert.AreEqual(subStorageCount, entries.Count());

            for (int i = 0; i < subStorageCount; i++)
                rootStorage.OpenStorage($"Test{i}");
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DeleteTrimsBaseStream(bool consolidate)
    {
        using var rootStorage = RootStorage.CreateInMemory(Version.V3);
        using (CfbStream stream = rootStorage.CreateStream("Test"))
        {
            byte[] buffer = TestData.CreateByteArray(4096);
            stream.Write(buffer, 0, buffer.Length);
        }

        rootStorage.Flush(consolidate);

        long originalLength = rootStorage.BaseStream.Length;

        rootStorage.Delete("Test");
        rootStorage.Flush(consolidate);

        long newLength = rootStorage.BaseStream.Length;

        Assert.IsTrue(originalLength > newLength);
    }
}
