using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_LPSTR_Property : TypedPropertyValue
        {
            private const int CP_WINUNICODE = 0x04B0;
            private uint size = 0;
            private byte[] data;

            public VT_LPSTR_Property(VTPropertyType vType, PropertyContext ctx, PropertyDimensions dim) : base(vType, ctx, dim)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                if (Dimensions == PropertyDimensions.IsVector)
                {
                    this.propertyValue = new List<String>();
                    size = br.ReadUInt32();

                    for (int i = 0; i < size; i++)
                    {
                        uint len = br.ReadUInt32();

                        string s = Encoding.GetEncoding(Ctx.CodePage).GetString(br.ReadBytes((int)len));
                        s = !String.IsNullOrEmpty(s) ? s.Substring(0, s.Length - 1) : String.Empty;
                        ((List<string>)propertyValue).Add(s);
                    }
                }
                else
                {
                    size = br.ReadUInt32();

                    data = br.ReadBytes((int)size);
                    string s = Encoding.GetEncoding(Ctx.CodePage).GetString(data);
                    this.propertyValue = !String.IsNullOrEmpty(s) ? s.Substring(0, s.Length - 1) : String.Empty;
                    //int m = (int)size % 4;
                    //br.ReadBytes(m); // padding
                }
            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                if (Dimensions == PropertyDimensions.IsVector)
                {
                    List<string> l = propertyValue as List<string>;
                    int totW = 0;
                    bw.Write((uint)l.Count);
                    foreach (var s in l)
                    {
                        var g = s + "\0";
                        var bc = Encoding.GetEncoding(Ctx.CodePage).GetByteCount(g);
                        bw.Write(bc);
                        totW += bc;
                        bw.Write(Encoding.GetEncoding(Ctx.CodePage).GetBytes(g));
                    }
                    int mod = totW % 4;
                    for (int i = 0; i < mod; i++)  // padding
                        bw.Write((byte)0);
                }
                else
                {
                    data = Encoding.GetEncoding(Ctx.CodePage).GetBytes((String)(propertyValue + "\0"));
                    size = (uint)data.Length;
                    int m = (int)size % 4;
                    bw.Write(m);
                    bw.Write(data);
                    for (int i = 0; i < m; i++)  // padding
                        bw.Write((byte)0);
                }

            }
        }

    }
}
