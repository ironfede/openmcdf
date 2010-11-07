using System;
using System.Collections.Generic;
using System.Text;
using BinaryTrees;

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
    /// Abstract base class for Structured Storage entities
    /// </summary>
    public abstract class CFItem : IDirectoryEntry, IComparable
    {
        private CompoundFile compoundFile;

        protected CompoundFile CompoundFile
        {
            get { return compoundFile; }
        }

        protected void CheckDisposed()
        {
            if (compoundFile.IsClosed)
                throw new CFDisposedException("Owner Compound file has been closed and owned items have been invalidated");
        }

        protected CFItem(CompoundFile compoundFile)
        {
            this.compoundFile = compoundFile;
        }

        #region IDirectoryEntry Members

        internal IDirectoryEntry dirEntry;

        int IDirectoryEntry.Child
        {
            get
            {
                return this.dirEntry.Child;

            }
            set
            {
                this.dirEntry.Child = value;
            }
        }

        internal int CompareTo(IDirectoryEntry other)
        {
            return this.dirEntry.CompareTo(other);
        }

        byte[] IDirectoryEntry.CreationDate
        {
            get
            {
                return this.dirEntry.CreationDate;
            }
            set
            {
                this.dirEntry.CreationDate = value;
            }
        }

        byte[] IDirectoryEntry.EntryName
        {
            get { return this.dirEntry.EntryName; }
        }

        string IDirectoryEntry.GetEntryName()
        {
            return this.dirEntry.GetEntryName();
        }

        int IDirectoryEntry.LeftSibling
        {
            get
            {
                return this.dirEntry.LeftSibling;
            }
            set
            {
                this.dirEntry.LeftSibling = value;
            }
        }

        byte[] IDirectoryEntry.ModifyDate
        {
            get
            {
                return this.dirEntry.ModifyDate;
            }
            set
            {
                this.dirEntry.ModifyDate = value;
            }
        }

        string IDirectoryEntry.Name
        {
            get { return this.dirEntry.Name; }
        }

        ushort IDirectoryEntry.NameLength
        {
            get
            {
                return this.dirEntry.NameLength;
            }
            set
            {
                this.dirEntry.NameLength = value;
            }
        }

        void IDirectoryEntry.Read(System.IO.BinaryReader br)
        {
            this.dirEntry.Read(br);
        }

        int IDirectoryEntry.RightSibling
        {
            get
            {
                return this.dirEntry.RightSibling;
            }
            set
            {
                this.dirEntry.RightSibling = value;
            }
        }

        void IDirectoryEntry.SetEntryName(string entryName)
        {
            this.dirEntry.SetEntryName(entryName);
        }

        int IDirectoryEntry.SID
        {
            get
            {
                return this.dirEntry.SID;
            }
            set
            {
                this.dirEntry.SID = value;
            }
        }

        long IDirectoryEntry.Size
        {
            get
            {
                return this.dirEntry.Size;
            }
            set
            {
                this.dirEntry.Size = value;
            }
        }



        int IDirectoryEntry.StartSetc
        {
            get
            {
                return this.dirEntry.StartSetc;
            }
            set
            {
                this.dirEntry.StartSetc = value;
            }
        }

        int IDirectoryEntry.StateBits
        {
            get
            {
                return this.dirEntry.StateBits;
            }
            set
            {
                this.dirEntry.StateBits = value;
            }
        }

        StgColor IDirectoryEntry.StgColor
        {
            get
            {
                return this.dirEntry.StgColor;
            }
            set
            {
                this.dirEntry.StgColor = value;
            }
        }

        StgType IDirectoryEntry.StgType
        {
            get
            {
                return this.dirEntry.StgType;
            }
            set
            {
                this.dirEntry.StgType = value;
            }
        }

        Guid IDirectoryEntry.StorageCLSID
        {
            get
            {
                return this.dirEntry.StorageCLSID;
            }
            set
            {
                this.dirEntry.StorageCLSID = value;
            }
        }

        byte[] IDirectoryEntry.ToByteArray()
        {
            return this.dirEntry.ToByteArray();
        }

        void IDirectoryEntry.Write(System.IO.BinaryWriter bw)
        {
            this.dirEntry.Write(bw);
        }



        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return this.dirEntry.CompareTo(obj as IDirectoryEntry);
        }

        #endregion

        /// <summary>
        /// Entity Name
        /// </summary>
        public String Name
        {
            get
            {
                String n = this.dirEntry.GetEntryName();
                if (n != null && n.Length > 0)
                {
                    return n.TrimEnd('\0');
                }
                else
                    return String.Empty;
            }
        }

        /// <summary>
        /// Size in bytes of the item. It has a valid value 
        /// only if entity is a stream, otherwise it is setted to zero.
        /// </summary>
        public long Size
        {
            get
            {
                return this.dirEntry.Size;
            }
        }

        ///// <summary>
        ///// Structured Storage item type: stream, storage or root entity.
        ///// </summary>
        //public StgType ItemType
        //{
        //    get
        //    {
        //        return this.dirEntry.StgType;
        //    }
        //}

        /// <summary>
        /// Return true if item is Storage
        /// </summary>
        /// <remarks>
        /// This check doesn't use reflection or runtime type information
        /// and doesn't suffer related performance penalties.
        /// </remarks>
        public bool IsStorage
        {
            get
            {
                return this.dirEntry.StgType == StgType.STGTY_STORAGE;
            }
        }

        /// <summary>
        /// Return true if item is a Stream
        /// </summary>
        /// <remarks>
        /// This check doesn't use reflection or runtime type information
        /// and doesn't suffer related performance penalties.
        /// </remarks>
        public bool IsStream
        {
            get
            {
                return this.dirEntry.StgType == StgType.STGTY_STREAM;
            }
        }

        /// <summary>
        /// Return true if item is the Root Storage
        /// </summary>
        /// <remarks>
        /// This check doesn't use reflection or runtime type information
        /// and doesn't suffer related performance penalties.
        /// </remarks>
        public bool IsRoot
        {
            get
            {
                return this.dirEntry.StgType == StgType.STGTY_ROOT;
            }
        }

        /// <summary>
        /// Get/Set the Creation Date of the current item
        /// </summary>
        public DateTime CreationDate
        {
            get
            {
                return DateTime.FromFileTime(BitConverter.ToInt64(this.dirEntry.CreationDate, 0));
            }

            set
            {
                this.dirEntry.CreationDate = BitConverter.GetBytes((value.ToFileTime()));
            }
        }

        /// <summary>
        /// Get/Set the Modify Date of the current item
        /// </summary>
        public DateTime ModifyDate
        {
            get
            {
                return DateTime.FromFileTime(BitConverter.ToInt64(this.dirEntry.ModifyDate, 0));
            }

            set
            {
                this.dirEntry.ModifyDate = BitConverter.GetBytes((value.ToFileTime()));
            }
        }
    }
}
