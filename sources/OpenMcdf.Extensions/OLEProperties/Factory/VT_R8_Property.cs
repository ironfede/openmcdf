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
            public VT_R8_Property(VTPropertyType vType) : base(vType)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                this.propertyValue = br.ReadDouble();
            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                bw.Write((Double)propertyValue);
            }
        }
    }
}
