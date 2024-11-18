namespace OpenMcdf.Tests;

[TestClass]
public sealed class StreamTests
{
    [TestMethod]
    [DataRow("TestStream_v3_0.cfs")]
    public void OpenStream(string fileName)
    {
        using var rootStorage = RootStorage.OpenRead(fileName);
        Assert.IsTrue(rootStorage.TryOpenStream("TestStream", out CfbStream? _));
        Assert.IsFalse(rootStorage.TryOpenStream("", out CfbStream? _));

        Assert.ThrowsException<FileNotFoundException>(() => rootStorage.OpenStream(""));

        CfbStream stream = rootStorage.OpenStream("TestStream");
        Assert.AreEqual("TestStream", stream.EntryInfo.Name);
    }

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
    [DataRow(Version.V3, 65536)]
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
    public void ReadViaCopyTo(Version version, int length)
    {
        // Test files are filled with bytes equal to their position modulo 256
        using MemoryStream expectedStream = new(length);
        for (int i = 0; i < length; i++)
            expectedStream.WriteByte((byte)i);

        string fileName = $"TestStream_v{(int)version}_{length}.cfs";
        using var rootStorage = RootStorage.OpenRead(fileName);

        using Stream stream = rootStorage.OpenStream("TestStream");
        Assert.AreEqual(length, stream.Length);

        using MemoryStream actualStream = new();
        stream.CopyTo(actualStream);

        StreamAssert.AreEqual(expectedStream, actualStream);
    }

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
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
    [DataRow(Version.V3, 65536)]
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
    public void ReadSpan(Version version, int length)
    {
        // Test files are filled with bytes equal to their position modulo 256
        byte[] expectedBuffer = new byte[length];
        for (int i = 0; i < length; i++)
            expectedBuffer[i] = ((byte)i);

        string fileName = $"TestStream_v{(int)version}_{length}.cfs";
        using var rootStorage = RootStorage.OpenRead(fileName);

        using Stream stream = rootStorage.OpenStream("TestStream");
        Assert.AreEqual(length, stream.Length);

        byte[] actualBuffer = new byte[length];
        stream.Read(actualBuffer);

        CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
    }
#endif

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
    [DataRow(Version.V3, 65536)]
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
    public void ReadSingleByte(Version version, int length)
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
        for (int i = 0; i < length; i++)
        {
            int value = stream.ReadByte();
            Assert.AreNotEqual(-1, value, "End of stream");
            actualStream.WriteByte((byte)value);
        }

