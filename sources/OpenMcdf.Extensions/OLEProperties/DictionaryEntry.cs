using System;
using System.IO;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class DictionaryEntry
    {
        int codePage;

        public DictionaryEntry(int codePage)
        {
            this.codePage = codePage;
        }

        public uint PropertyIdentifier { get; set; }
        public int Length { get; set; }
        public String Name { get { return GetName(); } }

        private byte[] nameBytes;

        public void Read(BinaryReader br)
        {
            PropertyIdentifier = br.ReadUInt32();
            Length = br.ReadInt32();

            if (codePage != CodePages.CP_WINUNICODE)
            {
                nameBytes = br.ReadBytes(Length);
            }
            else
            {
                nameBytes = br.ReadBytes(Length << 1);

                int m = (Length * 2) % 4;
                if (m > 0)
                    br.ReadBytes(m);
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(PropertyIdentifier);
            bw.Write(Length);
            bw.Write(nameBytes);

            //if (codePage == CP_WINUNICODE)
            //    int m = Length % 4;

            //if (m > 0)
            //    for (int i = 0; i < m; i++)
            //        bw.Write((byte)m);
        }

        private string GetName()
        {

            var result = Encoding.GetEncoding(this.codePage).GetString(nameBytes);

            result = result.Trim('\0');

            return result;

        }

    }
}
