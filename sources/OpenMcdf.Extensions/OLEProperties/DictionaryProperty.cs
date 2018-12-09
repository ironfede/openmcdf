using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class DictionaryProperty : IDictionaryProperty
    {
        private int codePage;

        public DictionaryProperty(int codePage)
        {
            this.codePage = codePage;
            this.entries = new Dictionary<uint, string>();

        }

        public PropertyType PropertyType
        {
            get
            {
                return PropertyType.DictionaryProperty;
            }
        }

        private Dictionary<uint, string> entries;

        public object Value
        {
            get { return entries; }
            set { entries = (Dictionary<uint, string>)value; }
        }

        public void Read(BinaryReader br)
        {
            long curPos = br.BaseStream.Position;

            uint numEntries = br.ReadUInt32();

            for (uint i = 0; i < numEntries; i++)
            {
                DictionaryEntry de = new DictionaryEntry(codePage);

                de.Read(br);
                this.entries.Add(de.PropertyIdentifier, de.Name);
            }

            int m = (int)(br.BaseStream.Position - curPos) % 4;

            if (m > 0)
            {
                for(int i = 0; i < m; i++)
                {
                    br.ReadByte();
                }
            }

        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(entries.Count);

            foreach (KeyValuePair<uint, string> kv in entries)
            {
                bw.Write(kv.Key);
                string s = kv.Value;
                if (!s.EndsWith("\0"))
                    s += "\0";
                bw.Write(Encoding.GetEncoding(this.codePage).GetBytes(s));
            }

        }
    }
}
