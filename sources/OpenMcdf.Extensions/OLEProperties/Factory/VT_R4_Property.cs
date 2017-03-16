using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_R4_Property : TypedPropertyValue
        {
            public VT_R4_Property(VTPropertyType vType, PropertyContext ctx, PropertyDimensions dim) : base(vType, ctx, dim)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        this.propertyValue = br.ReadSingle();
                        break;

                    case PropertyDimensions.IsVector:
                        uint size = br.ReadUInt32();
                        this.propertyValue = new List<float>();

                        for (int i = 0; i < size; i++)
                        {
                            ((List<float>)propertyValue).Add(br.ReadSingle());
                        }
                        break;
                }
            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        bw.Write((float)propertyValue);

                        break;

                    case PropertyDimensions.IsVector:
                        var size = ((List<float>)propertyValue).Count;
                        this.propertyValue = new List<float>();

                        foreach (var i in ((List<float>)propertyValue))
                        {
                            bw.Write(i);
                        }

                        break;
                }
            }
        }
    }
}
