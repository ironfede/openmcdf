namespace OpenMcdf3.Benchmarks;

internal static class OpenMcdfBenchmarks
{
    public static void ReadStream(Stream stream, byte[] buffer)
    {
        using var storage = RootStorage.Open(stream, StorageModeFlags.LeaveOpen);
        using CfbStream cfbStream = storage.OpenStream("Test");
        while (cfbStream.Position < cfbStream.Length)
        {
            int read = cfbStream.Read(buffer, 0, buffer.Length);
            if (read <= 0)
                throw new EndOfStreamException();
        }
    }

    public static void WriteStream(Stream stream, Version version, StorageModeFlags flags, byte[] buffer, long streamLength)
    {
        using var storage = RootStorage.Create(stream, version, flags);
        using CfbStream cfbStream = storage.CreateStream("Test");

        while (stream.Length < streamLength)
            stream.Write(buffer, 0, buffer.Length);
    }
}
