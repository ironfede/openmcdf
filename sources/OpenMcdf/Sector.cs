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

    internal class Sector : IDisposable
    {
        public const int FREESECT = unchecked((int) 0xFFFFFFFF);
        public const int ENDOFCHAIN = unchecked((int) 0xFFFFFFFE);
        public const int FATSECT = unchecked((int) 0xFFFFFFFD);
        public const int DIFSECT = unchecked((int) 0xFFFFFFFC);
        public static int MINISECTOR_SIZE = 64;

        private byte[] data;

        private readonly object lockObject = new object();

        private readonly Stream stream;


        public Sector(int size, Stream stream)
        {
            Size = size;
            this.stream = stream;
        }

        public Sector(int size, byte[] data)
        {
            Size = size;
            this.data = data;
            stream = null;
        }

        public Sector(int size)
        {
            Size = size;
            data = null;
            stream = null;
        }

        public bool DirtyFlag { get; set; }

        public bool IsStreamed => stream != null && Size != MINISECTOR_SIZE && Id * Size + Size < stream.Length;

        internal SectorType Type { get; set; }

        public int Id { get; set; } = -1;

        public int Size { get; private set; }

        public byte[] GetData()
        {
            if (data == null)
            {
                data = new byte[Size];

                if (IsStreamed)
                {
                    stream.Seek(Size + Id * (long) Size, SeekOrigin.Begin);
                    stream.Read(data, 0, Size);
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
            data = new byte[Size];
            DirtyFlag = true;
        }

        public void InitFATData()
        {
            data = new byte[Size];

            for (var i = 0; i < Size; i++)
                data[i] = 0xFF;

            DirtyFlag = true;
        }

        internal void ReleaseData()
        {
            data = null;
        }

        /// <summary>
        ///     When called from user code, release all resources, otherwise, in the case runtime called it,
        ///     only unmanagd resources are released.
        /// </summary>
        /// <param name="disposing">If true, method has been called from User code, if false it's been called from .net runtime</param>
        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (!_disposed)
                    lock (lockObject)
                    {
                        if (disposing)
                        {
                            // Call from user code...
                        }

                        data = null;
                        DirtyFlag = false;
                        Id = ENDOFCHAIN;
                        Size = 0;
                    }
            }
            finally
            {
                _disposed = true;
            }
        }

        #region IDisposable Members

        private bool _disposed; //false

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}