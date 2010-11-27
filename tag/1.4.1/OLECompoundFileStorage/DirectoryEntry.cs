using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;

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

    public class DirectoryEntry : IComparable, IDirectoryEntry
    {

        private int sid = -1;
        public int SID
        {
            get { return sid; }
            set { sid = value; }
        }

        internal static Int32 NOSTREAM
            = unchecked((int)0xFFFFFFFF);

        public DirectoryEntry(StgType stgType)
        {
            this.stgType = stgType;
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
                throw new CFException("Entry name MUST be smaller than 31 characters");



            byte[] newName = null;
            byte[] temp = Encoding.Unicode.GetBytes(entryName);
            newName = new byte[64];
            Buffer.BlockCopy(temp, 0, newName, 0, temp.Length);
            newName[temp.Length + 1] = 0x00;
            newName[temp.Length + 2] = 0x00;

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
            = Guid.NewGuid();

        public Guid StorageCLSID
        {
            get
            {
                return storageCLSID;
            }
            set
            {
                throw new NotImplementedException();
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
            const int THIS_IS_GREATER = 1;
            const int OTHER_IS_GREATER = -1;
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
                String thisName = Encoding.Unicode.GetString(this.EntryName).ToUpper(CultureInfo.InvariantCulture);
                String otherName = Encoding.Unicode.GetString(otherDir.EntryName).ToUpper(CultureInfo.InvariantCulture);

                for (int z = 0; z < thisName.Length; z++)
                {
                    if (BitConverter.ToInt16(BitConverter.GetBytes(thisName[z]), 0) > BitConverter.ToInt16(BitConverter.GetBytes(otherName[z]), 0))
                        return THIS_IS_GREATER;
                    else if (BitConverter.ToInt16(BitConverter.GetBytes(thisName[z]), 0) < BitConverter.ToInt16(BitConverter.GetBytes(otherName[z]), 0))
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

        public void Write(BinaryWriter bw)
        {
            bw.Write(entryName);
            bw.Write(nameLength);
            bw.Write((byte)stgType);
            bw.Write((byte)stgColor);
            bw.Write(leftSibling);
            bw.Write(rightSibling);
            bw.Write(child);
            bw.Write(storageCLSID.ToByteArray());
            bw.Write(stateBits);
            bw.Write(creationDate);
            bw.Write(modifyDate);
            bw.Write(startSetc);
            bw.Write(size);
        }

        public Byte[] ToByteArray()
        {
            MemoryStream ms
                = new MemoryStream(128);

            BinaryWriter bw = new BinaryWriter(ms);

            byte[] paddedName = new byte[64];
            Array.Copy(entryName, paddedName, entryName.Length);

            bw.Write(paddedName);
            bw.Write(nameLength);
            bw.Write((byte)stgType);
            bw.Write((byte)stgColor);
            bw.Write(leftSibling);
            bw.Write(rightSibling);
            bw.Write(child);
            bw.Write(storageCLSID.ToByteArray());
            bw.Write(stateBits);
            bw.Write(creationDate);
            bw.Write(modifyDate);
            bw.Write(startSetc);
            bw.Write(size);

            return ms.ToArray();
        }

        public void Read(BinaryReader br)
        {
            entryName = br.ReadBytes(64);
            nameLength = br.ReadUInt16();
            stgType = (StgType)br.ReadByte();
            br.ReadByte();//Ignore color, only black tree
            //stgColor = (StgColor)br.ReadByte();
            leftSibling = br.ReadInt32();
            rightSibling = br.ReadInt32();
            child = br.ReadInt32();
            storageCLSID = new Guid(br.ReadBytes(16));
            stateBits = br.ReadInt32();
            creationDate = br.ReadBytes(8);
            modifyDate = br.ReadBytes(8);
            startSetc = br.ReadInt32();
            size = br.ReadInt64();
        }

        public string Name
        {
            get { return GetEntryName(); }
        }

    }
}
