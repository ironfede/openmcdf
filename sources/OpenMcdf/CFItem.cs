
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
        protected CompoundFile CompoundFile { get; }

        protected void CheckDisposed()
        {
            if (CompoundFile.IsClosed)
                throw new CFDisposedException("Owner Compound file has been closed and owned items have been invalidated");
        }

        protected CFItem()
        {
        }

        protected CFItem(CompoundFile compoundFile)
        {
            CompoundFile = compoundFile;
        }

        #region IDirectoryEntry Members


        internal IDirectoryEntry DirEntry { get; set; }

        internal int CompareTo(CFItem other)
        {
            return DirEntry.CompareTo(other.DirEntry);
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return DirEntry.CompareTo(((CFItem)obj).DirEntry);
        }

        #endregion

        public static bool operator ==(CFItem leftItem, CFItem rightItem)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(leftItem, rightItem))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if ((leftItem is null) || (rightItem is null))
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
            return CompareTo(obj) == 0;
        }

        public override int GetHashCode()
        {
            return DirEntry.GetEntryName().GetHashCode();
        }

        /// <summary>
        /// Get entity name
        /// </summary>
        public string Name
        {
            get
            {
                string n = DirEntry.GetEntryName();
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
        /// only if entity is a stream, otherwise it is set to zero.
        /// </summary>
        public long Size => DirEntry.Size;

        /// <summary>
        /// Return true if item is Storage
        /// </summary>
        /// <remarks>
        /// This check doesn't use reflection or runtime type information
        /// and doesn't suffer related performance penalties.
        /// </remarks>
        public bool IsStorage => DirEntry.StgType == StgType.StgStorage;

        /// <summary>
        /// Return true if item is a Stream
        /// </summary>
        /// <remarks>
        /// This check doesn't use reflection or runtime type information
        /// and doesn't suffer related performance penalties.
        /// </remarks>
        public bool IsStream => DirEntry.StgType == StgType.StgStream;

        /// <summary>
        /// Return true if item is the Root Storage
        /// </summary>
        /// <remarks>
        /// This check doesn't use reflection or runtime type information
        /// and doesn't suffer related performance penalties.
        /// </remarks>
        public bool IsRoot => DirEntry.StgType == StgType.StgRoot;

        /// <summary>
        /// Get/Set the Creation Date of the current item
        /// </summary>
        public DateTime CreationDate
        {
            get
            {
                return DateTime.FromFileTimeUtc(BitConverter.ToInt64(DirEntry.CreationDate, 0));
            }

            set
            {
                if (DirEntry.StgType != StgType.StgStream && DirEntry.StgType != StgType.StgRoot)
                    DirEntry.CreationDate = BitConverter.GetBytes(value.ToFileTimeUtc());
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
                return DateTime.FromFileTimeUtc(BitConverter.ToInt64(DirEntry.ModifyDate, 0));
            }

            set
            {
                if (DirEntry.StgType != StgType.StgStream && DirEntry.StgType != StgType.StgRoot)
                    DirEntry.ModifyDate = BitConverter.GetBytes(value.ToFileTimeUtc());
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
                return DirEntry.StorageCLSID;
            }
            set
            {
                if (DirEntry.StgType != StgType.StgStream)
                {
                    DirEntry.StorageCLSID = value;
                }
                else
                    throw new CFException("Object class GUID can only be set on Root and Storage entries");
            }
        }

        int IComparable<CFItem>.CompareTo(CFItem other)
        {
            return DirEntry.CompareTo(other.DirEntry);
        }

        public override string ToString()
        {
            if (DirEntry != null)
                return "[" + DirEntry.LeftSibling + "," + DirEntry.SID + "," + DirEntry.RightSibling + "]" + " " + DirEntry.GetEntryName();
            else
                return string.Empty;
        }
    }
}
