namespace OpenMcdf3;

internal class McdfBinaryReader : BinaryReader
{
    public McdfBinaryReader(Stream input) : base(input)
    {
    }

    public Guid ReadGuid() => new(ReadBytes(16));

    public DateTime ReadFileTime()
    {
        long fileTime = ReadInt64();
        return DateTime.FromFileTimeUtc(fileTime);
    }
}
