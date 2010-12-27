﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;
using System.Collections.Specialized;
using System.Diagnostics;

/*
     The contents of this file are subject to the Mozilla Public License
     Version 1.1 (the "License"); you may not use this file except in
     compliance with the License. You may obtain a copy of the License at
     http://www.mozilla.org/MPL/

     Software distributed under the License is distributed on an "AS IS"
     basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
     License for the specific language governing rights and limitations
     under the License.

     The Original Code is OpenMCDF - Compound Document Format library.

     The Initial Developer of the Original Code is Federico Blaseotto.
*/

namespace OleCompoundFileStorage
{
    /// <summary>
    /// Stream decorator for a Sector or miniSector chain
    /// </summary>
    internal class StreamView : Stream
    {
        private int sectorSize;

        private long position;

        private List<Sector> sectorChain;

        public StreamView(List<Sector> sectorChain, int sectorSize)
        {
            if (sectorChain == null)
                throw new CFException("Sector Chain cannot be null");

            if (sectorSize <= 0)
                throw new CFException("Sector size must be greater than zero");

            this.sectorChain = sectorChain;
            this.sectorSize = sectorSize;
        }

        public StreamView(List<Sector> sectorChain, int sectorSize, long length, Stack<Sector> availableSectors)
            : this(sectorChain, sectorSize)
        {
            adjustLength(length, availableSectors);
        }


        public StreamView(List<Sector> sectorChain, int sectorSize, long length)
            : this(sectorChain, sectorSize)
        {
            adjustLength(length);
        }

        public List<Sector> BaseSectorChain
        {
            get { return sectorChain; }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {

        }

        private long length;

        public override long Length
        {
            get
            {
                return length;
            }
        }

        public override long Position
        {
            get
            {
                return position;
            }

            set
            {
                if (position > length - 1)
                    throw new ArgumentOutOfRangeException("value");

                position = value;
            }
        }

        public override void Close()
        {
            base.Close();
        }

        private byte[] buf = new byte[4];

        public int ReadInt32()
        {
            this.Read(buf, 0, 4);
            return  (((this.buf[0] | (this.buf[1] << 8)) | (this.buf[2] << 16)) | (this.buf[3] << 24));
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int nRead = 0;
            int nToRead = 0;

            if (sectorChain != null && sectorChain.Count > 0)
            {
                // First sector
                int secIndex = (int)position / sectorSize;

                // Bytes to read count is the min between request count
                // and sector border

                nToRead = Math.Min(
                    sectorChain[0].Size - ((int)position % sectorSize),
                    count);

                if (secIndex < sectorChain.Count)
                {
                    Buffer.BlockCopy(
                        sectorChain[secIndex].Data,
                        (int)(position % sectorSize),
                        buffer,
                        offset,
                        nToRead
                        );
                }

                nRead += nToRead;

                secIndex++;

                // Central sectors
                while (nRead < (count - sectorSize))
                {
                    nToRead = sectorSize;

                    Buffer.BlockCopy(
                        sectorChain[secIndex].Data,
                        0,
                        buffer,
                        offset + nRead,
                        nToRead
                        );

                    nRead += nToRead;
                    secIndex++;
                }

                // Last sector
                nToRead = count - nRead;

                if (nToRead != 0)
                {
                    Buffer.BlockCopy(
                        sectorChain[secIndex].Data,
                        0,
                        buffer,
                        offset + nRead,
                        nToRead
                        );

                    nRead += nToRead;
                }

                position += nRead;

                return nRead;

            }
            else
                return 0;

        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    position = offset;
                    break;
                case SeekOrigin.Current:
                    position += offset;
                    break;
                case SeekOrigin.End:
                    position = Length - offset;
                    break;
            }

            return position;
        }

        private void adjustLength(long value)
        {
            adjustLength(value, null);
        }

        private void adjustLength(long value, Stack<Sector> availableSectors)
        {
            this.length = value;

            long delta = value - (this.sectorChain.Count * sectorSize);

            if (delta > 0)
            {
                // enlargment required

                int nSec = (int)Math.Ceiling(((double)delta / sectorSize));

                while (nSec > 0)
                {
                    Sector t = null;

                    if (availableSectors == null || availableSectors.Count == 0)
                    {
                         t = new Sector(sectorSize);
                    }
                    else
                    {
                        t = availableSectors.Pop();
                    }

                    sectorChain.Add(t);
                    nSec--;
                }

                //if (((int)delta % sectorSize) != 0)
                //{
                //    Sector t = new Sector(sectorSize);
                //    sectorChain.Add(t);
                //}
            }
            else
            {
                // TODO: Freeing sector to avoid wasting space.

                // FREE Sectors
                //delta = Math.Abs(delta);
                //int nSec = (int)(length - delta)  / sectorSize;

                //if (((int)(length - delta) % sectorSize) != 0)
                //{
                //    nSec++;
                //}

                //while (sectorChain.Count > nSec)
                //{
                //    sectorChain.RemoveAt(sectorChain.Count - 1);
                //}
            }
        }

        public override void SetLength(long value)
        {
            adjustLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int byteWritten = 0;
            int roundByteWritten = 0;

            // Assure length
            if ((position + count) > length)
                SetLength((position + count));

            if (sectorChain != null)
            {
                // First sector
                int secOffset = (int)position / sectorSize;
                int secShift = (int)position % sectorSize;

                roundByteWritten = Math.Min(sectorSize - ((int)position % sectorSize), count);

                if (secOffset < sectorChain.Count)
                {
                    Buffer.BlockCopy(
                        buffer,
                        offset,
                        sectorChain[secOffset].Data,
                        secShift,
                        roundByteWritten
                        );

                    sectorChain[secOffset].DirtyFlag = true;
                }

                byteWritten += roundByteWritten;
                offset += roundByteWritten;
                secOffset++;

                // Central sectors
                while (byteWritten < (count - sectorSize))
                {
                    roundByteWritten = sectorSize;

                    Buffer.BlockCopy(
                        buffer,
                        offset,
                        sectorChain[secOffset].Data,
                        0,
                        roundByteWritten
                        );

                    sectorChain[secOffset].DirtyFlag = true;

                    byteWritten += roundByteWritten;
                    offset += roundByteWritten;
                    secOffset++;
                }

                // Last sector
                roundByteWritten = count - byteWritten;

                if (roundByteWritten != 0)
                {
                    Buffer.BlockCopy(
                        buffer,
                        offset,
                        sectorChain[secOffset].Data,
                        0,
                        roundByteWritten
                        );

                    sectorChain[secOffset].DirtyFlag = true;

                    offset += roundByteWritten;
                    byteWritten += roundByteWritten;
                }

                position += count;

            }
        }
    }
}