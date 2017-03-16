using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_DECIMAL_Property : TypedPropertyValue
        {

            public VT_DECIMAL_Property(VTPropertyType vType, PropertyContext ctx, PropertyDimensions dim) : base(vType, ctx, dim)
            {

            }

            private Decimal ReadDecimal(BinaryReader br)
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

                return d;
            }

            private void WriteDecimal(Decimal d, BinaryWriter bw)
            {
                int[] parts = Decimal.GetBits(d);
                bool sign = (parts[3] & 0x80000000) != 0;
                byte scale = (byte)((parts[3] >> 16) & 0x7F);

                bw.Write((short)(0x0000));
                bw.Write((byte)(sign ? 0 : 0x80));
                bw.Write(scale);
                bw.Write(parts[0]);
                bw.Write(parts[1]);
                bw.Write(parts[2]);

                //Decimal newValue = new Decimal(parts[0], parts[1], parts[2], sign, scale);
            }

            public override void Read(System.IO.BinaryReader br)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        propertyValue = ReadDecimal(br);
                        break;
                    case PropertyDimensions.IsVector:
                        uint size = br.ReadUInt32();
                        var t = new List<Decimal>();

                        for (int i = 0; i < size; i++)
                        {
                            t.Add(ReadDecimal(br));
                        }
                        break;
                }

            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        WriteDecimal((Decimal)propertyValue, bw);
                        break;
                    case PropertyDimensions.IsVector:
                        List<Decimal> ld = (List<Decimal>)propertyValue;

                        for (int i = 0; i < ld.Count; i++)
                        {
                            WriteDecimal(ld[i], bw);
                        }
                        break;
                }
            }
        }
    }
}
