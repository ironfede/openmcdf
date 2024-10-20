/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 * The Original Code is OpenMCDF - Compound Document Format library.
 *
 * The Initial Developer of the Original Code is Federico Blaseotto.*/

using System;
using System.Collections.Generic;
using System.IO;

namespace OpenMcdf
{
    /// <summary>
    /// Stream decorator for a Sector or miniSector chain
    /// </summary>
    internal sealed class StreamView : Stream
    {
        private readonly int sectorSize;

        private long position;
        private readonly Stream stream;
        private readonly bool isFatStream;
        private readonly List<Sector> freeSectors = new();
        public IEnumerable<Sector> FreeSectors => freeSectors;

        public StreamView(List<Sector> sectorChain, int sectorSize, Stream stream)
        {
            if (sectorChain == null)
                throw new CFException("Sector Chain cannot be null");

            if (sectorSize <= 0)
                throw new CFException("Sector size must be greater than zero");

            BaseSectorChain = sectorChain;
            this.sectorSize = sectorSize;
            this.stream = stream;
        }

        public StreamView(List<Sector> sectorChain, int sectorSize, long length, Queue<Sector> availableSectors, Stream stream, bool isFatStream = false)
            : this(sectorChain, sectorSize, stream)
        {
            this.isFatStream = isFatStream;
            AdjustLength(length, availableSectors);
        }

        public List<Sector> BaseSectorChain { get; }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override void Flush()
        {
        }

        private long length;

        public override long Length => length;

        public override long Position
        {
            get => position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be a non-negative number");

            if ((uint)count > buffer.Length - offset)
                throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection");

            int nRead = 0;

            // Don't try to read more bytes than this stream contains.
            long intMax = Math.Min(int.MaxValue, length);
            count = Math.Min((int)intMax, count);

            // Ensure read request greater then stream length, when position is not 0, return only the limited and correct number of bytes
            count = (int)Math.Min(length - position, count);

            if (BaseSectorChain != null && BaseSectorChain.Count > 0)
            {
                // First sector
                int secIndex = (int)(position / sectorSize);

                // Bytes to read count is the min between request count
                // and sector border

                int nToRead = Math.Min(
                    BaseSectorChain[0].Size - ((int)position % sectorSize),
                    count);

                if (secIndex < BaseSectorChain.Count)
                {
                    Buffer.BlockCopy(
                        BaseSectorChain[secIndex].GetData(),
                        (int)(position % sectorSize),
                        buffer,
                        offset,
                        nToRead);
                }

                nRead += nToRead;

                secIndex++;

                // Central sectors
                while (nRead < (count - sectorSize))
                {
                    nToRead = sectorSize;

                    Buffer.BlockCopy(
                        BaseSectorChain[secIndex].GetData(),
                        0,
                        buffer,
                        offset + nRead,
                        nToRead);

                    nRead += nToRead;
                    secIndex++;
                }

                // Last sector
                nToRead = count - nRead;

                if (nToRead != 0)
                {
                    if (secIndex > BaseSectorChain.Count) throw new CFCorruptedFileException("The file is probably corrupted.");

                    Buffer.BlockCopy(
                        BaseSectorChain[secIndex].GetData(),
                        0,
                        buffer,
                        offset + nRead,
                        nToRead);

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
                    if (offset < 0)
                        throw new IOException("Seek before origin");
                    position = offset;
                    break;

                case SeekOrigin.Current:
                    if (position + offset < 0)
                        throw new IOException("Seek before origin");
                    position += offset;
                    break;

                case SeekOrigin.End:
                    if (Length - offset < 0)
                        throw new IOException("Seek before origin");
                    position = Length - offset;
                    break;

                default:
                    throw new ArgumentException("Invalid seek origin", nameof(origin));
            }

            if (position > length)
                AdjustLength(position);

            return position;
        }

        private void AdjustLength(long value)
        {
            AdjustLength(value, null);
        }

        private void AdjustLength(long value, Queue<Sector> availableSectors)
        {
            length = value;

            long delta = value - (BaseSectorChain.Count * (long)sectorSize);

            if (delta > 0)
            {
                // Enlargement required

                int nSec = (int)Math.Ceiling((double)delta / sectorSize);

                while (nSec > 0)
                {
                    Sector t;
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

                    BaseSectorChain.Add(t);
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
            AdjustLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int byteWritten = 0;

            // Assure length
            if ((position + count) > length)
                AdjustLength(position + count);

            if (BaseSectorChain != null)
            {
                // First sector
                int secOffset = (int)(position / sectorSize);
                int secShift = (int)(position % sectorSize);

                int roundByteWritten = Math.Min(sectorSize - (int)(position % sectorSize), count);

                if (secOffset < BaseSectorChain.Count)
                {
                    Buffer.BlockCopy(
                        buffer,
                        offset,
                        BaseSectorChain[secOffset].GetData(),
                        secShift,
                        roundByteWritten);

                    BaseSectorChain[secOffset].DirtyFlag = true;
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
                        BaseSectorChain[secOffset].GetData(),
                        0,
                        roundByteWritten);

                    BaseSectorChain[secOffset].DirtyFlag = true;

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
                        BaseSectorChain[secOffset].GetData(),
                        0,
                        roundByteWritten);

                    BaseSectorChain[secOffset].DirtyFlag = true;
                }

                position += count;
            }
        }
    }
}
