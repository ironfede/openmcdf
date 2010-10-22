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
            set { sectorShift = value; }
        }

        //32 2 Size of a short-sector in the short-stream container stream (➜6.1) in power-of-two (sssz),
        //real short-sector size is short_sec_size = 2sssz bytes (maximum value is sector size
        //ssz, see above, most used value is 6 which means 64 bytes)
        private ushort miniSectorShift = 6;
        public ushort MiniSectorShift
        {
            get { return miniSectorShift; }
            set { miniSectorShift = value; }
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
        private uint unUsed2 = 0;

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
        private uint miniFATSectorsNumber = 0;

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
        private uint difatSectorsNumber = 0;

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
        {
            for (int i = 0; i < 109; i++)
            {
                difat[i] = Sector.FREESECT;
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(headerSignature);
            bw.Write(clsid);
            bw.Write(minorVersion);
            bw.Write(majorVersion);
            bw.Write(byteOrder);
            bw.Write(sectorShift);
            bw.Write(miniSectorShift);
            bw.Write(unUsed);
            bw.Write(directorySectorsNumber);
            bw.Write(fatSectorsNumber);
            bw.Write(firstDirectorySectorID);
            bw.Write(unUsed2);
            bw.Write(minSizeStandardStream);
            bw.Write(firstMiniFATSectorID);
            bw.Write(miniFATSectorsNumber);
            bw.Write(firstDIFATSectorID);
            bw.Write(difatSectorsNumber);

            foreach (int i in difat)
            {
                bw.Write(i);
            }
        }

        public void Read(BinaryReader br)
        {
            headerSignature = br.ReadBytes(8);
            CheckSignature();
            clsid = br.ReadBytes(16);
            minorVersion = br.ReadUInt16();
            majorVersion = br.ReadUInt16();
            CheckVersion();
            byteOrder = br.ReadUInt16();
            sectorShift = br.ReadUInt16();
            miniSectorShift = br.ReadUInt16();
            unUsed = br.ReadBytes(6);
            directorySectorsNumber = br.ReadInt32();
            fatSectorsNumber = br.ReadInt32();
            firstDirectorySectorID = br.ReadInt32();
            unUsed2 = br.ReadUInt32();
            minSizeStandardStream = br.ReadUInt32();
            firstMiniFATSectorID = br.ReadInt32();
            miniFATSectorsNumber = br.ReadUInt32();
            firstDIFATSectorID = br.ReadInt32();
            difatSectorsNumber = br.ReadUInt32();

            for (int i = 0; i < 109; i++)
            {
                this.DIFAT[i] = br.ReadInt32();
            }
        }

        private UInt16 SUPPORTED_VERSION = 0x0003;
        private void CheckVersion()
        {
            if(this.majorVersion!= SUPPORTED_VERSION)
                throw  new CFFileFormatException("Unsupported version. OLECFS only supports Compound Files with major version equal to 3 ");
        }

        /// <summary>
        /// Structured Storage signature
        /// </summary>
        private byte[] OLE_CFS_SIGNATURE = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
        
        private void CheckSignature()
        {
            for (int i = 0; i<headerSignature.Length;i++)
            {
                if (headerSignature[i] != OLE_CFS_SIGNATURE[i])
                    throw new CFFileFormatException("Invalid OLE structured storage file");
            }
        }
    }
}
