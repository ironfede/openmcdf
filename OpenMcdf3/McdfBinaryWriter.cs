namespace OpenMcdf3;

internal class McdfBinaryWriter : BinaryWriter
{
    public McdfBinaryWriter(Stream input) : base(input)
    {
    }

    public void Write(Guid value)
    {
        // TODO: Avoid heap allocation
        byte[] bytes = value.ToByteArray();
        Write(bytes, 0, bytes.Length);
    }

    public void Write(DateTime value)
    {
        long fileTime = value.ToFileTimeUtc();
        Write(fileTime);
    }
}
