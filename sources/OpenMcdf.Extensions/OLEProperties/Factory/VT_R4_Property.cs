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
            public VT_R4_Property(VTPropertyType vType) : base(vType)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                this.propertyValue = br.ReadSingle();
            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                bw.Write((Single)propertyValue);
            }
        }
    }
}
