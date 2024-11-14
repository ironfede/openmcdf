using System.Diagnostics;

namespace OpenMcdf.Perf;

internal sealed class Program
{
    static void Main(string[] args)
    {
        var stopwatch = Stopwatch.StartNew();
        Write(Version.V3, StorageModeFlags.Transacted, 1024 * 1024, 1024 * 1024, 100);
        Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");
    }

    static void Write(Version version, StorageModeFlags storageModeFlags, int bufferLength, int streamLength, int iterations)
    {
        // Fill with bytes equal to their position modulo 256
        byte[] expectedBuffer = new byte[bufferLength];
        for (int i = 0; i < bufferLength; i++)
            expectedBuffer[i] = (byte)i;

        //byte[] actualBuffer = new byte[length];

        //using MemoryStream memoryStream = new(2 * length * iterations);
        using FileStream baseStream = File.Create(Path.GetTempFileName());
        for (int i = 0; i < iterations; i++)
        {
            using var rootStorage = RootStorage.Create(baseStream, version, storageModeFlags);
            using Stream stream = rootStorage.CreateStream("TestStream");

            for (int j = 0; j < streamLength / bufferLength; j++)
                stream.Write(expectedBuffer, 0, expectedBuffer.Length);

            if (storageModeFlags.HasFlag(StorageModeFlags.Transacted))
                rootStorage.Commit();
        }
    }
}
