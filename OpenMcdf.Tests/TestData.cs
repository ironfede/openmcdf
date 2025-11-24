namespace OpenMcdf.Tests;

public static class TestData
{
    /// <summary>
    /// Fill with bytes equal to their position modulo 256.
    /// </summary>
    public static byte[] CreateByteArray(int length)
    {
        byte[] expectedBuffer = new byte[length];
        for (int i = 0; i < length; i++)
            expectedBuffer[i] = (byte)i;
        return expectedBuffer;
    }

    public static MemoryStream CreateMemoryStreamFromFile(string fileName)
    {
        using FileStream fs = File.OpenRead(fileName);
        MemoryStream ms = new((int)fs.Length);
        fs.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }

    public static IEnumerable<object[]> ShortVersionsAndSizes { get; } = new[]
    {
        new object[] { Version.V3, 0 },
        [Version.V3, 63],
        [Version.V3, 64], // Mini-stream sector size
        [Version.V3, 65],
        [Version.V3, 511],
        [Version.V3, 512], // Multiple stream sectors
        [Version.V3, 513],
        [Version.V3, 4095],
        [Version.V3, 4096],
        [Version.V3, 4097],
        [Version.V4, 0],
        [Version.V4, 63],
        [Version.V4, 64], // Mini-stream sector size
        [Version.V4, 65],
        [Version.V4, 511],
        [Version.V4, 512],
        [Version.V4, 513],
        [Version.V4, 4095],
        [Version.V4, 4096], // Multiple stream sectors
        [Version.V4, 4097],
    };

    public static IEnumerable<object[]> VersionsAndSizes { get; } = new[]
    {
        new object[] { Version.V3, 0 },
        [Version.V3, 63],
        [Version.V3, 64], // Mini-stream sector size
        [Version.V3, 65],
        [Version.V3, 511],
        [Version.V3, 512], // Multiple stream sectors
        [Version.V3, 513],
        [Version.V3, 4095],
        [Version.V3, 4096],
        [Version.V3, 4097],
        [Version.V3, 128 * 512], // Multiple FAT sectors
        [Version.V3, 1024 * 4096], // Multiple FAT sectors
        [Version.V3, 7087616], // First DIFAT chain
        [Version.V3, 2 * 7087616], // Long DIFAT chain
        [Version.V4, 0],
        [Version.V4, 63],
        [Version.V4, 64], // Mini-stream sector size
        [Version.V4, 65],
        [Version.V4, 511],
        [Version.V4, 512],
        [Version.V4, 513],
        [Version.V4, 4095],
        [Version.V4, 4096], // Multiple stream sectors
        [Version.V4, 4097],
        [Version.V4, 1024 * 4096], // Multiple FAT sectors (1024 * 4096
        [Version.V4, 7087616 * 4], // First DIFAT chain
        [Version.V4, 2 * 7087616 * 4], // Long DIFAT chain
    };
}
