using System;
using System.IO;

namespace OpenMcdf
{
    class ReadonlyStreamViewForSectorList : Stream, IStreamReader
    {
        public ReadonlyStreamViewForSectorList(SectorList sectorChain, long length, Stream sourceStream,
            IByteArrayPool byteArrayPool)
        {
            _sectorChain = sectorChain;
            _sourceStream = sourceStream;
            _byteArrayPool = byteArrayPool;
            Length = length;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public long Seek(long offset)
        {
            return Seek(offset, SeekOrigin.Begin);
        }

        public T ReadValue<T>(int length, Func<byte[], T> convert)
        {
            var buffer = _byteArrayPool.Rent(length);
            Read(buffer, 0, length);

            var result = convert(buffer);

            _byteArrayPool.Return(buffer);
            return result;
        }

        byte IStreamReader.ReadByte()
        {
            return ReadValue(1, buffer => buffer[0]);
        }

        public ushort ReadUInt16()
        {
            return ReadValue(2, buffer => (ushort)(buffer[0] | (buffer[1] << 8)));
        }

        public int ReadInt32()
        {
            return ReadValue(4, buffer => BitConverter.ToInt32(buffer, 0));
        }

        public uint ReadUInt32()
        {
            return ReadValue(4, buffer => (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24)));
        }

        public long ReadInt64()
        {
            return ReadValue(8, buffer =>
            {
                uint ls = (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
                uint ms = (uint)((buffer[4]) | (buffer[5] << 8) | (buffer[6] << 16) | (buffer[7] << 24));
                return (long)(((ulong)ms << 32) | ls);
            });
        }

        public ulong ReadUInt64()
        {
            return ReadValue(8, buffer => BitConverter.ToUInt64(buffer, 0));
        }

        public byte[] ReadBytes(int count)
        {
            byte[] result = new byte[count];
            Read(result, 0, count);
            return result;
        }

        public byte[] ReadBytes(int count, out int readCount)
        {
            byte[] result = new byte[count];
            readCount = Read(result, 0, count);
            return result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int readCount = 0;
            var sectorChain = _sectorChain;
            if (sectorChain == null || sectorChain.Count <= 0)
            {
                return 0;
            }

            var sectorSize = sectorChain.SectorSize;
            // First sector
            int sectorIndex = (int)(Position / sectorSize);

            // Bytes to read count is the min between request count
            // and sector border

            var needToReadCount = Math.Min(sectorChain.SectorSize - ((int)Position % sectorSize),
                count);
            if (sectorIndex < sectorChain.Count)
            {
                var readPosition = (int)(Position % sectorSize);

                sectorChain.Read(sectorIndex, buffer, readPosition, offset, needToReadCount);
            }

            readCount += needToReadCount;
            sectorIndex++;
            // Central sectors
            while (readCount < (count - sectorSize))
            {
                needToReadCount = sectorSize;
                var readPosition = 0;
                sectorChain.Read(sectorIndex, buffer, readPosition, offset + readCount, needToReadCount);

                readCount += needToReadCount;
                sectorIndex++;
            }

            // Last sector
            needToReadCount = count - readCount;
            if (needToReadCount != 0)
            {
                if (sectorIndex > sectorChain.Count) throw new CFCorruptedFileException("The file is probably corrupted.");
                var readPosition = 0;
                readCount += sectorChain.Read(sectorIndex, buffer, readPosition, offset + readCount, needToReadCount);
            }

            Position += readCount;
            return readCount;
        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                Position = offset;
                return Position;
            }

            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length { get; }

        public override long Position { set; get; }

        private readonly SectorList _sectorChain;
        private readonly Stream _sourceStream;
        private readonly IByteArrayPool _byteArrayPool;
    }
}