using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_DECIMAL_Property : TypedPropertyValue
        {

            public VT_DECIMAL_Property(VTPropertyType vType) : base(vType)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                Decimal d;

                br.ReadInt16(); // wReserved
                byte scale = br.ReadByte();
                byte sign = br.ReadByte();

                uint u = br.ReadUInt32();
                d = Convert.ToDecimal(Math.Pow(2, 64)) * u;
                d += br.ReadUInt64();

                if (sign != 0)
                    d = -d;
                d /= (10 << scale);

                this.propertyValue = d;
            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                bw.Write((short)propertyValue);
            }
        }
    }
}