        StreamAssert.AreEqual(expectedStream, actualStream);
    }

    [TestMethod]
    [DataRow(Version.V3, 64)] // Mini-stream
    [DataRow(Version.V4, 4096)] // Regular stream
    public void Seek(Version version, int length)
    {
        string fileName = $"TestStream_v{(int)version}_{length}.cfs";
        using var rootStorage = RootStorage.OpenRead(fileName);
        using Stream stream = rootStorage.OpenStream("TestStream");

        stream.Seek(0, SeekOrigin.Begin);
        Assert.ThrowsException<IOException>(() => stream.Seek(-1, SeekOrigin.Begin));
        Assert.ThrowsException<IOException>(() => stream.Seek(-1, SeekOrigin.Current));
        Assert.ThrowsException<IOException>(() => stream.Seek(length + 1, SeekOrigin.End));
        Assert.ThrowsException<ArgumentException>(() => stream.Seek(length, (SeekOrigin)3));
    }

    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 63)]
    [DataRow(Version.V3, 64)] // Mini-stream sector size
    [DataRow(Version.V3, 65)]
    [DataRow(Version.V3, 511)]
    [DataRow(Version.V3, 512)] // Multiple stream sectors
    [DataRow(Version.V3, 513)]
    [DataRow(Version.V3, 4095)]
    [DataRow(Version.V3, 4096)]
    [DataRow(Version.V3, 4097)]
    [DataRow(Version.V3, 128 * 512)] // Multiple FAT sectors
    [DataRow(Version.V3, 1024 * 4096)] // Multiple FAT sectors
    [DataRow(Version.V3, 7087616)] // First DIFAT chain
    [DataRow(Version.V3, 2 * 7087616)] // Long DIFAT chain
    [DataRow(Version.V4, 0)]
    [DataRow(Version.V4, 63)]
    [DataRow(Version.V4, 64)] // Mini-stream sector size
    [DataRow(Version.V4, 65)]
    [DataRow(Version.V4, 511)]
    [DataRow(Version.V4, 512)]
    [DataRow(Version.V4, 513)]
    [DataRow(Version.V4, 4095)]
    [DataRow(Version.V4, 4096)] // Multiple stream sectors
    [DataRow(Version.V4, 4097)]
    [DataRow(Version.V4, 1024 * 4096)] // Multiple FAT sectors (1024 * 4096)
    [DataRow(Version.V4, 7087616 * 4)] // First DIFAT chain
    [DataRow(Version.V4, 2 * 7087616 * 4)] // Long DIFAT chain
    public void Write(Version version, int length) => WriteCore(version, length, false);

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 63)]
    [DataRow(Version.V3, 64)] // Mini-stream sector size
    [DataRow(Version.V3, 65)]
    [DataRow(Version.V3, 511)]
    [DataRow(Version.V3, 512)] // Multiple stream sectors
    [DataRow(Version.V3, 513)]
    [DataRow(Version.V3, 4095)]
    [DataRow(Version.V3, 4096)]
    [DataRow(Version.V3, 4097)]
    [DataRow(Version.V3, 128 * 512)] // Multiple FAT sectors
    [DataRow(Version.V3, 1024 * 4096)] // Multiple FAT sectors
    [DataRow(Version.V3, 7087616)] // First DIFAT chain
    [DataRow(Version.V3, 2 * 7087616)] // Long DIFAT chain
    [DataRow(Version.V4, 0)]
    [DataRow(Version.V4, 63)]
    [DataRow(Version.V4, 64)] // Mini-stream sector size
    [DataRow(Version.V4, 65)]
    [DataRow(Version.V4, 511)]
    [DataRow(Version.V4, 512)]
    [DataRow(Version.V4, 513)]
    [DataRow(Version.V4, 4095)]
    [DataRow(Version.V4, 4096)] // Multiple stream sectors
    [DataRow(Version.V4, 4097)]
    [DataRow(Version.V4, 1024 * 4096)] // Multiple FAT sectors (1024 * 4096)
    [DataRow(Version.V4, 7087616 * 4)] // First DIFAT chain
    [DataRow(Version.V4, 2 * 7087616 * 4)] // Long DIFAT chain
    public void WriteSpan(Version version, int length) => WriteCore(version, length, true);
#endif

    static void WriteCore(Version version, int length, bool preferSpan)
    {
        using MemoryStream memoryStream = new();
        using var rootStorage = RootStorage.Create(memoryStream, version);
        using CfbStream stream = rootStorage.CreateStream("TestStream");
        Assert.AreEqual(0, stream.Length);

        // Fill with bytes equal to their position modulo 256
        byte[] expectedBuffer = new byte[length];
        for (int i = 0; i < length; i++)
            expectedBuffer[i] = (byte)i;

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
        if (preferSpan)
            stream.Write(expectedBuffer);
        else
            stream.Write(expectedBuffer, 0, expectedBuffer.Length);
#else
        stream.Write(expectedBuffer, 0, expectedBuffer.Length);
#endif

        Assert.AreEqual(length, stream.Length);
        Assert.AreEqual(length, stream.Position);

        rootStorage.Validate();

        byte[] actualBuffer = new byte[length];
        stream.Position = 0;
        stream.ReadExactly(actualBuffer);

        CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
    }

    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 63)]
    [DataRow(Version.V3, 64)] // Mini-stream sector size
    [DataRow(Version.V3, 65)]
    [DataRow(Version.V3, 511)]
    [DataRow(Version.V3, 512)] // Multiple stream sectors
    [DataRow(Version.V3, 513)]
    [DataRow(Version.V3, 4095)]
    [DataRow(Version.V3, 4096)]
    [DataRow(Version.V3, 4097)]
    [DataRow(Version.V3, 128 * 512)] // Multiple FAT sectors
    [DataRow(Version.V3, 1024 * 4096)] // Multiple FAT sectors
    [DataRow(Version.V3, 7087616)] // First DIFAT chain
    [DataRow(Version.V3, 2 * 7087616)] // Long DIFAT chain
    [DataRow(Version.V4, 0)]
    [DataRow(Version.V4, 63)]
    [DataRow(Version.V4, 64)] // Mini-stream sector size
    [DataRow(Version.V4, 65)]
    [DataRow(Version.V4, 511)]
    [DataRow(Version.V4, 512)]
    [DataRow(Version.V4, 513)]
    [DataRow(Version.V4, 4095)]
    [DataRow(Version.V4, 4096)] // Multiple stream sectors
    [DataRow(Version.V4, 4097)]
    [DataRow(Version.V4, 1024 * 4096)] // Multiple FAT sectors (1024 * 4096)
    [DataRow(Version.V4, 7087616 * 4)] // First DIFAT chain
    [DataRow(Version.V4, 2 * 7087616 * 4)] // Long DIFAT chain
    public void WriteThenRead(Version version, int length)
    {
        // Fill with bytes equal to their position modulo 256
        byte[] expectedBuffer = new byte[length];
        for (int i = 0; i < length; i++)
            expectedBuffer[i] = (byte)i;

        using MemoryStream memoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            using CfbStream stream = rootStorage.CreateStream("TestStream");
            Assert.AreEqual(0, stream.Length);

            stream.Write(expectedBuffer, 0, expectedBuffer.Length);
        }

        byte[] actualBuffer = new byte[length];
        using (var rootStorage = RootStorage.Open(memoryStream, StorageModeFlags.LeaveOpen))
        {
            using CfbStream stream = rootStorage.OpenStream("TestStream");
            rootStorage.Validate();
            Assert.AreEqual(length, stream.Length);

            stream.ReadExactly(actualBuffer);
            CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
        }

