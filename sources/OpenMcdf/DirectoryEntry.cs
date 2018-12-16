/* This Source Code Form is subject to the terms of the Mozilla Public
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

namespace OpenMcdf
{
    public enum StgType
    {
        StgInvalid = 0,
        StgStorage = 1,
        StgStream = 2,
        StgLockbytes = 3,
        StgProperty = 4,
        StgRoot = 5
    }

    public enum StgColor
    {
        Red = 0,
        Black = 1
    }

    internal class DirectoryEntry : IDirectoryEntry
    {
        internal const int THIS_IS_GREATER = 1;
        internal const int OTHER_IS_GREATER = -1;

        internal static int NOSTREAM = unchecked((int)0xFFFFFFFF);

        private readonly IList<IDirectoryEntry> dirRepository;

        private ushort nameLength;

        private IDirectoryEntry parent;
        
        private DirectoryEntry(string name, StgType stgType, IList<IDirectoryEntry> dirRepository)
        {
            this.dirRepository = dirRepository;

            StgType = stgType;

            switch (stgType)
            {
                case StgType.StgStream:
                    StorageCLSID = new Guid("00000000000000000000000000000000");
                    //CreationDate = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    //ModifyDate = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    break;

                case StgType.StgStorage:
                    CreationDate = BitConverter.GetBytes(DateTime.Now.ToFileTime());
                    break;

                case StgType.StgRoot:
                    //CreationDate = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    //ModifyDate = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    break;
                case StgType.StgInvalid:
                case StgType.StgLockbytes:
                case StgType.StgProperty:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stgType), stgType, null);
            }

            SetEntryName(name);
        }

        public int SID { get; set; } = -1;

        public byte[] EntryName { get; private set; } = new byte[64];

        public string GetEntryName()
        {
            if (EntryName != null && EntryName.Length > 0)
            {
                return Encoding.Unicode.GetString(EntryName).Remove((nameLength - 1) / 2);
            }

            return string.Empty;
        }

        public void SetEntryName(string entryName)
        {
            if (entryName.Contains(@"\") ||
                entryName.Contains(@"/") ||
                entryName.Contains(@":") ||
                entryName.Contains(@"!")
            )
            {
                throw new CFException(
                    "Invalid character in entry: the characters '\\', '/', ':','!' cannot be used in entry name");
            }

            if (entryName.Length > 31)
            {
                throw new CFException("Entry name MUST be smaller than 31 characters");
            }

            byte[] newName = null;
            var temp = Encoding.Unicode.GetBytes(entryName);
            newName = new byte[64];
            Buffer.BlockCopy(temp, 0, newName, 0, temp.Length);
            newName[temp.Length] = 0x00;
            newName[temp.Length + 1] = 0x00;

            EntryName = newName;
            nameLength = (ushort)(temp.Length + 2);
        }

        public ushort NameLength
        {
            get => nameLength;
            set => throw new NotImplementedException();
        }

        public StgType StgType { get; set; } // = StgType.StgInvalid;

        public StgColor StgColor { get; set; } = StgColor.Black;

        public int LeftSibling { get; set; } = NOSTREAM;

        public int RightSibling { get; set; } = NOSTREAM;

        public int Child { get; set; } = NOSTREAM;

        public Guid StorageCLSID { get; set; } = Guid.NewGuid();

        public int StateBits { get; set; }

        public byte[] CreationDate { get; set; } = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        public byte[] ModifyDate { get; set; } = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        public int StartSetc { get; set; } = Sector.ENDOFCHAIN;

        public long Size { get; set; }


        public int CompareTo(object obj)
        {
            var otherDir = obj as IDirectoryEntry;

            if (otherDir == null)
            {
                throw new CFException("Invalid casting: compared object does not implement IDirectorEntry interface");
            }

            if (NameLength > otherDir.NameLength)
            {
                return THIS_IS_GREATER;
            }

            if (NameLength < otherDir.NameLength)
            {
                return OTHER_IS_GREATER;
            }

            var thisName = Encoding.Unicode.GetString(EntryName, 0, NameLength);
            var otherName = Encoding.Unicode.GetString(otherDir.EntryName, 0, otherDir.NameLength);

            for (var z = 0; z < thisName.Length; z++)
            {
                var thisChar = char.ToUpperInvariant(thisName[z]);
                var otherChar = char.ToUpperInvariant(otherName[z]);

                if (thisChar > otherChar)
                {
                    return THIS_IS_GREATER;
                }

                if (thisChar < otherChar)
                {
                    return OTHER_IS_GREATER;
                }
            }

            return 0;

            //   return String.Compare(Encoding.Unicode.GetString(this.EntryName).ToUpper(), Encoding.Unicode.GetString(other.EntryName).ToUpper());
        }

        public void Write(Stream stream)
        {
            var rw = new StreamRW(stream);

            rw.Write(EntryName);
            rw.Write(nameLength);
            rw.Write((byte)StgType);
            rw.Write((byte)StgColor);
            rw.Write(LeftSibling);
            rw.Write(RightSibling);
            rw.Write(Child);
            rw.Write(StorageCLSID.ToByteArray());
            rw.Write(StateBits);
            rw.Write(CreationDate);
            rw.Write(ModifyDate);
            rw.Write(StartSetc);
            rw.Write(Size);

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
            var rw = new StreamRW(stream);

            EntryName = rw.ReadBytes(64);
            nameLength = rw.ReadUInt16();
            StgType = (StgType)rw.ReadByte();
            //rw.ReadByte();//Ignore color, only black tree
            StgColor = (StgColor)rw.ReadByte();
            LeftSibling = rw.ReadInt32();
            RightSibling = rw.ReadInt32();
            Child = rw.ReadInt32();

            // Thanks to bugaccount (BugTrack id 3519554)
            if (StgType == StgType.StgInvalid)
            {
                LeftSibling = NOSTREAM;
                RightSibling = NOSTREAM;
                Child = NOSTREAM;
            }

            StorageCLSID = new Guid(rw.ReadBytes(16));
            StateBits = rw.ReadInt32();
            CreationDate = rw.ReadBytes(8);
            ModifyDate = rw.ReadBytes(8);
            StartSetc = rw.ReadInt32();

            if (ver == CFSVersion.Ver_3)
            {
                // avoid dirty read for version 3 files (max size: 32bit integer)
                // where most significant bits are not initialized to zero

                Size = rw.ReadInt32();
                rw.ReadBytes(4); //discard most significant 4 (possibly) dirty bytes
            }
            else
            {
                Size = rw.ReadInt64();
            }
        }

        public string Name => GetEntryName();
        
        public IRBNode Left
        {
            get
            {
                if (LeftSibling == NOSTREAM)
                {
                    return null;
                }

                return dirRepository[LeftSibling];
            }
            set
            {
                LeftSibling = value != null ? ((IDirectoryEntry)value).SID : NOSTREAM;

                if (LeftSibling != NOSTREAM)
                {
                    dirRepository[LeftSibling].Parent = this;
                }
            }
        }

        public IRBNode Right
        {
            get => RightSibling == NOSTREAM ? null : dirRepository[RightSibling];
            set
            {
                if (value == null)
                    return;

                RightSibling = ((IDirectoryEntry)value).SID;
                dirRepository[RightSibling].Parent = this;
            }
        }

        public Color Color
        {
            get => (Color)StgColor;
            set => StgColor = (StgColor)value;
        }

        public IRBNode Parent
        {
            get => parent;
            set => parent = value as IDirectoryEntry;
        }

        public IRBNode Grandparent()
        {
            return parent?.Parent;
        }

        public IRBNode Sibling()
        {
            return this == Parent.Left ? Parent.Right : Parent.Left;
        }

        public IRBNode Uncle()
        {
            return parent != null ? Parent.Sibling() : null;
        }
        
        public void AssignValueTo(IRBNode other)
        {
            var d = other as DirectoryEntry;

            d.SetEntryName(GetEntryName());

            d.CreationDate = new byte[CreationDate.Length];
            CreationDate.CopyTo(d.CreationDate, 0);

            d.ModifyDate = new byte[ModifyDate.Length];
            ModifyDate.CopyTo(d.ModifyDate, 0);

            d.Size = Size;
            d.StartSetc = StartSetc;
            d.StateBits = StateBits;
            d.StgType = StgType;
            d.StorageCLSID = new Guid(StorageCLSID.ToByteArray());
            d.Child = Child;
        }

        public override bool Equals(object obj)
        {
            return CompareTo(obj) == 0;
        }

        /// <summary>
        ///     FNV hash, short for Fowler/Noll/Vo
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>(not warranted) unique hash for byte array</returns>
        private static ulong fnv_hash(byte[] buffer)
        {
            ulong h = 2166136261;
            int i;

            for (i = 0; i < buffer.Length; i++)
            {
                h = (h * 16777619) ^ buffer[i];
            }

            return h;
        }

        public override int GetHashCode()
        {
            return (int)fnv_hash(EntryName);
        }

        internal static IDirectoryEntry New(string name, StgType stgType, IList<IDirectoryEntry> dirRepository)
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
            {
                throw new ArgumentNullException(nameof(dirRepository), "Directory repository cannot be null in New() method");
            }

            return de;
        }

        internal static IDirectoryEntry Mock(string name, StgType stgType)
        {
            return new DirectoryEntry(name, stgType, null);
        }

        internal static IDirectoryEntry TryNew(string name, StgType stgType, IList<IDirectoryEntry> dirRepository)
        {
            var de = new DirectoryEntry(name, stgType, dirRepository);

            // If we are not adding an invalid dirEntry as
            // in a normal loading from file (invalid dirs MAY pad a sector)
            if (de == null)
            {
                // TODO: this just doesn't make sense here, what was intended?
            }

            for (var i = 0; i < dirRepository.Count; i++)
            {
                if (dirRepository[i].StgType != StgType.StgInvalid)
                    continue;

                dirRepository[i] = de;
                de.SID = i;
                return de;
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
    }
}