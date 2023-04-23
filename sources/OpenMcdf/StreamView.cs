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
using System.Collections;
using System.IO;
using System.Collections.Specialized;
using System.Diagnostics;

namespace OpenMcdf
{
    /// <summary>
    /// Stream decorator for a Sector or miniSector chain
    /// </summary>
    internal class StreamView : Stream
    {
        private int sectorSize;

        private long position;

        private List<Sector> sectorChain;
        private Stream stream;
        private bool isFatStream = false;
        private List<Sector> freeSectors = new List<Sector>();
        public IEnumerable<Sector> FreeSectors
        {
            get { return freeSectors; }
        }

        public StreamView(List<Sector> sectorChain, int sectorSize, Stream stream)
        {
            if (sectorChain == null)
                throw new CFException("Sector Chain cannot be null");

            if (sectorSize <= 0)
                throw new CFException("Sector size must be greater than zero");

            this.sectorChain = sectorChain;
            this.sectorSize = sectorSize;
            this.stream = stream;
        }

        public StreamView(List<Sector> sectorChain, int sectorSize, long length, Queue<Sector> availableSectors, Stream stream, bool isFatStream = false)
            : this(sectorChain, sectorSize, stream)
        {
            this.isFatStream = isFatStream;
            adjustLength(length, availableSectors);
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
            return (((this.buf[0] | (this.buf[1] << 8)) | (this.buf[2] << 16)) | (this.buf[3] << 24));
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int nRead = 0;
            int nToRead = 0;

            // Don't try to read more bytes than this stream contains.
            long intMax = Math.Min(Int32.MaxValue, this.length);
            count = Math.Min((int)(intMax), count);

            if (sectorChain != null && sectorChain.Count > 0)
            {
                // First sector
                int secIndex = (int)(position / (long)sectorSize);

                // Bytes to read count is the min between request count
                // and sector border

                nToRead = Math.Min(
                    sectorChain[0].Size - ((int)position % sectorSize),
                    count);

                if (secIndex < sectorChain.Count)
                {
                    Buffer.BlockCopy(
                        sectorChain[secIndex].GetData(),
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
                        sectorChain[secIndex].GetData(),
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
					if (secIndex > sectorChain.Count) throw new CFCorruptedFileException("The file is probably corrupted.");

                    Buffer.BlockCopy(
                        sectorChain[secIndex].GetData(),
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

            if (this.length <= position) // Dont't adjust the length when position is inside the bounds of 0 and the current length.
                adjustLength(position);

            return position;
        }

        private void adjustLength(long value)
        {
            adjustLength(value, null);
        }

        private void adjustLength(long value, Queue<Sector> availableSectors)
        {
            this.length = value;

            long delta = value - ((long)this.sectorChain.Count * (long)sectorSize);

            if (delta > 0)
            {
                // enlargment required

                int nSec = (int)Math.Ceiling(((double)delta / sectorSize));

                while (nSec > 0)
                {
                    Sector t = null;

                    if (availableSectors == null || availableSectors.Count == 0)
                    {
                        t = new Sector(sectorSize, stream);

                        if (sectorSize == Sector.MINISECTOR_SIZE)
                            t.Type = SectorType.Mini;
                    }
                    else
                    {
                        t = availableSectors.Dequeue();
                    }

                    if (isFatStream)
                    {
                        t.InitFATData();
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
            //else
            //{
            //    // FREE Sectors
            //    delta = Math.Abs(delta);

            //    int nSec = (int)Math.Floor(((double)delta / sectorSize));

            //    while (nSec > 0)
            //    {
            //        freeSectors.Add(sectorChain[sectorChain.Count - 1]);
            //        sectorChain.RemoveAt(sectorChain.Count - 1);
            //        nSec--;
            //    }
            //}
        }

        public override void SetLength(long value)
        {
            adjustLength(value);
        }

        public void WriteInt32(int val)
        {
            byte[] buffer = new byte[4];
            buffer[0] = (byte)val;
            buffer[1] = (byte)(val << 8);
            buffer[2] = (byte)(val << 16);
            buffer[3] = (byte)(val << 24);
            Write(buffer, 0, 4);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int byteWritten = 0;
            int roundByteWritten = 0;

            // Assure length
            if ((position + count) > length)
                adjustLength((position + count));

            if (sectorChain != null)
            {
                // First sector
                int secOffset = (int)(position / (long)sectorSize);
                int secShift = (int)(position % sectorSize);

                roundByteWritten = (int)Math.Min(sectorSize - (int)(position % (long)sectorSize), count);

                if (secOffset < sectorChain.Count)
                {
                    Buffer.BlockCopy(
                        buffer,
                        offset,
                        sectorChain[secOffset].GetData(),
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
                        sectorChain[secOffset].GetData(),
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
                        sectorChain[secOffset].GetData(),
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