#if WINDOWS
        using (var rootStorage = StructuredStorage.Storage.Open(memoryStream))
        {
            IEnumerable<string> entries = rootStorage.EnumerateEntries();
            using StructuredStorage.Stream stream = rootStorage.OpenStream("TestStream");
            Assert.AreEqual(length, stream.Length);

            stream.ReadExactly(actualBuffer);
            CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
        }
#endif
    }

#if WINDOWS
    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 63)]
    [DataRow(Version.V3, 64)] // Mini-stream sector size
    [DataRow(Version.V3, 65)]
    [DataRow(Version.V3, 511)]
    [DataRow(Version.V3, 512)] // Multiple stream sectors
    [DataRow(Version.V3, 513)]
    [DataRow(Version.V3, 4095)]
    [DataRow(Version.V3, 4096)]
    [DataRow(Version.V3, 4097)]
    [DataRow(Version.V3, 128 * 512)] // Multiple FAT sectors
    [DataRow(Version.V3, 1024 * 4096)] // Multiple FAT sectors
    [DataRow(Version.V3, 7087616)] // First DIFAT chain
    [DataRow(Version.V3, 2 * 7087616)] // Long DIFAT chain
    [DataRow(Version.V4, 0)]
    [DataRow(Version.V4, 63)]
    [DataRow(Version.V4, 64)] // Mini-stream sector size
    [DataRow(Version.V4, 65)]
    [DataRow(Version.V4, 511)]
    [DataRow(Version.V4, 512)]
    [DataRow(Version.V4, 513)]
    [DataRow(Version.V4, 4095)]
    [DataRow(Version.V4, 4096)] // Multiple stream sectors
    [DataRow(Version.V4, 4097)]
    [DataRow(Version.V4, 1024 * 4096)] // Multiple FAT sectors (1024 * 4096)
    [DataRow(Version.V4, 7087616 * 4)] // First DIFAT chain
    [DataRow(Version.V4, 2 * 7087616 * 4)] // Long DIFAT chain
    public void StructuredStorageWriteThenRead(Version version, int length)
    {
        // Fill with bytes equal to their position modulo 256
        byte[] expectedBuffer = new byte[length];
        for (int i = 0; i < length; i++)
            expectedBuffer[i] = (byte)i;

        using MemoryStream memoryStream = new();
        string fileName = Path.GetTempFileName();
        File.Delete(fileName);

        using (var rootStorage = StructuredStorage.Storage.Create(fileName, StructuredStorage.StorageModes.AccessReadWrite | StructuredStorage.StorageModes.ShareExclusive, version == Version.V4))
        {
            IEnumerable<string> entries = rootStorage.EnumerateEntries();
            using StructuredStorage.Stream stream = rootStorage.CreateStream("TestStream");

            stream.Write(expectedBuffer, 0, expectedBuffer.Length);
            Assert.AreEqual(length, stream.Length);
        }

        byte[] actualBuffer = new byte[length];
        using (var rootStorage = RootStorage.OpenRead(fileName))
        {
            using CfbStream stream = rootStorage.OpenStream("TestStream");
            rootStorage.Validate();
            Assert.AreEqual(length, stream.Length);

            stream.ReadExactly(actualBuffer);
            CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
        }
    }
