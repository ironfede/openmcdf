using System.IO;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class DictionaryEntry
    {
        private const int CP_WINUNICODE = 0x04B0;

        private readonly int codePage;

        private byte[] nameBytes;

        public DictionaryEntry(int codePage)
        {
            this.codePage = codePage;
        }

        public uint PropertyIdentifier { get; set; }
        public int Length { get; set; }
        public string Name => GetName();

        public void Read(BinaryReader br)
        {
            PropertyIdentifier = br.ReadUInt32();
            Length = br.ReadInt32();

            if (codePage != CP_WINUNICODE)
            {
                nameBytes = br.ReadBytes(Length);
            }
            else
            {
                nameBytes = br.ReadBytes(Length << 2);

                var m = Length % 4;
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
            return Encoding.GetEncoding(codePage).GetString(nameBytes);
        }
    }
}