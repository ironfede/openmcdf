using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenMcdf.Extensions.OLEProperties.Interfaces;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class DictionaryProperty : IDictionaryProperty
    {
        private readonly int codePage;

        private Dictionary<uint, string> entries;

        public DictionaryProperty(int codePage)
        {
            this.codePage = codePage;
            entries = new Dictionary<uint, string>();
        }

        public PropertyType PropertyType => PropertyType.DictionaryProperty;

        public object Value
        {
            get => entries;
            set => entries = (Dictionary<uint, string>) value;
        }

        public void Read(BinaryReader br)
        {
            var curPos = br.BaseStream.Position;

            var numEntries = br.ReadUInt32();

            for (uint i = 0; i < numEntries; i++)
            {
                var de = new DictionaryEntry(codePage);

                de.Read(br);
                entries.Add(de.PropertyIdentifier, de.Name);
            }

            var m = (int) (br.BaseStream.Position - curPos) % 4;

            if (m > 0)
                for (var i = 0; i < m; i++)
                    br.ReadByte();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(entries.Count);

            foreach (var kv in entries)
            {
                bw.Write(kv.Key);
                var s = kv.Value;
                if (!s.EndsWith("\0"))
                    s += "\0";
                bw.Write(Encoding.GetEncoding(codePage).GetBytes(s));
            }
        }
    }
}