#endif

    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 256)]
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
    public void WriteMultiple(Version version, int length)
    {
        const int IterationCount = 2048;
        using MemoryStream memoryStream = new();
        using var rootStorage = RootStorage.Create(memoryStream, version);
        using CfbStream stream = rootStorage.CreateStream("TestStream");
        Assert.AreEqual(0, stream.Length);

        // Fill with bytes equal to their position modulo 256
        byte[] expectedBuffer = new byte[length];
        for (int i = 0; i < length; i++)
            expectedBuffer[i] = (byte)i;

        for (int i = 0; i < IterationCount; i++)
        {
            stream.Write(expectedBuffer, 0, expectedBuffer.Length);
            Assert.AreEqual(length * (i + 1), stream.Length);
        }

        stream.Flush();

        byte[] actualBuffer = new byte[length];
        stream.Position = 0;
        for (int i = 0; i < IterationCount; i++)
        {
            actualBuffer.AsSpan().Clear();
            stream.ReadExactly(actualBuffer);
            CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
        }
    }

    [TestMethod]
    [DataRow(Version.V3, 32)]
    [DataRow(Version.V4, 256)]
    public void CreateMultipleStreams(Version version, int streamCount)
    {
        using var rootStorage = RootStorage.CreateInMemory(version);
        for (int i = 0; i < streamCount; i++)
        {
            Assert.AreEqual(i, rootStorage.EnumerateEntries().Count());

            string streamName = $"TestStream{i}";
            using Stream stream = rootStorage.CreateStream(streamName);
        }
    }

    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 63)]
    [DataRow(Version.V3, 64)] // Mini-stream sector size
    [DataRow(Version.V3, 65)]
    [DataRow(Version.V3, 511)]
    [DataRow(Version.V3, 512)] // Multiple stream sectors
    [DataRow(Version.V3, 513)]
    [DataRow(Version.V3, 4095)]
    [DataRow(Version.V3, 4096)]
    [DataRow(Version.V3, 4097)]
    [DataRow(Version.V3, 128 * 512)] // Multiple FAT sectors
    [DataRow(Version.V3, 1024 * 4096)] // Multiple FAT sectors
    [DataRow(Version.V3, 7087616)] // First DIFAT chain
    [DataRow(Version.V3, 2 * 7087616)] // Long DIFAT chain
    [DataRow(Version.V4, 0)]
    [DataRow(Version.V4, 63)]
    [DataRow(Version.V4, 64)] // Mini-stream sector size
    [DataRow(Version.V4, 65)]
    [DataRow(Version.V4, 511)]
    [DataRow(Version.V4, 512)]
    [DataRow(Version.V4, 513)]
    [DataRow(Version.V4, 4095)]
    [DataRow(Version.V4, 4096)] // Multiple stream sectors
    [DataRow(Version.V4, 4097)]
    [DataRow(Version.V4, 1024 * 4096)] // Multiple FAT sectors (1024 * 4096)
    [DataRow(Version.V4, 7087616 * 4)] // First DIFAT chain
    [DataRow(Version.V4, 2 * 7087616 * 4)] // Long DIFAT chain
    public void Modify(Version version, int length)
    {
        // Fill with bytes equal to their position modulo 256
        byte[] expectedBuffer = new byte[length];
        for (int i = 0; i < length; i++)
            expectedBuffer[i] = (byte)i;

        using MemoryStream memoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            using CfbStream stream = rootStorage.CreateStream("TestStream1");
            Assert.AreEqual(0, stream.Length);

            stream.Write(expectedBuffer, 0, expectedBuffer.Length);
        }

        using (var rootStorage = RootStorage.Open(memoryStream, StorageModeFlags.LeaveOpen))
        {
            using CfbStream stream = rootStorage.CreateStream("TestStream2");
            Assert.AreEqual(0, stream.Length);

            stream.Write(expectedBuffer, 0, expectedBuffer.Length);
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
        {
            rootStorage.Validate();

            using (CfbStream stream = rootStorage.OpenStream("TestStream1"))
            {
                Assert.AreEqual(length, stream.Length);

                byte[] actualBuffer = new byte[length];
                stream.ReadExactly(actualBuffer);
                CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
            }

            using (CfbStream stream = rootStorage.OpenStream("TestStream2"))
            {
                Assert.AreEqual(length, stream.Length);

                byte[] actualBuffer = new byte[length];
                stream.ReadExactly(actualBuffer);
                CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
            }
        }
    }

    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 63)]
    [DataRow(Version.V3, 64)] // Mini-stream sector size
    [DataRow(Version.V3, 65)]
    [DataRow(Version.V3, 511)]
    [DataRow(Version.V3, 512)] // Multiple stream sectors
    [DataRow(Version.V3, 513)]
    [DataRow(Version.V3, 4095)]
    [DataRow(Version.V3, 4096)]
    [DataRow(Version.V3, 4097)]
    [DataRow(Version.V3, 128 * 512)] // Multiple FAT sectors
    [DataRow(Version.V3, 1024 * 4096)] // Multiple FAT sectors
    [DataRow(Version.V3, 7087616)] // First DIFAT chain
    [DataRow(Version.V3, 2 * 7087616)] // Long DIFAT chain
    [DataRow(Version.V4, 0)]
    [DataRow(Version.V4, 63)]
    [DataRow(Version.V4, 64)] // Mini-stream sector size
    [DataRow(Version.V4, 65)]
    [DataRow(Version.V4, 511)]
    [DataRow(Version.V4, 512)]
    [DataRow(Version.V4, 513)]
    [DataRow(Version.V4, 4095)]
    [DataRow(Version.V4, 4096)] // Multiple stream sectors
    [DataRow(Version.V4, 4097)]
    [DataRow(Version.V4, 1024 * 4096)] // Multiple FAT sectors (1024 * 4096)
    [DataRow(Version.V4, 7087616 * 4)] // First DIFAT chain
    [DataRow(Version.V4, 2 * 7087616 * 4)] // Long DIFAT chain
    public void ModifyCommit(Version version, int length) => ModifyCommit(version, length, false);

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 63)]
    [DataRow(Version.V3, 64)] // Mini-stream sector size
    [DataRow(Version.V3, 65)]
    [DataRow(Version.V3, 511)]
    [DataRow(Version.V3, 512)] // Multiple stream sectors
    [DataRow(Version.V3, 513)]
    [DataRow(Version.V3, 4095)]
    [DataRow(Version.V3, 4096)]
    [DataRow(Version.V3, 4097)]
    [DataRow(Version.V3, 128 * 512)] // Multiple FAT sectors
    [DataRow(Version.V3, 1024 * 4096)] // Multiple FAT sectors
    [DataRow(Version.V3, 7087616)] // First DIFAT chain
    [DataRow(Version.V3, 2 * 7087616)] // Long DIFAT chain
    [DataRow(Version.V4, 0)]
    [DataRow(Version.V4, 63)]
    [DataRow(Version.V4, 64)] // Mini-stream sector size
    [DataRow(Version.V4, 65)]
    [DataRow(Version.V4, 511)]
    [DataRow(Version.V4, 512)]
    [DataRow(Version.V4, 513)]
    [DataRow(Version.V4, 4095)]
    [DataRow(Version.V4, 4096)] // Multiple stream sectors
    [DataRow(Version.V4, 4097)]
    [DataRow(Version.V4, 1024 * 4096)] // Multiple FAT sectors (1024 * 4096)
    [DataRow(Version.V4, 7087616 * 4)] // First DIFAT chain
    [DataRow(Version.V4, 2 * 7087616 * 4)] // Long DIFAT chain
    public void ModifyCommitSpan(Version version, int length) => ModifyCommit(version, length, true);
