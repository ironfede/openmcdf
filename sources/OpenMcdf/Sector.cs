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
    internal enum SectorType
    {
        Normal,
        Mini,
        FAT,
        DIFAT,
        RangeLockSector,
        Directory
    }

    internal sealed class Sector
    {
        public const int MINISECTOR_SIZE = 64;

        public const int FREESECT = unchecked((int)0xFFFFFFFF);
        public const int ENDOFCHAIN = unchecked((int)0xFFFFFFFE);
        public const int FATSECT = unchecked((int)0xFFFFFFFD);
        public const int DIFSECT = unchecked((int)0xFFFFFFFC);

        public bool DirtyFlag { get; set; } = false;

        public bool IsStreamed => (stream != null && Size != MINISECTOR_SIZE) && (Id * Size) + Size < stream.Length;

        private readonly Stream stream;

        public Sector(int size, Stream stream)
        {
            Size = size;
            this.stream = stream;
        }

        public Sector(int size)
        {
            Size = size;
            data = null;
            stream = null;
        }

        internal SectorType Type { get; set; }

        public int Id { get; set; } = -1;

        public int Size { get; private set; } = 0;

        private byte[] data;

        public byte[] GetData()
        {
            if (data == null)
            {
                data = new byte[Size];

                if (IsStreamed)
                {
                    long position = Size + Id * (long)Size;
                    // Enlarge the stream if necessary and possible
                    long endPosition = position + Size;
                    if (endPosition > stream.Length)
                    {
                        if (!stream.CanWrite)
                            return data;
                        stream.SetLength(endPosition);
                    }
                    stream.Seek(position, SeekOrigin.Begin);
                    stream.ReadExactly(data, 0, Size);
                }
            }

            return data;
        }

        //public void SetSectorData(byte[] b)
        //{
        //    this.data = b;
        //}

        //public void FillData(byte b)
        //{
        //    if (data != null)
        //    {
        //        for (int i = 0; i < data.Length; i++)
        //        {
        //            data[i] = b;
        //        }
        //    }
        //}

        public void ZeroData()
        {
            if (data is null)
                data = new byte[Size];
            else
                Array.Clear(data, 0, data.Length);
            DirtyFlag = true;
        }

        public void InitFATData()
        {
            data ??= new byte[Size];
            for (int i = 0; i < Size; i++)
                data[i] = 0xFF;

            DirtyFlag = true;
        }

        internal void ReleaseData()
        {
            data = null;
        }
    }
}
