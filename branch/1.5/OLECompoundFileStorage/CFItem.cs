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


namespace OLECompoundFileStorage
{
    /// <summary>
    /// Abstract base class for Structured Storage entities
    /// </summary>
    public abstract class CFItem : DirectoryEntry
    {
        private CompoundFile compoundFile;

        protected CompoundFile CompoundFile
        {
            get { return compoundFile; }
        }

        protected void CheckDisposed()
        {
            if (compoundFile.IsClosed)
                throw new CFException("Owner Compound file has been closed and owned items have been invalidated");
        }

        protected CFItem(CompoundFile compoundFile, StgType entryType):base(entryType)
        {
            this.compoundFile = compoundFile;
        }


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
                String n = this.GetEntryName();
                if (n != null && n.Length > 0)
                {
                    return n.TrimEnd('\0');
                }
                else
                    return String.Empty;
            }
        }


      

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
                return this.StgType == StgType.STGTY_STORAGE;
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
                return this.StgType == StgType.STGTY_STREAM;
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
                return this.StgType == StgType.STGTY_ROOT;
            }
        }

        /// <summary>
        /// Get/Set the Creation Date of the current item
        /// </summary>
        public DateTime CreationDate
        {
            get
            {
                return DateTime.FromFileTime(BitConverter.ToInt64(this.CreationDateBytes, 0));
            }

            set
            {
                this.CreationDateBytes = BitConverter.GetBytes((value.ToFileTime()));
            }
        }

        /// <summary>
        /// Get/Set the Modify Date of the current item
        /// </summary>
        public DateTime ModifyDate
        {
            get
            {
                return DateTime.FromFileTime(BitConverter.ToInt64(this.ModifyDateBytes, 0));
            }

            set
            {
                this.ModifyDateBytes = BitConverter.GetBytes((value.ToFileTime()));
            }
        }
    }
}
