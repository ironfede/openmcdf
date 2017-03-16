using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_I4_Property : TypedPropertyValue
        {
            public VT_I4_Property(VTPropertyType vType, PropertyContext ctx, PropertyDimensions dim) : base(vType, ctx, dim)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        this.propertyValue = br.ReadInt32();
                        break;

                    case PropertyDimensions.IsVector:
                        uint size = br.ReadUInt32();
                        this.propertyValue = new List<int>();

                        for (int i = 0; i < size; i++)
                        {
                            ((List<int>)propertyValue).Add(br.ReadInt32());
                        }
                        break;
                }
            }

            public override void Write(System.IO.BinaryWriter bw)
            {

                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        bw.Write((int)propertyValue);

                        break;

                    case PropertyDimensions.IsVector:
                        var size = ((List<int>)propertyValue).Count;
                        this.propertyValue = new List<int>();

                        foreach (var i in ((List<int>)propertyValue))
                        {
                            bw.Write(i);
                        }

                        break;
                }

            }
        }
    }
}
