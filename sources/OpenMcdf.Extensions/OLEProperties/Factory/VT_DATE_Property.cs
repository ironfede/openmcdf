using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_DATE_Property : TypedPropertyValue
        {
            public VT_DATE_Property(VTPropertyType vType) : base(vType)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                Double temp = br.ReadDouble();

                this.propertyValue = DateTime.FromOADate(temp);
            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                bw.Write(((DateTime)propertyValue).ToOADate());
            }
        }
    }
}
