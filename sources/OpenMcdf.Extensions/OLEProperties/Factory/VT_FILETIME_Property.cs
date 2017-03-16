using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_FILETIME_Property : TypedPropertyValue
        {

            public VT_FILETIME_Property(VTPropertyType vType, PropertyContext ctx, PropertyDimensions dim) : base(vType, ctx, dim)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        Int64 tmp = br.ReadInt64();
                        propertyValue = DateTime.FromFileTime(tmp);
                        break;
                    case PropertyDimensions.IsVector:
                        uint size = br.ReadUInt32();
                        var t = new List<DateTime>();
                        for (int i = 0; i < size; i++)
                        {
                            t.Add(DateTime.FromFileTime(br.ReadInt64()));
                        }
                        this.propertyValue = t;
                        break;
                }

            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        bw.Write(((DateTime)propertyValue).ToOADate());
                        break;
                    case PropertyDimensions.IsVector:
                        var t = this.propertyValue as List<DateTime>;
                        bw.Write((uint)t.Count);
                        foreach (var d in t)
                        {
                            bw.Write(d.ToFileTime());
                        }


                        break;
                }

            }
        }

    }
}
