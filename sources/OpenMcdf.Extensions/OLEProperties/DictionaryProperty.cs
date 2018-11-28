using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class DictionaryProperty
    {
        private int codePage;

        public DictionaryProperty(int codePage)
        {
            this.codePage = codePage;
            this.Entries = new Dictionary<uint, string>();

        }

        public Dictionary<uint, string> Entries { get; }

        public void Read(BinaryReader br)
        {
            uint numEntries = br.ReadUInt32();

            for (uint i = 0; i < numEntries; i++)
            {
                DictionaryEntry de = new DictionaryEntry(codePage);

                de.Read(br);
                this.Entries.Add(de.PropertyIdentifier, de.Name);
            }

        }
    }
}
