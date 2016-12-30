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
        Normal, Mini, FAT, DIFAT, RangeLockSector, Directory
    }

    internal class Sector : IDisposable
    {
        public static int MINISECTOR_SIZE = 64;

        public const int FREESECT = unchecked((int)0xFFFFFFFF);
        public const int ENDOFCHAIN = unchecked((int)0xFFFFFFFE);
        public const int FATSECT = unchecked((int)0xFFFFFFFD);
        public const int DIFSECT = unchecked((int)0xFFFFFFFC);

        private bool dirtyFlag = false;

        public bool DirtyFlag
        {
            get { return dirtyFlag; }
            set { dirtyFlag = value; }
        }

        public bool IsStreamed
        {
            get { return (stream != null && size != MINISECTOR_SIZE) ? (this.id * size) + size < stream.Length : false; }
        }

        private int size = 0;
        private Stream stream;


        public Sector(int size, Stream stream)
        {
            this.size = size;
            this.stream = stream;
        }

        public Sector(int size, byte[] data)
        {
            this.size = size;
            this.data = data;
            this.stream = null;
        }

        public Sector(int size)
        {
            this.size = size;
            this.data = null;
            this.stream = null;
        }

        private SectorType type;

        internal SectorType Type
        {
            get { return type; }
            set { type = value; }
        }

        private int id = -1;

        public int Id
        {
            get { return id; }
            set
            {
                id = value;
            }
        }

        public int Size
        {
            get
            {
                return size;
            }
        }

        private byte[] data;

        public byte[] GetData()
        {
            if (this.data == null)
            {
                data = new byte[size];

                if (IsStreamed)
                {
                    stream.Seek((long)size + (long)this.id * (long)size, SeekOrigin.Begin);
                    stream.Read(data, 0, size);
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
            data = new byte[size];
            dirtyFlag = true;
        }

        public void InitFATData()
        {
            data = new byte[size];
            
            for (int i = 0; i < size; i++)
                data[i] = 0xFF;

            dirtyFlag = true;
        }

        internal void ReleaseData()
        {
            this.data = null;
        }

        private object lockObject = new Object();

        /// <summary>
        /// When called from user code, release all resources, otherwise, in the case runtime called it,
        /// only unmanagd resources are released.
        /// </summary>
        /// <param name="disposing">If true, method has been called from User code, if false it's been called from .net runtime</param>
        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (!_disposed)
                {
                    lock (lockObject)
                    {
                        if (disposing)
                        {
                            // Call from user code...


                        }

                        this.data = null;
                        this.dirtyFlag = false;
                        this.id = Sector.ENDOFCHAIN;
                        this.size = 0;

                    }
                }
            }
            finally
            {
                _disposed = true;
            }

        }

        #region IDisposable Members

        private bool _disposed;//false

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }




}
