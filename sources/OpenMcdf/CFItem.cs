
/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. 
 * 
 * The Original Code is OpenMCDF - Compound Document Format library.
 * 
 * The Initial Developer of the Original Code is Federico Blaseotto.*/

using System;

namespace OpenMcdf
{
    /// <summary>
    /// Abstract base class for Structured Storage entities.
    /// </summary>
    /// <example>
    /// <code>
    /// 
    /// const String STORAGE_NAME = "report.xls";
    /// CompoundFile cf = new CompoundFile(STORAGE_NAME);
    ///
    /// FileStream output = new FileStream("LogEntries.txt", FileMode.Create);
    /// TextWriter tw = new StreamWriter(output);
    ///
    /// // CFItem represents both storage and stream items
    /// VisitedEntryAction va = delegate(CFItem item)
    /// {
    ///      tw.WriteLine(item.Name);
    /// };
    ///
    /// cf.RootStorage.VisitEntries(va, true);
    ///
    /// tw.Close();
    /// 
    /// </code>
    /// </example>
    public abstract class CFItem : IComparable<CFItem>
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

        protected CFItem()
        {
        }

        protected CFItem(CompoundFile compoundFile)
        {
            this.compoundFile = compoundFile;
        }

        #region IDirectoryEntry Members

        private IDirectoryEntry dirEntry;

        internal IDirectoryEntry DirEntry
        {
            get { return dirEntry; }
            set { dirEntry = value; }
        }



        internal int CompareTo(CFItem other)
        {

            return this.dirEntry.CompareTo(other.DirEntry);
        }


        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return this.dirEntry.CompareTo(((CFItem)obj).DirEntry);
        }

        #endregion

        public static bool operator ==(CFItem leftItem, CFItem rightItem)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(leftItem, rightItem))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)leftItem == null) || ((object)rightItem == null))
            {
                return false;
            }

            // Return true if the fields match:
            return leftItem.CompareTo(rightItem) == 0;
        }

        public static bool operator !=(CFItem leftItem, CFItem rightItem)
        {
            return !(leftItem == rightItem);
        }

        public override bool Equals(object obj)
        {
            return this.CompareTo(obj) == 0;
        }

        public override int GetHashCode()
        {
            return this.dirEntry.GetEntryName().GetHashCode();
        }

        /// <summary>
        /// Get entity name
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
                return this.dirEntry.StgType == StgType.StgStorage;
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
                return this.dirEntry.StgType == StgType.StgStream;
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
                return this.dirEntry.StgType == StgType.StgRoot;
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
                if (this.dirEntry.StgType != StgType.StgStream && this.dirEntry.StgType != StgType.StgRoot)
                    this.dirEntry.CreationDate = BitConverter.GetBytes((value.ToFileTime()));
                else
                    throw new CFException("Creation Date can only be set on storage entries");
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
                if (this.dirEntry.StgType != StgType.StgStream && this.dirEntry.StgType != StgType.StgRoot)
                    this.dirEntry.ModifyDate = BitConverter.GetBytes((value.ToFileTime()));
                else
                    throw new CFException("Modify Date can only be set on storage entries");
            }
        }

        /// <summary>
        /// Get/Set Object class Guid for Root and Storage entries.
        /// </summary>
        public Guid CLSID
        {
            get
            {
                return this.dirEntry.StorageCLSID;
            }
            set
            {
                if (this.dirEntry.StgType != StgType.StgStream)
                {
                    this.dirEntry.StorageCLSID = value;
                }
                else
                    throw new CFException("Object class GUID can only be set on Root and Storage entries");
            }
        }

        int IComparable<CFItem>.CompareTo(CFItem other)
        {
            return this.dirEntry.CompareTo(other.DirEntry);
        }

        public override string ToString()
        {
            if (this.dirEntry != null)
                return "[" + this.dirEntry.LeftSibling + "," + this.dirEntry.SID + "," + this.dirEntry.RightSibling + "]" + " " + this.dirEntry.GetEntryName();
            else
                return String.Empty;
        }
    }
}
