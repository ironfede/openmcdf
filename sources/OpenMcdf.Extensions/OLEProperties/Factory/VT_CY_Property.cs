using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_CY_Property : TypedPropertyValue
        {

            public VT_CY_Property(VTPropertyType vType, PropertyContext ctx, PropertyDimensions dim) : base(vType, ctx, dim)
            {
            }

            public override void Read(System.IO.BinaryReader br)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        Int64 temp = br.ReadInt64();
                        this.propertyValue = (double)(temp /= 10000);
                        break;
                    case PropertyDimensions.IsVector:
                        this.propertyValue = new List<double>();
                        var size = br.ReadUInt32();
                        for (int i = 0; i < size; i++)
                        {
                            Int64 t = br.ReadInt64();
                            ((List<double>)propertyValue).Add((double)(t /= 10000));
                        }
                        break;
                }

            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        bw.Write((Int64)propertyValue * 10000);
                        break;
                    case PropertyDimensions.IsVector:
                        bw.Write((uint)((List<double>)propertyValue).Count);
                        foreach (double d in ((List<double>)propertyValue))
                        {
                            bw.Write((Int64)(d * 10000));
                        }

                        break;

                }
            }
        }
    }
}
