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
            public VT_CY_Property(VTPropertyType vType) : base(vType)
            {
            }

            public override void Read(System.IO.BinaryReader br)
            {
                Int64 temp = br.ReadInt64();

                this.propertyValue = (double)(temp /= 10000);
            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                bw.Write((Int64)propertyValue * 10000);
            }
        }
    }
}
