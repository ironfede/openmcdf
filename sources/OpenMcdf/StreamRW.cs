/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. 
 * 
 * The Original Code is OpenMCDF - Compound Document Format library.
 * 
 * The Initial Developer of the Original Code is Federico Blaseotto.*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OpenMcdf
{
    internal class StreamRW
    {
        private byte[] buffer = new byte[8];
        private Stream stream;

        public StreamRW(Stream stream)
        {

            this.stream = stream;
        }

        public long Seek(long offset)
        {
            return stream.Seek(offset, SeekOrigin.Begin);
        }

        public byte ReadByte()
        {
            this.stream.Read(buffer, 0, 1);
            return buffer[0];
        }

        public ushort ReadUInt16()
        {
            this.stream.Read(buffer, 0, 2);
            return (ushort)(buffer[0] | (buffer[1] << 8));
        }

        public int ReadInt32()
        {
            this.stream.Read(buffer, 0, 4);
            return (int)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
        }

        public uint ReadUInt32()
        {
            this.stream.Read(buffer, 0, 4);
            return (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
        }

        public long ReadInt64()
        {
            this.stream.Read(buffer, 0, 8);
            uint ls = (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
            uint ms = (uint)((buffer[4]) | (buffer[5] << 8) | (buffer[6] << 16) | (buffer[7] << 24));
            return (long)(((ulong)ms << 32) | ls);
        }

        public ulong ReadUInt64()
        {
            this.stream.Read(buffer, 0, 8);
            return (ulong)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24) | (buffer[4] << 32) | (buffer[5] << 40) | (buffer[6] << 48) | (buffer[7] << 56));
        }

        public byte[] ReadBytes(int count)
        {
            byte[] result = new byte[count];
            this.stream.Read(result, 0, count);
            return result;
        }

        public byte[] ReadBytes(int count, out int r_count)
        {
            byte[] result = new byte[count];
            r_count = this.stream.Read(result, 0, count);
            return result;
        }

        public void Write(byte b)
        {
            buffer[0] = b;
            this.stream.Write(buffer, 0, 1);
        }

        public void Write(ushort value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);

            this.stream.Write(buffer, 0, 2);
        }

        public void Write(int value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);

            this.stream.Write(buffer, 0, 4);
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

            this.stream.Write(buffer, 0, 8);
        }

        public void Write(uint value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);

            this.stream.Write(buffer, 0, 4);
        }

        public void Write(byte[] value)
        {
            this.stream.Write(value, 0, value.Length);
        }

        public void Close()
        {
            //Nothing to do ;-)
        }
    }
}