#endif

    void ModifyCommit(Version version, int length, bool preferSpan)
    {
        // Fill with bytes equal to their position modulo 256
        byte[] expectedBuffer = new byte[length];
        for (int i = 0; i < length; i++)
            expectedBuffer[i] = (byte)i;

        using MemoryStream memoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            using CfbStream stream = rootStorage.CreateStream("TestStream1");
            Assert.AreEqual(0, stream.Length);

            stream.Write(expectedBuffer, 0, expectedBuffer.Length);
        }

        using (var rootStorage = RootStorage.Open(memoryStream, StorageModeFlags.LeaveOpen | StorageModeFlags.Transacted))
        {
            using (CfbStream stream = rootStorage.CreateStream("TestStream2"))
            {
                Assert.AreEqual(0, stream.Length);

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
                if (preferSpan)
                    stream.Write(expectedBuffer);
                else
                    stream.Write(expectedBuffer, 0, expectedBuffer.Length);
#else
                stream.Write(expectedBuffer, 0, expectedBuffer.Length);
#endif
            }

            rootStorage.Commit();
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
        {
            rootStorage.Validate();

            using (CfbStream stream = rootStorage.OpenStream("TestStream1"))
            {
                Assert.AreEqual(length, stream.Length);

                byte[] actualBuffer = new byte[length];
                stream.ReadExactly(actualBuffer);
                CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
            }

            using (CfbStream stream = rootStorage.OpenStream("TestStream2"))
            {
                Assert.AreEqual(length, stream.Length);

                byte[] actualBuffer = new byte[length];
                stream.ReadExactly(actualBuffer);
                CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
            }
        }
    }

    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 63)]
    [DataRow(Version.V3, 64)] // Mini-stream sector size
    [DataRow(Version.V3, 65)]
    [DataRow(Version.V3, 511)]
    [DataRow(Version.V3, 512)] // Multiple stream sectors
    [DataRow(Version.V3, 513)]
    [DataRow(Version.V3, 4095)]
    [DataRow(Version.V3, 4096)]
    [DataRow(Version.V3, 4097)]
    [DataRow(Version.V3, 128 * 512)] // Multiple FAT sectors
    [DataRow(Version.V3, 1024 * 4096)] // Multiple FAT sectors
    [DataRow(Version.V3, 7087616)] // First DIFAT chain
    [DataRow(Version.V3, 2 * 7087616)] // Long DIFAT chain
    [DataRow(Version.V4, 0)]
    [DataRow(Version.V4, 63)]
    [DataRow(Version.V4, 64)] // Mini-stream sector size
    [DataRow(Version.V4, 65)]
    [DataRow(Version.V4, 511)]
    [DataRow(Version.V4, 512)]
    [DataRow(Version.V4, 513)]
    [DataRow(Version.V4, 4095)]
    [DataRow(Version.V4, 4096)] // Multiple stream sectors
    [DataRow(Version.V4, 4097)]
    [DataRow(Version.V4, 1024 * 4096)] // Multiple FAT sectors (1024 * 4096)
    [DataRow(Version.V4, 7087616 * 4)] // First DIFAT chain
    [DataRow(Version.V4, 2 * 7087616 * 4)] // Long DIFAT chain
    public void TransactedRead(Version version, int length)
    {
        // Fill with bytes equal to their position modulo 256
        byte[] expectedBuffer = new byte[length];
        for (int i = 0; i < length; i++)
            expectedBuffer[i] = (byte)i;

        using MemoryStream memoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            using CfbStream stream = rootStorage.CreateStream("TestStream1");
            Assert.AreEqual(0, stream.Length);

            stream.Write(expectedBuffer, 0, expectedBuffer.Length);
        }

        using (var rootStorage = RootStorage.Open(memoryStream, StorageModeFlags.Transacted))
        {
            using CfbStream stream = rootStorage.OpenStream("TestStream1");
            byte[] actualBuffer = new byte[length];
            stream.ReadExactly(actualBuffer);
            CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
        }
    }

    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 63)]
    [DataRow(Version.V3, 64)] // Mini-stream sector size
    [DataRow(Version.V3, 65)]
    [DataRow(Version.V3, 511)]
    [DataRow(Version.V3, 512)] // Multiple stream sectors
    [DataRow(Version.V3, 513)]
    [DataRow(Version.V3, 4095)]
    [DataRow(Version.V3, 4096)]
    [DataRow(Version.V3, 4097)]
    [DataRow(Version.V3, 128 * 512)] // Multiple FAT sectors
    [DataRow(Version.V3, 1024 * 4096)] // Multiple FAT sectors
    [DataRow(Version.V3, 7087616)] // First DIFAT chain
    [DataRow(Version.V3, 2 * 7087616)] // Long DIFAT chain
    [DataRow(Version.V4, 0)]
    [DataRow(Version.V4, 63)]
    [DataRow(Version.V4, 64)] // Mini-stream sector size
    [DataRow(Version.V4, 65)]
    [DataRow(Version.V4, 511)]
    [DataRow(Version.V4, 512)]
    [DataRow(Version.V4, 513)]
    [DataRow(Version.V4, 4095)]
    [DataRow(Version.V4, 4096)] // Multiple stream sectors
    [DataRow(Version.V4, 4097)]
    [DataRow(Version.V4, 1024 * 4096)] // Multiple FAT sectors (1024 * 4096)
    [DataRow(Version.V4, 7087616 * 4)] // First DIFAT chain
    [DataRow(Version.V4, 2 * 7087616 * 4)] // Long DIFAT chain
    public void ModifyRevert(Version version, int length)
    {
        // Fill with bytes equal to their position modulo 256
        byte[] expectedBuffer = new byte[length];
        for (int i = 0; i < length; i++)
            expectedBuffer[i] = (byte)i;

        using MemoryStream memoryStream = new();
        using (var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.LeaveOpen))
        {
            using CfbStream stream = rootStorage.CreateStream("TestStream1");
            Assert.AreEqual(0, stream.Length);

            stream.Write(expectedBuffer, 0, expectedBuffer.Length);
        }

        using (var rootStorage = RootStorage.Open(memoryStream, StorageModeFlags.LeaveOpen | StorageModeFlags.Transacted))
        {
            using CfbStream stream = rootStorage.CreateStream("TestStream2");
            Assert.AreEqual(0, stream.Length);

            stream.Write(expectedBuffer, 0, expectedBuffer.Length);
            rootStorage.Revert();
        }

        using (var rootStorage = RootStorage.Open(memoryStream))
        {
            rootStorage.Validate();

            using (CfbStream stream = rootStorage.OpenStream("TestStream1"))
            {
                Assert.AreEqual(length, stream.Length);

                byte[] actualBuffer = new byte[length];
                stream.ReadExactly(actualBuffer);
                CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
            }

            Assert.ThrowsException<FileNotFoundException>(() => rootStorage.OpenStream("TestStream2"));
        }
    }

    [TestMethod]
    [DataRow(Version.V3, 0)]
    [DataRow(Version.V3, 63)]
    [DataRow(Version.V3, 64)] // Mini-stream sector size
    [DataRow(Version.V3, 2 * 64)] // Simplest case (1 sector => 2)
    [DataRow(Version.V3, 65)]
    [DataRow(Version.V3, 511)]
    [DataRow(Version.V3, 512)] // Multiple stream sectors
    [DataRow(Version.V3, 513)]
    [DataRow(Version.V3, 4095)]
    [DataRow(Version.V3, 4096)]
    [DataRow(Version.V3, 4097)]
    [DataRow(Version.V3, 128 * 512)] // Multiple FAT sectors
    [DataRow(Version.V3, 1024 * 4096)] // Multiple FAT sectors
    [DataRow(Version.V3, 7087616)] // First DIFAT chain
    [DataRow(Version.V3, 2 * 7087616)] // Long DIFAT chain
    [DataRow(Version.V4, 0)]
    [DataRow(Version.V4, 63)]
    [DataRow(Version.V4, 64)] // Mini-stream sector size
    [DataRow(Version.V4, 65)]
    [DataRow(Version.V4, 511)]
    [DataRow(Version.V4, 512)]
    [DataRow(Version.V4, 513)]
    [DataRow(Version.V4, 4095)]
    [DataRow(Version.V4, 4096)] // Multiple stream sectors
    [DataRow(Version.V4, 2 * 4096)] // Simplest case (1 sector => 2)
    [DataRow(Version.V4, 4097)]
    [DataRow(Version.V4, 1024 * 4096)] // Multiple FAT sectors (1024 * 4096)
    [DataRow(Version.V4, 7087616 * 4)] // First DIFAT chain
    [DataRow(Version.V4, 2 * 7087616 * 4)] // Long DIFAT chain
    public void Shrink(Version version, int length)
    {
        using MemoryStream memoryStream = new();
        using var rootStorage = RootStorage.Create(memoryStream, version);
        using CfbStream stream = rootStorage.CreateStream("Test");

        // Fill with bytes equal to their position modulo 256
        byte[] expectedBuffer = new byte[length];
        for (int i = 0; i < length; i++)
            expectedBuffer[i] = (byte)i;

        stream.Write(expectedBuffer, 0, expectedBuffer.Length);
        Assert.AreEqual(length, stream.Length);

        long baseStreamLength = memoryStream.Length;

        int newLength = length / 2;
        stream.SetLength(newLength);
        Assert.AreEqual(newLength, stream.Length);

        stream.Position = newLength;
        stream.Write(expectedBuffer, newLength, expectedBuffer.Length - newLength);
        Assert.AreEqual(length, stream.Length);

        byte[] actualBuffer = new byte[length];
        stream.Seek(0, SeekOrigin.Begin);
        stream.ReadExactly(actualBuffer);

        CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void MiniFatToFat(Version version)
    {
        using MemoryStream memoryStream = new();
        using var rootStorage = RootStorage.Create(memoryStream, version);
        using CfbStream stream = rootStorage.CreateStream("Test");

        int length = 256;
        // Fill with bytes equal to their position modulo 256
        byte[] expectedBuffer = new byte[length];
        for (int i = 0; i < length; i++)
            expectedBuffer[i] = (byte)i;

        int iterations = (int)Header.MiniStreamCutoffSize / length;
        for (int i = 0; i < iterations; i++)
            stream.Write(expectedBuffer, 0, expectedBuffer.Length);

        Assert.AreEqual(length * iterations, stream.Length);

        byte[] actualBuffer = new byte[length];
        stream.Position = 0;
        for (int i = 0; i < iterations; i++)
        {
            actualBuffer.AsSpan().Clear();
            stream.ReadExactly(actualBuffer);
            CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
        }
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void FatToMiniFat(Version version)
    {
        const int length = 256;

        using MemoryStream memoryStream = new();
        using var rootStorage = RootStorage.Create(memoryStream, version);
        using CfbStream stream = rootStorage.CreateStream("Test");

        // Fill with bytes equal to their position modulo 256
        byte[] expectedBuffer = new byte[length];
        for (int i = 0; i < length; i++)
            expectedBuffer[i] = (byte)i;

        int iterations = (int)Header.MiniStreamCutoffSize / length;
        for (int i = 0; i < iterations; i++)
            stream.Write(expectedBuffer, 0, expectedBuffer.Length);

        Assert.AreEqual(length * iterations, stream.Length);

        byte[] actualBuffer = new byte[length];

        // Check reading from the regular sectors
        stream.Position = 0;
        for (int i = 0; i < iterations; i++)
        {
            actualBuffer.AsSpan().Clear();
            stream.ReadExactly(actualBuffer);
            CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
        }

        stream.SetLength(length);
        Assert.AreEqual(length, stream.Length);

        stream.Position = 0;
        stream.ReadExactly(actualBuffer);
        CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
    }

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
    public void CopyFromStream(Version version, int length)
    {
        using MemoryStream memoryStream = new();
        using var rootStorage = RootStorage.Create(memoryStream, version);
        using CfbStream stream = rootStorage.CreateStream("TestStream");
        Assert.AreEqual(0, stream.Length);

        // Fill with bytes equal to their position modulo 256
        using MemoryStream expectedStream = new(length);
        for (int i = 0; i < length; i++)
            expectedStream.WriteByte((byte)i);

        expectedStream.Position = 0;
        expectedStream.CopyTo(stream);
        Assert.AreEqual(length, stream.Length);

        using MemoryStream actualStream = new();
        stream.Seek(0, SeekOrigin.Begin);
        stream.CopyTo(actualStream);

        StreamAssert.AreEqual(expectedStream, actualStream);
    }

    [TestMethod]
    [DataRow(Version.V3)]
    [DataRow(Version.V4)]
    public void CreateDuplicateStreamThrowsException(Version version)
    {
        using MemoryStream memoryStream = new();
        using var rootStorage = RootStorage.Create(memoryStream, version);
        using CfbStream stream = rootStorage.CreateStream("Test");
        Assert.ThrowsException<IOException>(() => rootStorage.CreateStream("Test"));
    }
}
