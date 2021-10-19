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
    public abstract class ReadonlyCompoundFileItem : IComparable<ReadonlyCompoundFileItem>
    {
        protected ReadonlyCompoundFile CompoundFile { get; }

        protected void CheckDisposed()
        {
            if (CompoundFile.IsClosed)
                throw new CFDisposedException(
                    "Owner Compound file has been closed and owned items have been invalidated");
        }

        protected ReadonlyCompoundFileItem()
        {
        }

        protected ReadonlyCompoundFileItem(ReadonlyCompoundFile compoundFile)
        {
            this.CompoundFile = compoundFile;
        }

        #region IDirectoryEntry Members

        internal IDirectoryEntry DirEntry { get; set; }


        internal int CompareTo(ReadonlyCompoundFileItem other)
        {
            return this.DirEntry.CompareTo(other.DirEntry);
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return this.DirEntry.CompareTo(((ReadonlyCompoundFileItem) obj).DirEntry);
        }

        #endregion

        public static bool operator ==(ReadonlyCompoundFileItem leftItem, ReadonlyCompoundFileItem rightItem)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(leftItem, rightItem))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object) leftItem == null) || ((object) rightItem == null))
            {
                return false;
            }

            // Return true if the fields match:
            return leftItem.CompareTo(rightItem) == 0;
        }

        public static bool operator !=(ReadonlyCompoundFileItem leftItem, ReadonlyCompoundFileItem rightItem)
        {
            return !(leftItem == rightItem);
        }

        public override bool Equals(object obj)
        {
            return this.CompareTo(obj) == 0;
        }

        public override int GetHashCode()
        {
            return this.DirEntry.GetEntryName().GetHashCode();
        }

        /// <summary>
        /// Get entity name
        /// </summary>
        public string Name
        {
            get
            {
                string n = this.DirEntry.GetEntryName();
                if (n != null && n.Length > 0)
                {
                    return n.TrimEnd('\0');
                }
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Size in bytes of the item. It has a valid value 
        /// only if entity is a stream, otherwise it is setted to zero.
        /// </summary>
        public long Size
        {
            get { return this.DirEntry.Size; }
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
            get { return this.DirEntry.StgType == StgType.StgStorage; }
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
            get { return this.DirEntry.StgType == StgType.StgStream; }
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
            get { return this.DirEntry.StgType == StgType.StgRoot; }
        }

        /// <summary>
        /// Get/Set the Creation Date of the current item
        /// </summary>
        public DateTime CreationDate
        {
            get { return DateTime.FromFileTime(BitConverter.ToInt64(this.DirEntry.CreationDate, 0)); }

            set
            {
                if (this.DirEntry.StgType != StgType.StgStream && this.DirEntry.StgType != StgType.StgRoot)
                    this.DirEntry.CreationDate = BitConverter.GetBytes((value.ToFileTime()));
                else
                    throw new CFException("Creation Date can only be set on storage entries");
            }
        }

        /// <summary>
        /// Get/Set the Modify Date of the current item
        /// </summary>
        public DateTime ModifyDate
        {
            get { return DateTime.FromFileTime(BitConverter.ToInt64(this.DirEntry.ModifyDate, 0)); }

            set
            {
                if (this.DirEntry.StgType != StgType.StgStream && this.DirEntry.StgType != StgType.StgRoot)
                    this.DirEntry.ModifyDate = BitConverter.GetBytes((value.ToFileTime()));
                else
                    throw new CFException("Modify Date can only be set on storage entries");
            }
        }

        /// <summary>
        /// Get/Set Object class Guid for Root and Storage entries.
        /// </summary>
        public Guid CLSID
        {
            get { return this.DirEntry.StorageCLSID; }
            set
            {
                if (this.DirEntry.StgType != StgType.StgStream)
                {
                    this.DirEntry.StorageCLSID = value;
                }
                else
                    throw new CFException("Object class GUID can only be set on Root and Storage entries");
            }
        }

        int IComparable<ReadonlyCompoundFileItem>.CompareTo(ReadonlyCompoundFileItem other)
        {
            return this.DirEntry.CompareTo(other.DirEntry);
        }

        public override string ToString()
        {
            if (this.DirEntry != null)
                return
                    $"[{this.DirEntry.LeftSibling},{this.DirEntry.SID},{this.DirEntry.RightSibling}] {this.DirEntry.GetEntryName()}";
            else
                return string.Empty;
        }
    }
}