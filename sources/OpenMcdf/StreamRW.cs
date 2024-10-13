/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 * The Original Code is OpenMCDF - Compound Document Format library.
 *
 * The Initial Developer of the Original Code is Federico Blaseotto.*/

using System;
using System.IO;

namespace OpenMcdf
{
    internal sealed class StreamRW
    {
        private readonly byte[] buffer = new byte[16];
        private readonly Stream stream;

        public StreamRW(Stream stream)
        {
            this.stream = stream;
        }

        public long Seek(long count, SeekOrigin origin)
        {
            return stream.Seek(count, origin);
        }

        public byte ReadByte()
        {
            stream.ReadExactly(buffer, 0, sizeof(byte));
            return buffer[0];
        }

        public ushort ReadUInt16()
        {
            stream.ReadExactly(buffer, 0, sizeof(ushort));
            return (ushort)(buffer[0] | (buffer[1] << 8));
        }

        public int ReadInt32()
        {
            stream.ReadExactly(buffer, 0, sizeof(int));
            return buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
        }

        public uint ReadUInt32()
        {
            stream.ReadExactly(buffer, 0, sizeof(uint));
            return (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
        }

        public long ReadInt64()
        {
            stream.ReadExactly(buffer, 0, sizeof(long));
            uint ls = (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
            uint ms = (uint)((buffer[4]) | (buffer[5] << 8) | (buffer[6] << 16) | (buffer[7] << 24));
            return (long)(((ulong)ms << 32) | ls);
        }

        public ulong ReadUInt64()
        {
            stream.ReadExactly(buffer, 0, sizeof(ulong));
            return (ulong)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24) | (buffer[4] << 32) | (buffer[5] << 40) | (buffer[6] << 48) | (buffer[7] << 56));
        }

        public void ReadBytes(byte[] result)
        {
            stream.ReadExactly(result, 0, result.Length);
        }

        public Guid ReadGuid()
        {
            stream.ReadExactly(buffer, 0, 16);
            return new Guid(buffer);
        }

        public void Write(byte b)
        {
            buffer[0] = b;
            stream.Write(buffer, 0, 1);
        }

        public void Write(ushort value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);

            stream.Write(buffer, 0, 2);
        }

        public void Write(int value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);

            stream.Write(buffer, 0, 4);
        }

        public void Write(long value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            buffer[4] = (byte)(value >> 32);
            buffer[5] = (byte)(value >> 40);
            buffer[6] = (byte)(value >> 48);
            buffer[7] = (byte)(value >> 56);

            stream.Write(buffer, 0, 8);
        }

        public void Write(uint value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);

            stream.Write(buffer, 0, 4);
        }

        public void Write(byte[] value)
        {
            stream.Write(value, 0, value.Length);
        }

        public void Write(Guid value)
        {
            var data = value.ToByteArray();
            Write(data);
        }
    }
}
