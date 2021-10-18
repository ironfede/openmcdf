namespace OpenMcdf
{
    class ByteArrayPool : IByteArrayPool
    {
        public byte[] Rent(int minimumLength)
        {
            for (var i = 0; i < _bufferList.Length; i++)
            {
                var buffer = _bufferList[i];

                if (buffer != null && buffer.Length >= minimumLength)
                {
                    _bufferList[i] = null;
                    return buffer;
                }
            }

            return new byte[minimumLength];
        }

        public void Return(byte[] byteList)
        {
            if (byteList == null || byteList.Length >= MaxBufferLength)
            {
                return;
            }

            for (var i = 0; i < _bufferList.Length; i++)
            {
                var buffer = _bufferList[i];
                if (buffer is null || byteList.Length > buffer.Length)
                {
                    buffer = byteList;
                    _bufferList[i] = buffer;
                }
            }
        }

        private readonly byte[][] _bufferList = new byte[4][];
        private const int MaxBufferLength = 8 * 1024;
    }
}