namespace OpenMcdf3.Benchmarks;

internal class StructuredStorageBenchmarks
{
    public static void ReadStream(MemoryStream stream, byte[] buffer)
    {
        using var storage = StructuredStorage.Storage.Open(stream);
        using StructuredStorage.Stream storageStream = storage.OpenStream("Test");

        while (storageStream.Position < storageStream.Length)
        {
            int read = stream.Read(buffer, 0, buffer.Length);
            if (read <= 0)
                throw new EndOfStreamException();
        }
    }

    public static void WriteInMemory(Version version, StorageModeFlags flags, byte[] buffer, long streamLength)
    {
        using var storage = StructuredStorage.Storage.CreateInMemory((int)streamLength * 2);
        using StructuredStorage.Stream stream = storage.CreateStream("Test");

        while (stream.Length < streamLength)
            stream.Write(buffer, 0, buffer.Length);
    }
}
