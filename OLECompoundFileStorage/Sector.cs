using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;
using System.Collections.Specialized;

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
    internal enum SectorType
    {
        Normal, Mini, FAT, DIFAT, RangeLockSector
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

        //public const int HEADER = unchecked((int)0xEEEEEEEE);

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

        public void FillData(byte b)
        {
            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = b;
                }
            }
        }

        public void ZeroData()
        {
            if (this.data != null)
            {
                for (int i = 0; i < this.data.Length; i++)
                {
                    data[i] = 0x00;
                }
            }
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
