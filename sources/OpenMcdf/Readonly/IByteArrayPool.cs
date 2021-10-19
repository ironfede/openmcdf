namespace OpenMcdf
{
    public interface IByteArrayPool
    {
        byte[] Rent(int minimumLength);
        void Return(byte[] byteList);
    }
}