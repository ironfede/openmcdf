/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 * The Original Code is OpenMCDF - Compound Document Format library.
 *
 * The Initial Developer of the Original Code is Federico Blaseotto.*/

using System;
using System.IO;

namespace OpenMcdf3
{
    internal sealed class Sector 
    {
        public const int MINISECTOR_SIZE = 64;

        public const int FREESECT = unchecked((int)0xFFFFFFFF);
        public const int ENDOFCHAIN = unchecked((int)0xFFFFFFFE);
        public const int FATSECT = unchecked((int)0xFFFFFFFD);
        public const int DIFSECT = unchecked((int)0xFFFFFFFC);

        public int Id { get; set; } = -1;
        public int Size { get; private set; }

        private byte[] data;

        public bool DirtyFlag { get; set; }
        public SectorType Type { get; set; }
        
        public Sector(int size, SectorType sectorType)
        {
            Size = size;
            data = null;
            Type = SectorType.Normal;
        }

        
      

       

        public byte[] Data 
        {
            get => data;
            set => data = value;
        }
        

        public void ZeroData()
        {
            Array.Clear(data, 0, data.Length);
            DirtyFlag = true;
        }

        public void InitFATData()
        {
            data ??= new byte[Size];
            for (int i = 0; i < Size; i++)
                data[i] = 0xFF;

            DirtyFlag = true;
        }
    }
}
