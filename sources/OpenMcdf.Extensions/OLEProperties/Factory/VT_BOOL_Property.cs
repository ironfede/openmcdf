using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_BOOL_Property : TypedPropertyValue
        {
            public VT_BOOL_Property(VTPropertyType vType) : base(vType)
            {

            }

            public override void Read(BinaryReader br)
            {
                this.propertyValue = br.ReadUInt16() == (ushort)0xFFFF ? true : false;
                //br.ReadUInt16();//padding
            }
        }
    }
}
