using System.Diagnostics;

namespace OpenMcdf3.Perf;

internal sealed class Program
{
    static void Main(string[] args)
    {
        var stopwatch = Stopwatch.StartNew();
        Write(Version.V3, 512, 2 * 1024);
        Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");
    }

    static void Write(Version version, int length, int iterations)
    {
        // Fill with bytes equal to their position modulo 256
        byte[] expectedBuffer = new byte[length];
        for (int i = 0; i < length; i++)
            expectedBuffer[i] = (byte)i;

        //byte[] actualBuffer = new byte[length];

        using MemoryStream memoryStream = new(2 * length * iterations);
        using var rootStorage = RootStorage.Create(memoryStream, version, StorageModeFlags.Transacted);
        using Stream stream = rootStorage.CreateStream("TestStream");

        for (int i = 0; i < iterations; i++)
        {
            stream.Write(expectedBuffer, 0, expectedBuffer.Length);
        }
    }
}
