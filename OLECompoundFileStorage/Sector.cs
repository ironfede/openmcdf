using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;
using System.Collections.Specialized;

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
    internal enum SectorType
    {
        Normal, Mini, FAT, DIFAT
    }

    internal class Sector
    {
        public static int SECTOR_SIZE = 512;
        public static int MINISECTOR_SIZE = 64;

        public const int FREESECT = unchecked((int)0xFFFFFFFF);
        public const int ENDOFCHAIN = unchecked((int)0xFFFFFFFE);
        public const int FATSECT = unchecked((int)0xFFFFFFFD);
        public const int DIFSECT = unchecked((int)0xFFFFFFFC);

        private bool isAllocated = false;
        public bool IsAllocated
        {
            get { return isAllocated; }
        }

        public const int HEADER = unchecked((int)0xEEEEEEEE);

        public Sector(int size)
        {

            this.data = new byte[size];

            for (int i = 0; i < size; i++)
            {
                data[i] = 0xFF;
            }

        }

        public Sector()
        {
        }

        private SectorType type;

        internal SectorType Type
        {
            get { return type; }
            set { type = value; }
        }

        private int id = -1;

        public int Id
        {
            get { return id; }
            set
            {
                isAllocated = true;
                id = value;
            }
        }

        public int Size
        {
            get
            {
                if (data != null)
                    return data.Length;
                else
                    return 0;
            }
        }

        private byte[] data = new byte[SECTOR_SIZE];

        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }

        public static Sector LoadSector(int secID, BinaryReader reader, int size)
        {
            Sector s = null;

            s = new Sector();
            s.Id = secID;
            reader.BaseStream.Position = 512 + secID * size;
            s.data = reader.ReadBytes(size);

            return s;
        }


    }




}
