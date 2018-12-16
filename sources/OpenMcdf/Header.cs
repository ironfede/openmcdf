/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. 
 * 
 * The Original Code is OpenMCDF - Compound Document Format library.
 * 
 * The Initial Developer of the Original Code is Federico Blaseotto.*/


using System.IO;

namespace OpenMcdf
{
    internal class Header
    {
        // 00 | 8 | Compound document file identifier: D0H CFH 11H E0H A1H B1H 1AH E1H
        // 08 | 16 | Unique identifier (UID) of this file (not of interest in the following, may be all 0)
        // 24 | 2 | Revision number of the file format (most used is 003EH)
        // 26 | 2 | Version number of the file format (most used is 0003H)
        // 28 | 2 | Byte order identifier (➜4.2): FEH FFH = Little-Endian FFH FEH = Big-Endian
        // 30 | 2 | Size of a sector in the compound document file (➜3.1) in power-of-two (ssz), real sector
        //size is sec_size = 2ssz bytes (minimum value is 7 which means 128 bytes, most used 
        //value is 9 which means 512 bytes)
        // 32 | 2 | Size of a short-sector in the short-stream container stream (➜6.1) in power-of-two (sssz),
        //real short-sector size is short_sec_size = 2sssz bytes (maximum value is sector size
        //ssz, see above, most used value is 6 which means 64 bytes)
        // 34 | 6 | Not used
        // 40 | 4 | Total number of sectors used Directory (➜5.2)
        // 44 | 4 | Total number of sectors used for the sector allocation table (➜5.2)
        // 48 | 4 | SecID of first sector of the directory stream (➜7)
        // 52 | 4 | Not used
        // 56 | 4 | Minimum size of a standard stream (in bytes, minimum allowed and most used size is 4096
        //bytes), streams with an actual size smaller than (and not equal to) this value are stored as
        //short-streams (➜6)
        // 60 | 4 | SecID of first sector of the short-sector allocation table (➜6.2), or –2 (End Of Chain
        //SecID, ➜3.1) if not extant
        // 64 | 4 | Total number of sectors used for the short-sector allocation table (➜6.2)
        // 68 | 4 | SecID of first sector of the master sector allocation table (➜5.1), or –2 (End Of Chain
        //SecID, ➜3.1) if no additional sectors used
        // 72 | 4 | Total number of sectors used for the master sector allocation table (➜5.1)
        // 76 | 436 | First part of the master sector allocation table (➜5.1) containing 109 SecIDs

        public byte[] HeaderSignature { get; private set; } = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

        public byte[] CLSID { get; set; } = new byte[16];

        public ushort MinorVersion { get; private set; } = 0x003E;

        public ushort MajorVersion { get; private set; } // = 0x0003;

        public ushort ByteOrder { get; private set; } = 0xFFFE;

        public ushort SectorShift { get; private set; } // = 9;

        public ushort MiniSectorShift { get; private set; } = 6;

        public byte[] UnUsed { get; private set; } = new byte[6];

        public int DirectorySectorsNumber { get; set; }

        public int FATSectorsNumber { get; set; }

        public int FirstDirectorySectorID { get; set; } = Sector.ENDOFCHAIN;

        public uint UnUsed2 { get; private set; }

        public uint MinSizeStandardStream { get; set; } = 4096;

        /// <summary>
        ///     This integer field contains the starting sector number for the mini FAT
        /// </summary>
        public int FirstMiniFATSectorID { get; set; } = unchecked((int)0xFFFFFFFE);

        public uint MiniFATSectorsNumber { get; set; }

        public int FirstDIFATSectorID { get; set; } = Sector.ENDOFCHAIN;

        public uint DIFATSectorsNumber { get; set; }

        public int[] DIFAT { get; } = new int[109];

        /// <summary>
        ///     Structured Storage signature
        /// </summary>
        private readonly byte[] OLE_CFS_SIGNATURE = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

        public Header()
            : this(3)
        {
        }

        public Header(ushort version)
        {
            switch (version)
            {
                case 3:
                    MajorVersion = 3;
                    SectorShift = 0x0009;
                    break;

                case 4:
                    MajorVersion = 4;
                    SectorShift = 0x000C;
                    break;

                default:
                    throw new CFException("Invalid Compound File Format version");
            }

            for (var i = 0; i < 109; i++)
            {
                DIFAT[i] = Sector.FREESECT;
            }
        }

        public void Write(Stream stream)
        {
            var rw = new StreamRW(stream);

            rw.Write(HeaderSignature);
            rw.Write(CLSID);
            rw.Write(MinorVersion);
            rw.Write(MajorVersion);
            rw.Write(ByteOrder);
            rw.Write(SectorShift);
            rw.Write(MiniSectorShift);
            rw.Write(UnUsed);
            rw.Write(DirectorySectorsNumber);
            rw.Write(FATSectorsNumber);
            rw.Write(FirstDirectorySectorID);
            rw.Write(UnUsed2);
            rw.Write(MinSizeStandardStream);
            rw.Write(FirstMiniFATSectorID);
            rw.Write(MiniFATSectorsNumber);
            rw.Write(FirstDIFATSectorID);
            rw.Write(DIFATSectorsNumber);

            foreach (var i in DIFAT)
            {
                rw.Write(i);
            }

            if (MajorVersion == 4)
            {
                var zeroHead = new byte[3584];
                rw.Write(zeroHead);
            }

            rw.Close();
        }

        public void Read(Stream stream)
        {
            var rw = new StreamRW(stream);

            HeaderSignature = rw.ReadBytes(8);
            CheckSignature();
            CLSID = rw.ReadBytes(16);
            MinorVersion = rw.ReadUInt16();
            MajorVersion = rw.ReadUInt16();
            CheckVersion();
            ByteOrder = rw.ReadUInt16();
            SectorShift = rw.ReadUInt16();
            MiniSectorShift = rw.ReadUInt16();
            UnUsed = rw.ReadBytes(6);
            DirectorySectorsNumber = rw.ReadInt32();
            FATSectorsNumber = rw.ReadInt32();
            FirstDirectorySectorID = rw.ReadInt32();
            UnUsed2 = rw.ReadUInt32();
            MinSizeStandardStream = rw.ReadUInt32();
            FirstMiniFATSectorID = rw.ReadInt32();
            MiniFATSectorsNumber = rw.ReadUInt32();
            FirstDIFATSectorID = rw.ReadInt32();
            DIFATSectorsNumber = rw.ReadUInt32();

            for (var i = 0; i < 109; i++)
            {
                DIFAT[i] = rw.ReadInt32();
            }

            rw.Close();
        }


        private void CheckVersion()
        {
            if (MajorVersion != 3 && MajorVersion != 4)
            {
                throw new CFFileFormatException(
                    "Unsupported Binary File Format version: OpenMcdf only supports Compound Files with major version equal to 3 or 4 ");
            }
        }

        private void CheckSignature()
        {
                        for (var i = 0; i < HeaderSignature.Length; i++)
            {
                if (HeaderSignature[i] != OLE_CFS_SIGNATURE[i])
                {
                    throw new CFFileFormatException("Invalid OLE structured storage file");
                }
            }
        }
    }
}