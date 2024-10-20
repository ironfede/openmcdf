﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 * The Original Code is OpenMCDF - Compound Document Format library.
 *
 * The Initial Developer of the Original Code is Federico Blaseotto.*/

using RedBlackTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenMcdf3
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

    internal sealed class DirectoryEntry : IRBNode
    {
        internal const int THIS_IS_GREATER = 1;
        internal const int OTHER_IS_GREATER = -1;
        internal const int NOSTREAM = unchecked((int)0xFFFFFFFF);
        internal const int ZERO = 0;
        internal const int EntryNameLength = 64;

        private readonly IList<DirectoryEntry> dirRepository;

        public int SID { get; set; } = -1;

        private DirectoryEntry(string name, StgType stgType, IList<DirectoryEntry> dirRepository)
        {
            this.dirRepository = dirRepository;

            StgType = stgType;

            if (stgType == StgType.StgStorage)
            {
                CreationDate = BitConverter.GetBytes(DateTime.Now.ToFileTimeUtc());
                StartSect = ZERO;
            }

            if (stgType == StgType.StgInvalid)
            {
                StartSect = ZERO;
            }

            if (name != string.Empty)
            {
                SetEntryName(name);
            }
        }

        public byte[] EntryName { get; private set; } = new byte[EntryNameLength];

        public string GetEntryName()
        {
            if (EntryName != null && EntryName.Length > 0)
            {
                return Encoding.Unicode.GetString(EntryName).Remove((nameLength - 1) / 2);
            }
            else
                return string.Empty;
        }

        public void SetEntryName(string entryName)
        {
            if (entryName == string.Empty)
            {
                Array.Clear(EntryName, 0, EntryName.Length);
                nameLength = 0;
            }
            else
            {
                if (
                    entryName.Contains(@"\") ||
                    entryName.Contains(@"/") ||
                    entryName.Contains(@":") ||
                    entryName.Contains(@"!"))
                    throw new CFException("Invalid character in entry: the characters '\\', '/', ':','!' cannot be used in entry name");

                if (Encoding.Unicode.GetByteCount(entryName) + 2 > EntryNameLength)
                    throw new CFException($"Encoded entry name exceeds maximum length of ({EntryNameLength} bytes)");

                Array.Clear(EntryName, 0, EntryName.Length);
                int localNameLength = Encoding.Unicode.GetBytes(entryName, 0, entryName.Length, EntryName, 0);
                nameLength = (ushort)(localNameLength + 2);
            }
        }

        private ushort nameLength;

        public ushort NameLength
        {
            get => nameLength;
            set => throw new NotImplementedException();
        }

        public StgType StgType { get; set; } = StgType.StgInvalid;

        public StgColor StgColor { get; set; } = StgColor.Red;

        public int LeftSibling { get; set; } = NOSTREAM;

        public int RightSibling { get; set; } = NOSTREAM;

        public int Child { get; set; } = NOSTREAM;

        private Guid storageCLSID
            = Guid.Empty;

        public Guid StorageCLSID
        {
            get => storageCLSID;
            set => storageCLSID = value;
        }

        public int StateBits { get; set; }

        public byte[] CreationDate { get; set; } = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        public byte[] ModifyDate { get; set; } = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        public int StartSect { get; set; } = Sector.ENDOFCHAIN;

        public long Size { get; set; }

        public int CompareTo(object obj)
        {
            if (obj is not DirectoryEntry otherDir)
                throw new CFException("Invalid casting: compared object does not implement IDirectorEntry interface");

            if (NameLength > otherDir.NameLength)
            {
                return THIS_IS_GREATER;
            }
            else if (NameLength < otherDir.NameLength)
            {
                return OTHER_IS_GREATER;
            }
            else
            {
                string thisName = Encoding.Unicode.GetString(EntryName, 0, NameLength);
                string otherName = Encoding.Unicode.GetString(otherDir.EntryName, 0, otherDir.NameLength);

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
            return CompareTo(obj) == 0;
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
            return (int)fnv_hash(EntryName);
        }

        public void Write(StreamRW streamRW)
        {
            streamRW.Write(EntryName);
            streamRW.Write(nameLength);
            streamRW.Write((byte)StgType);
            streamRW.Write((byte)StgColor);
            streamRW.Write(LeftSibling);
            streamRW.Write(RightSibling);
            streamRW.Write(Child);
            streamRW.Write(storageCLSID);
            streamRW.Write(StateBits);
            streamRW.Write(CreationDate);
            streamRW.Write(ModifyDate);
            streamRW.Write(StartSect);
            streamRW.Write(Size);
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

        public void Read(StreamRW streamRW, CFSVersion ver = CFSVersion.Ver_3)
        {
            streamRW.ReadBytes(EntryName);
            nameLength = streamRW.ReadUInt16();
            StgType = (StgType)streamRW.ReadByte();
            //rw.ReadByte();//Ignore color, only black tree
            StgColor = (StgColor)streamRW.ReadByte();
            LeftSibling = streamRW.ReadInt32();
            RightSibling = streamRW.ReadInt32();
            Child = streamRW.ReadInt32();

            // Thanks to bugaccount (BugTrack id 3519554)
            if (StgType == StgType.StgInvalid)
            {
                LeftSibling = NOSTREAM;
                RightSibling = NOSTREAM;
                Child = NOSTREAM;
            }

            storageCLSID = streamRW.ReadGuid();
            StateBits = streamRW.ReadInt32();
            streamRW.ReadBytes(CreationDate);
            streamRW.ReadBytes(ModifyDate);
            StartSect = streamRW.ReadInt32();

            if (ver == CFSVersion.Ver_3)
            {
                // avoid dirty read for version 3 files (max size: 32bit integer)
                // where most significant bits are not initialized to zero

                Size = streamRW.ReadInt32();
                streamRW.Seek(4, SeekOrigin.Current); // discard most significant 4 (possibly) dirty bytes
            }
            else
            {
                Size = streamRW.ReadInt64();
            }
        }

        public string Name => GetEntryName();

        public RedBlackTree.IRBNode Left
        {
            get
            {
                if (LeftSibling == NOSTREAM)
                    return null;

                return dirRepository[LeftSibling];
            }
            set
            {
                LeftSibling = value != null ? ((DirectoryEntry)value).SID : NOSTREAM;

                if (LeftSibling != NOSTREAM)
                    dirRepository[LeftSibling].Parent = this;
            }
        }

        public RedBlackTree.IRBNode Right
        {
            get
            {
                if (RightSibling == NOSTREAM)
                    return null;

                return dirRepository[RightSibling];
            }
            set
            {
                RightSibling = value != null ? ((DirectoryEntry)value).SID : NOSTREAM;

                if (RightSibling != NOSTREAM)
                    dirRepository[RightSibling].Parent = this;
            }
        }

        public RedBlackTree.Color Color
        {
            get => (RedBlackTree.Color)StgColor;
            set => StgColor = (StgColor)value;
        }

        private DirectoryEntry parent;

        public RedBlackTree.IRBNode Parent
        {
            get => parent;
            set => parent = value as DirectoryEntry;
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

        internal static DirectoryEntry New(string name, StgType stgType, IList<DirectoryEntry> dirRepository)
        {
            DirectoryEntry de;
            if (dirRepository != null)
            {
                de = new DirectoryEntry(name, stgType, dirRepository);
                // No invalid directory entry found
                dirRepository.Add(de);
                de.SID = dirRepository.Count - 1;
            }
            else
                throw new ArgumentNullException(nameof(dirRepository), "Directory repository cannot be null in New() method");

            return de;
        }

        internal static DirectoryEntry Mock(string name, StgType stgType)
        {
            DirectoryEntry de = new DirectoryEntry(name, stgType, null);

            return de;
        }

        internal static DirectoryEntry TryNew(string name, StgType stgType, IList<DirectoryEntry> dirRepository)
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
            return Name + " [" + SID + "]" + (StgType == StgType.StgStream ? "Stream" : "Storage");
        }

        public void AssignValueTo(RedBlackTree.IRBNode other)
        {
            DirectoryEntry d = other as DirectoryEntry;
            d.SetEntryName(GetEntryName());
            CreationDate.CopyTo(d.CreationDate, 0);
            ModifyDate.CopyTo(d.ModifyDate, 0);
            d.Size = Size;
            d.StartSect = StartSect;
            d.StateBits = StateBits;
            d.StgType = StgType;
            d.storageCLSID = storageCLSID;
            d.Child = Child;
        }

        /// <summary>
        /// Reset a directory entry setting it to StgInvalid in the Directory.
        /// </summary>
        public void Reset()
        {
            // TODO: Delete IDirectoryEntry interface and use as DirectoryEntry
            // member instead for improved performance from devirtualization
            SetEntryName(string.Empty);
            Left = null;
            Right = null;
            Parent = null;
            StgType = StgType.StgInvalid;
            StartSect = ZERO;
            StorageCLSID = Guid.Empty;
            Size = 0;
            StateBits = 0;
            StgColor = StgColor.Red;
            Array.Clear(CreationDate, 0, CreationDate.Length);
            Array.Clear(ModifyDate, 0, ModifyDate.Length);
        }
    }
}
