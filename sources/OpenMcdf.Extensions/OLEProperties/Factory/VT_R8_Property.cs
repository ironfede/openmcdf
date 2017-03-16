using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_R8_Property : TypedPropertyValue
        {
            public VT_R8_Property(VTPropertyType vType, PropertyContext ctx, PropertyDimensions dim) : base(vType, ctx, dim)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        this.propertyValue = br.ReadDouble();
                        break;

                    case PropertyDimensions.IsVector:
                        uint size = br.ReadUInt32();
                        this.propertyValue = new List<double>();

                        for (int i = 0; i < size; i++)
                        {
                            ((List<double>)propertyValue).Add(br.ReadDouble());
                        }
                        break;
                }
            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        bw.Write((double)propertyValue);

                        break;

                    case PropertyDimensions.IsVector:
                        var size = ((List<double>)propertyValue).Count;
                        this.propertyValue = new List<double>();

                        foreach (var i in ((List<double>)propertyValue))
                        {
                            bw.Write(i);
                        }

                        break;
                }
            }
        }
    }
}
