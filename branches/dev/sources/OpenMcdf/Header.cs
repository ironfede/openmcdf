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
        //0 8 Compound document file identifier: D0H CFH 11H E0H A1H B1H 1AH E1H
        private byte[] headerSignature
            = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

        public byte[] HeaderSignature
        {
            get { return headerSignature; }
        }

        //8 16 Unique identifier (UID) of this file (not of interest in the following, may be all 0)
        private byte[] clsid = new byte[16];

        public byte[] CLSID
        {
            get { return clsid; }
            set { clsid = value; }
        }

        //24 2 Revision number of the file format (most used is 003EH)
        private ushort minorVersion = 0x003E;

        public ushort MinorVersion
        {
            get { return minorVersion; }
        }

        //26 2 Version number of the file format (most used is 0003H)
        private ushort majorVersion = 0x0003;

        public ushort MajorVersion
        {
            get { return majorVersion; }
        }

        //28 2 Byte order identifier (➜4.2): FEH FFH = Little-Endian FFH FEH = Big-Endian
        private ushort byteOrder = 0xFFFE;

        public ushort ByteOrder
        {
            get { return byteOrder; }
        }

        //30 2 Size of a sector in the compound document file (➜3.1) in power-of-two (ssz), real sector
        //size is sec_size = 2ssz bytes (minimum value is 7 which means 128 bytes, most used 
        //value is 9 which means 512 bytes)
        private ushort sectorShift = 9;

        public ushort SectorShift
        {
            get { return sectorShift; }
            
        }

        //32 2 Size of a short-sector in the short-stream container stream (➜6.1) in power-of-two (sssz),
        //real short-sector size is short_sec_size = 2sssz bytes (maximum value is sector size
        //ssz, see above, most used value is 6 which means 64 bytes)
        private ushort miniSectorShift = 6;
        public ushort MiniSectorShift
        {
            get { return miniSectorShift; }
        }

        //34 10 Not used
        private byte[] unUsed = new byte[6];

        public byte[] UnUsed
        {
            get { return unUsed; }
        }

        //44 4 Total number of sectors used Directory (➜5.2)
        private int directorySectorsNumber;

        public int DirectorySectorsNumber
        {
            get { return directorySectorsNumber; }
            set { directorySectorsNumber = value; }
        }

        //44 4 Total number of sectors used for the sector allocation table (➜5.2)
        private int fatSectorsNumber;

        public int FATSectorsNumber
        {
            get { return fatSectorsNumber; }
            set { fatSectorsNumber = value; }
        }

        //48 4 SecID of first sector of the directory stream (➜7)
        private int firstDirectorySectorID = Sector.ENDOFCHAIN;

        public int FirstDirectorySectorID
        {
            get { return firstDirectorySectorID; }
            set { firstDirectorySectorID = value; }
        }

        //52 4 Not used
        private uint unUsed2;

        public uint UnUsed2
        {
            get { return unUsed2; }
        }

        //56 4 Minimum size of a standard stream (in bytes, minimum allowed and most used size is 4096
        //bytes), streams with an actual size smaller than (and not equal to) this value are stored as
        //short-streams (➜6)
        private uint minSizeStandardStream = 4096;

        public uint MinSizeStandardStream
        {
            get { return minSizeStandardStream; }
            set { minSizeStandardStream = value; }
        }

        //60 4 SecID of first sector of the short-sector allocation table (➜6.2), or –2 (End Of Chain
        //SecID, ➜3.1) if not extant
        private int firstMiniFATSectorID = unchecked((int)0xFFFFFFFE);

        /// <summary>
        /// This integer field contains the starting sector number for the mini FAT
        /// </summary>
        public int FirstMiniFATSectorID
        {
            get { return firstMiniFATSectorID; }
            set { firstMiniFATSectorID = value; }
        }

        //64 4 Total number of sectors used for the short-sector allocation table (➜6.2)
        private uint miniFATSectorsNumber;

        public uint MiniFATSectorsNumber
        {
            get { return miniFATSectorsNumber; }
            set { miniFATSectorsNumber = value; }
        }

        //68 4 SecID of first sector of the master sector allocation table (➜5.1), or –2 (End Of Chain
        //SecID, ➜3.1) if no additional sectors used
        private int firstDIFATSectorID = Sector.ENDOFCHAIN;

        public int FirstDIFATSectorID
        {
            get { return firstDIFATSectorID; }
            set { firstDIFATSectorID = value; }
        }

        //72 4 Total number of sectors used for the master sector allocation table (➜5.1)
        private uint difatSectorsNumber;

        public uint DIFATSectorsNumber
        {
            get { return difatSectorsNumber; }
            set { difatSectorsNumber = value; }
        }

        //76 436 First part of the master sector allocation table (➜5.1) containing 109 SecIDs
        private int[] difat = new int[109];

        public int[] DIFAT
        {
            get { return difat; }
        }


        public Header()
            : this(3)
        {

        }


        public Header(ushort version)
        {

            switch (version)
            {
                case 3:
                    this.majorVersion = 3;
                    this.sectorShift = 0x0009;
                    break;

                case 4:
                    this.majorVersion = 4;
                    this.sectorShift = 0x000C;
                    break;

                default:
                    throw new CFException("Invalid Compound File Format version");


            }

            for (int i = 0; i < 109; i++)
            {
                difat[i] = Sector.FREESECT;
            }


        }

        public void Write(Stream stream)
        {
            StreamRW rw = new StreamRW(stream);

            rw.Write(headerSignature);
            rw.Write(clsid);
            rw.Write(minorVersion);
            rw.Write(majorVersion);
            rw.Write(byteOrder);
            rw.Write(sectorShift);
            rw.Write(miniSectorShift);
            rw.Write(unUsed);
            rw.Write(directorySectorsNumber);
            rw.Write(fatSectorsNumber);
            rw.Write(firstDirectorySectorID);
            rw.Write(unUsed2);
            rw.Write(minSizeStandardStream);
            rw.Write(firstMiniFATSectorID);
            rw.Write(miniFATSectorsNumber);
            rw.Write(firstDIFATSectorID);
            rw.Write(difatSectorsNumber);

            foreach (int i in difat)
            {
                rw.Write(i);
            }

            if (majorVersion == 4)
            {
                byte[] zeroHead = new byte[3584];
                rw.Write(zeroHead);
            }

            rw.Close();
        }

        public void Read(Stream stream)
        {
            StreamRW rw = new StreamRW(stream);

            headerSignature = rw.ReadBytes(8);
            CheckSignature();
            clsid = rw.ReadBytes(16);
            minorVersion = rw.ReadUInt16();
            majorVersion = rw.ReadUInt16();
            CheckVersion();
            byteOrder = rw.ReadUInt16();
            sectorShift = rw.ReadUInt16();
            miniSectorShift = rw.ReadUInt16();
            unUsed = rw.ReadBytes(6);
            directorySectorsNumber = rw.ReadInt32();
            fatSectorsNumber = rw.ReadInt32();
            firstDirectorySectorID = rw.ReadInt32();
            unUsed2 = rw.ReadUInt32();
            minSizeStandardStream = rw.ReadUInt32();
            firstMiniFATSectorID = rw.ReadInt32();
            miniFATSectorsNumber = rw.ReadUInt32();
            firstDIFATSectorID = rw.ReadInt32();
            difatSectorsNumber = rw.ReadUInt32();

            for (int i = 0; i < 109; i++)
            {
                this.DIFAT[i] = rw.ReadInt32();
            }

            rw.Close();
        }


        private void CheckVersion()
        {
            if (this.majorVersion != 3 && this.majorVersion != 4)
                throw new CFFileFormatException("Unsupported Binary File Format version: OpenMcdf only supports Compound Files with major version equal to 3 or 4 ");
        }

        /// <summary>
        /// Structured Storage signature
        /// </summary>
        private byte[] OLE_CFS_SIGNATURE = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

        private void CheckSignature()
        {
            for (int i = 0; i < headerSignature.Length; i++)
            {
                if (headerSignature[i] != OLE_CFS_SIGNATURE[i])
                    throw new CFFileFormatException("Invalid OLE structured storage file");
            }
        }
    }
}
