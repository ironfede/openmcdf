using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_VECTOR_LPSTR_Property : TypedPropertyValue
        {
            private uint size = 0;
            private byte[] data;
            private int codePage;

            public VT_VECTOR_LPSTR_Property(VTPropertyType vType, int codePage) : base(vType)
            {
                this.codePage = codePage;
            }

            public override void Read(System.IO.BinaryReader br)
            {
                this.propertyValue = new List<String>();
                size = br.ReadUInt32();
                for(int i = 0; i < size; i++)
                {
                    uint len = br.ReadUInt32();

                }
                data = br.ReadBytes((int)size);
                this.propertyValue = Encoding.GetEncoding(codePage).GetString(data);
                int m = (int)size % 4;
                br.ReadBytes(m); // padding
            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                data = Encoding.GetEncoding(codePage).GetBytes((String)propertyValue);
                size = (uint)data.Length;
                int m = (int)size % 4;
                bw.Write(data);
                for (int i = 0; i < m; i++)  // padding
                    bw.Write(0);
            }
        }

    }
}
