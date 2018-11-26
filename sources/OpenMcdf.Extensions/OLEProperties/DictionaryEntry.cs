using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class DictionaryEntry
    {
        
        public uint PropertyIdentifier { get; set; }
        public int Length { get; set; }
        public byte[] Name { get; set; }

        public void Read(BinaryReader br)
        {
            this.PropertyIdentifier = br.ReadUInt32();
            this.Length = br.ReadInt32();

        }
    }
}
