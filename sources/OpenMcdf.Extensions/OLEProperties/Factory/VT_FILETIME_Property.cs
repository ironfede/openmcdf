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

            public VT_FILETIME_Property(VTPropertyType vType) : base(vType)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                Int64 tmp = br.ReadInt64();
                propertyValue = DateTime.FromFileTime(tmp);
            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                bw.Write(((DateTime)propertyValue).ToFileTime());
            }
        }

    }
}
