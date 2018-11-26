using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class DictionaryProperty
    {
        public List<DictionaryProperty> Entries { get; set; }

        public void Read(BinaryReader br)
        {
            uint numEntries = br.ReadUInt32();

            for(uint i=0; i< numEntries; i++)
            {
                DictionaryEntry dp = new DictionaryEntry();
                dp.Read(br);
            }
        }

    }
}
