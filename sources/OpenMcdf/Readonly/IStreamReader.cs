namespace OpenMcdf
{
    internal interface IStreamReader
    {
        long Seek(long offset);
        byte ReadByte();
        ushort ReadUInt16();
        int ReadInt32();
        uint ReadUInt32();
        long ReadInt64();
        ulong ReadUInt64();
        byte[] ReadBytes(int count);
        byte[] ReadBytes(int count, out int readCount);
        void Close();
    }
}