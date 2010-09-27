using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

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
    public enum StgType : byte
    {
        STGTY_INVALID = 0,
        STGTY_STORAGE = 1,
        STGTY_STREAM = 2,
        STGTY_LOCKBYTES = 3,
        STGTY_PROPERTY = 4,
        STGTY_ROOT = 5
    }

    public enum StgColor : byte
    {
        RED = 0,
        BLACK = 1
    }

    public class DirectoryEntry : IComparable, IDirectoryEntry
    {
        
        private int sid = -1;
        public int SID
        {
            get { return sid; }
            set { sid = value; }
        }

        public static Int32 NOSTREAM
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
                return Encoding.Unicode.GetString(entryName).Remove(this.nameLength / 2);
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
                throw new CFSException("Invalid character in entry: the characters '\\', '/', ':','!' cannot be used in entry name");

            if (entryName.Length > 31)
                throw new CFSException("Entry name MUST be smaller than 31 characters");



            byte[] newName = null;
            byte[] temp = Encoding.Unicode.GetBytes(entryName);
            newName = new byte[64];
            Buffer.BlockCopy(temp, 0, newName, 0, temp.Length);
            newName[temp.Length + 1] = 0x00;
            newName[temp.Length + 2] = 0x00;

            this.entryName = newName;
            this.nameLength = (ushort)(temp.Length + 2);

        }

        private ushort nameLength = 0;
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

        private StgType stgType = StgType.STGTY_INVALID;
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
        private StgColor stgColor = StgColor.BLACK;
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


        private Int32 stateBits = 0x0000;

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


        public int CompareTo(object other)
        {
            const int THIS_IS_GREATER = 1;
            const int OTHER_IS_GREATER = -1;

            if ((other as IDirectoryEntry) == null)
                throw new Exception();

            IDirectoryEntry otherDir = (IDirectoryEntry)other;
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
                String thisName = Encoding.Unicode.GetString(this.EntryName).ToUpper();
                String otherName = Encoding.Unicode.GetString(otherDir.EntryName).ToUpper();

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
            byte b = br.ReadByte();
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
