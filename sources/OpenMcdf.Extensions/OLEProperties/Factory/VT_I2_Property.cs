using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_I2_Property : TypedPropertyValue
        {
            public VT_I2_Property(VTPropertyType vType, PropertyContext ctx, PropertyDimensions dim) : base(vType, ctx, dim)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        this.propertyValue = br.ReadInt16();
                        break;

                    case PropertyDimensions.IsVector:
                        uint size = br.ReadUInt32();
                        this.propertyValue = new List<short>();

                        for (int i = 0; i < size; i++)
                        {
                            ((List<short>)propertyValue).Add(br.ReadInt16());
                        }
                        break;
                }

            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        bw.Write((short)propertyValue);
                        bw.Write((short)0x0000);//padding
                        break;

                    case PropertyDimensions.IsVector:
                        var size = ((List<short>)propertyValue).Count;
                        this.propertyValue = new List<short>();

                        foreach (var i in ((List<short>)propertyValue))
                        {
                            bw.Write(i);
                        }

                        if (((List<short>)propertyValue).Count > 0 && ((List<short>)propertyValue).Count % 2 != 0)
                            bw.Write((short)0x0000);
                        break;
                }

            }
        }
    }
}
