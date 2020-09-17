/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. 
 * 
 * The Original Code is OpenMCDF - Compound Document Format library.
 * 
 * The Initial Developer of the Original Code is Federico Blaseotto.*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OpenMcdf
{
    public enum StgType : int
    {
        StgInvalid = 0,
        StgStorage = 1,
        StgStream = 2,
        StgLockbytes = 3,
        StgProperty = 4,
        StgRoot = 5
    }

    public enum StgColor : int
    {
        Red = 0,
        Black = 1
    }

    internal class DirectoryEntry : IDirectoryEntry
    {
        internal const int THIS_IS_GREATER = 1;
        internal const int OTHER_IS_GREATER = -1;
        private IList<IDirectoryEntry> dirRepository;

        private int sid = -1;
        public int SID
        {
            get { return sid; }
            set { sid = value; }
        }

        internal static Int32 NOSTREAM
            = unchecked((int)0xFFFFFFFF);

        private DirectoryEntry(String name, StgType stgType, IList<IDirectoryEntry> dirRepository)
        {
            this.dirRepository = dirRepository;

            this.stgType = stgType;

            if (stgType == StgType.StgStorage)
            {
                this.creationDate = BitConverter.GetBytes((DateTime.Now.ToFileTime()));
            }

            this.SetEntryName(name);

        }

        private byte[] entryName = new byte[64];

        public byte[] EntryName
        {
            get
            {
                return entryName;
            }
            //set
            //{
            //    entryName = value;
            //}
        }

        public String GetEntryName()
        {
            if (entryName != null && entryName.Length > 0)
            {
                return Encoding.Unicode.GetString(entryName).Remove((this.nameLength - 1) / 2);
            }
            else
                return String.Empty;
        }

        public void SetEntryName(String entryName)
        {
            if (
                entryName.Contains(@"\") ||
                entryName.Contains(@"/") ||
                entryName.Contains(@":") ||
                entryName.Contains(@"!")

                )
                throw new CFException("Invalid character in entry: the characters '\\', '/', ':','!' cannot be used in entry name");

            if (entryName.Length > 31)
                throw new CFException("Entry name MUST NOT exceed 31 characters");



            byte[] newName = null;
            byte[] temp = Encoding.Unicode.GetBytes(entryName);
            newName = new byte[64];
            Buffer.BlockCopy(temp, 0, newName, 0, temp.Length);
            newName[temp.Length] = 0x00;
            newName[temp.Length + 1] = 0x00;

            this.entryName = newName;
            this.nameLength = (ushort)(temp.Length + 2);

        }

        private ushort nameLength;
        public ushort NameLength
        {
            get
            {
                return nameLength;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        private StgType stgType = StgType.StgInvalid;
        public StgType StgType
        {
            get
            {
                return stgType;
            }
            set
            {
                stgType = value;
            }
        }
        private StgColor stgColor = StgColor.Black;

        public StgColor StgColor
        {
            get
            {
                return stgColor;
            }
            set
            {
                stgColor = value;
            }
        }

        private Int32 leftSibling = NOSTREAM;
        public Int32 LeftSibling
        {
            get { return leftSibling; }
            set { leftSibling = value; }
        }

        private Int32 rightSibling = NOSTREAM;
        public Int32 RightSibling
        {
            get { return rightSibling; }
            set { rightSibling = value; }
        }

        private Int32 child = NOSTREAM;
        public Int32 Child
        {
            get { return child; }
            set { child = value; }
        }

        private Guid storageCLSID
            = Guid.Empty;

        public Guid StorageCLSID
        {
            get
            {
                return storageCLSID;
            }
            set
            {
                this.storageCLSID = value;
            }
        }


        private Int32 stateBits;

        public Int32 StateBits
        {
            get { return stateBits; }
            set { stateBits = value; }
        }

        private byte[] creationDate = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        public byte[] CreationDate
        {
            get
            {
                return creationDate;
            }
            set
            {
                creationDate = value;
            }
        }

        private byte[] modifyDate = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        public byte[] ModifyDate
        {
            get
            {
                return modifyDate;
            }
            set
            {
                modifyDate = value;
            }
        }

        private Int32 startSetc = Sector.ENDOFCHAIN;
        public Int32 StartSetc
        {
            get
            {
                return startSetc;
            }
            set
            {
                startSetc = value;
            }
        }
        private long size;
        public long Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
            }
        }


        public int CompareTo(object obj)
        {

            IDirectoryEntry otherDir = obj as IDirectoryEntry;

            if (otherDir == null)
                throw new CFException("Invalid casting: compared object does not implement IDirectorEntry interface");

            if (this.NameLength > otherDir.NameLength)
            {
                return THIS_IS_GREATER;
            }
            else if (this.NameLength < otherDir.NameLength)
            {
                return OTHER_IS_GREATER;
            }
            else
            {
                String thisName = Encoding.Unicode.GetString(this.EntryName, 0, this.NameLength);
                String otherName = Encoding.Unicode.GetString(otherDir.EntryName, 0, otherDir.NameLength);

                for (int z = 0; z < thisName.Length; z++)
                {
                    char thisChar = char.ToUpperInvariant(thisName[z]);
                    char otherChar = char.ToUpperInvariant(otherName[z]);

                    if (thisChar > otherChar)
                        return THIS_IS_GREATER;
                    else if (thisChar < otherChar)
                        return OTHER_IS_GREATER;
                }

                return 0;

            }

            //   return String.Compare(Encoding.Unicode.GetString(this.EntryName).ToUpper(), Encoding.Unicode.GetString(other.EntryName).ToUpper());
        }

        public override bool Equals(object obj)
        {
            return this.CompareTo(obj) == 0;
        }

        /// <summary>
        /// FNV hash, short for Fowler/Noll/Vo
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>(not warranted) unique hash for byte array</returns>
        private static ulong fnv_hash(byte[] buffer)
        {

            ulong h = 2166136261;
            int i;

            for (i = 0; i < buffer.Length; i++)
                h = (h * 16777619) ^ buffer[i];

            return h;
        }

        public override int GetHashCode()
        {
            return (int)fnv_hash(this.entryName);
        }

        public void Write(Stream stream)
        {
            StreamRW rw = new StreamRW(stream);

            rw.Write(entryName);
            rw.Write(nameLength);
            rw.Write((byte)stgType);
            rw.Write((byte)stgColor);
            rw.Write(leftSibling);
            rw.Write(rightSibling);
            rw.Write(child);
            rw.Write(storageCLSID.ToByteArray());
            rw.Write(stateBits);
            rw.Write(creationDate);
            rw.Write(modifyDate);
            rw.Write(startSetc);
            rw.Write(size);

            rw.Close();
        }

        //public Byte[] ToByteArray()
        //{
        //    MemoryStream ms
        //        = new MemoryStream(128);

        //    BinaryWriter bw = new BinaryWriter(ms);

        //    byte[] paddedName = new byte[64];
        //    Array.Copy(entryName, paddedName, entryName.Length);

        //    bw.Write(paddedName);
        //    bw.Write(nameLength);
        //    bw.Write((byte)stgType);
        //    bw.Write((byte)stgColor);
        //    bw.Write(leftSibling);
        //    bw.Write(rightSibling);
        //    bw.Write(child);
        //    bw.Write(storageCLSID.ToByteArray());
        //    bw.Write(stateBits);
        //    bw.Write(creationDate);
        //    bw.Write(modifyDate);
        //    bw.Write(startSetc);
        //    bw.Write(size);

        //    return ms.ToArray();
        //}

        public void Read(Stream stream, CFSVersion ver = CFSVersion.Ver_3)
        {
            StreamRW rw = new StreamRW(stream);

            entryName = rw.ReadBytes(64);
            nameLength = rw.ReadUInt16();
            stgType = (StgType)rw.ReadByte();
            //rw.ReadByte();//Ignore color, only black tree
            stgColor = (StgColor)rw.ReadByte();
            leftSibling = rw.ReadInt32();
            rightSibling = rw.ReadInt32();
            child = rw.ReadInt32();

            // Thanks to bugaccount (BugTrack id 3519554)
            if (stgType == StgType.StgInvalid)
            {
                leftSibling = NOSTREAM;
                rightSibling = NOSTREAM;
                child = NOSTREAM;
            }

            storageCLSID = new Guid(rw.ReadBytes(16));
            stateBits = rw.ReadInt32();
            creationDate = rw.ReadBytes(8);
            modifyDate = rw.ReadBytes(8);
            startSetc = rw.ReadInt32();

            if (ver == CFSVersion.Ver_3)
            {
                // avoid dirty read for version 3 files (max size: 32bit integer)
                // where most significant bits are not initialized to zero

                size = rw.ReadInt32();
                rw.ReadBytes(4); //discard most significant 4 (possibly) dirty bytes
            }
            else
            {
                size = rw.ReadInt64();
            }
        }

        public string Name
        {
            get { return GetEntryName(); }
        }


        public RedBlackTree.IRBNode Left
        {
            get
            {
                if (leftSibling == DirectoryEntry.NOSTREAM)
                    return null;

                return dirRepository[leftSibling];
            }
            set
            {
                leftSibling = value != null ? ((IDirectoryEntry)value).SID : DirectoryEntry.NOSTREAM;

                if (leftSibling != DirectoryEntry.NOSTREAM)
                    dirRepository[leftSibling].Parent = this;
            }
        }

        public RedBlackTree.IRBNode Right
        {
            get
            {
                if (rightSibling == DirectoryEntry.NOSTREAM)
                    return null;

                return dirRepository[rightSibling];
            }
            set
            {

                rightSibling = value != null ? ((IDirectoryEntry)value).SID : DirectoryEntry.NOSTREAM;

                if (rightSibling != DirectoryEntry.NOSTREAM)
                    dirRepository[rightSibling].Parent = this;

            }
        }

        public RedBlackTree.Color Color
        {
            get
            {
                return (RedBlackTree.Color)StgColor;
            }
            set
            {
                StgColor = (StgColor)value;
            }
        }

        private IDirectoryEntry parent = null;

        public RedBlackTree.IRBNode Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value as IDirectoryEntry;
            }
        }

        public RedBlackTree.IRBNode Grandparent()
        {
            return parent != null ? parent.Parent : null;
        }

        public RedBlackTree.IRBNode Sibling()
        {
            if (this == Parent.Left)
                return Parent.Right;
            else
                return Parent.Left;
        }

        public RedBlackTree.IRBNode Uncle()
        {
            return parent != null ? Parent.Sibling() : null;
        }

        internal static IDirectoryEntry New(String name, StgType stgType, IList<IDirectoryEntry> dirRepository)
        {
            DirectoryEntry de = null;
            if (dirRepository != null)
            {
                de = new DirectoryEntry(name, stgType, dirRepository);
                // No invalid directory entry found
                dirRepository.Add(de);
                de.SID = dirRepository.Count - 1;
            }
            else
                throw new ArgumentNullException("dirRepository", "Directory repository cannot be null in New() method");

            return de;
        }

        internal static IDirectoryEntry Mock(String name, StgType stgType)
        {
            DirectoryEntry de = new DirectoryEntry(name, stgType, null);

            return de;
        }

        internal static IDirectoryEntry TryNew(String name, StgType stgType, IList<IDirectoryEntry> dirRepository)
        {
            DirectoryEntry de = new DirectoryEntry(name, stgType, dirRepository);

            // If we are not adding an invalid dirEntry as
            // in a normal loading from file (invalid dirs MAY pad a sector)
            if (de != null)
            {
                // Find first available invalid slot (if any) to reuse it
                for (int i = 0; i < dirRepository.Count; i++)
                {
                    if (dirRepository[i].StgType == StgType.StgInvalid)
                    {
                        dirRepository[i] = de;
                        de.SID = i;
                        return de;
                    }
                }
            }

            // No invalid directory entry found
            dirRepository.Add(de);
            de.SID = dirRepository.Count - 1;

            return de;
        }




        public override string ToString()
        {
            return this.Name + " [" + this.sid + "]" + (this.stgType == StgType.StgStream ? "Stream" : "Storage");
        }


        public void AssignValueTo(RedBlackTree.IRBNode other)
        {
            DirectoryEntry d = other as DirectoryEntry;

            d.SetEntryName(this.GetEntryName());

            d.creationDate = new byte[this.creationDate.Length];
            this.creationDate.CopyTo(d.creationDate, 0);

            d.modifyDate = new byte[this.modifyDate.Length];
            this.modifyDate.CopyTo(d.modifyDate, 0);

            d.size = this.size;
            d.startSetc = this.startSetc;
            d.stateBits = this.stateBits;
            d.stgType = this.stgType;
            d.storageCLSID = new Guid(this.storageCLSID.ToByteArray());
            d.Child = this.Child;
        }
    }
}
