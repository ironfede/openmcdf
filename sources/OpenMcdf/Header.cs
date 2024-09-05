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
    internal sealed class Header
    {
        public byte[] HeaderSignature { get; private set; } = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

        public byte[] CLSID { get; set; } = new byte[16];

        public ushort MinorVersion { get; private set; } = 0x003E;

        public ushort MajorVersion { get; private set; } = 0x0003;

        public ushort ByteOrder { get; private set; } = 0xFFFE;

        public ushort SectorShift { get; private set; } = 9;

        public ushort MiniSectorShift { get; private set; } = 6;

        public byte[] UnUsed { get; private set; } = new byte[6];

        public int DirectorySectorsNumber { get; set; }

        public int FATSectorsNumber { get; set; }

        public int FirstDirectorySectorID { get; set; } = Sector.ENDOFCHAIN;

        public uint UnUsed2 { get; private set; }

        public uint MinSizeStandardStream { get; set; } = 4096;

        /// <summary>
        /// This integer field contains the starting sector number for the mini FAT
        /// </summary>
        public int FirstMiniFATSectorID { get; set; } = unchecked((int)0xFFFFFFFE);

        public uint MiniFATSectorsNumber { get; set; }

        public int FirstDIFATSectorID { get; set; } = Sector.ENDOFCHAIN;

        public uint DIFATSectorsNumber { get; set; }

        public int[] DIFAT { get; } = new int[109];

        public Header()
            : this(3)
        {
        }

        public Header(ushort version)
        {
            switch (version)
            {
                case 3:
                    this.MajorVersion = 3;
                    this.SectorShift = 0x0009;
                    break;

                case 4:
                    this.MajorVersion = 4;
                    this.SectorShift = 0x000C;
                    break;

                default:
                    throw new CFException("Invalid Compound File Format version");
            }

            for (int i = 0; i < 109; i++)
            {
                DIFAT[i] = Sector.FREESECT;
            }
        }

        public void Write(Stream stream)
        {
            StreamRW rw = new StreamRW(stream);

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

            foreach (int i in DIFAT)
            {
                rw.Write(i);
            }

            if (MajorVersion == 4)
            {
                byte[] zeroHead = new byte[3584];
                rw.Write(zeroHead);
            }

            rw.Close();
        }

        public void Read(Stream stream)
        {
            StreamRW rw = new StreamRW(stream);

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

            for (int i = 0; i < 109; i++)
            {
                this.DIFAT[i] = rw.ReadInt32();
            }

            rw.Close();
        }

        private void CheckVersion()
        {
            if (this.MajorVersion != 3 && this.MajorVersion != 4)
                throw new CFFileFormatException("Unsupported Binary File Format version: OpenMcdf only supports Compound Files with major version equal to 3 or 4 ");
        }

        /// <summary>
        /// Structured Storage signature
        /// </summary>
        private byte[] OLE_CFS_SIGNATURE = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

        private void CheckSignature()
        {
            for (int i = 0; i < HeaderSignature.Length; i++)
            {
                if (HeaderSignature[i] != OLE_CFS_SIGNATURE[i])
                    throw new CFFileFormatException("Invalid OLE structured storage file");
            }
        }
    }
}
