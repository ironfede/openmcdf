using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OpenMcdf.Extensions.OLEProperties
{
    internal class PropertyFactory
    {
        private PropertyContext ctx;

        public PropertyFactory(PropertyContext ctx)
        {
            this.ctx = ctx;
        }

        private PropertyFactory()
        {

        }

        public ITypedPropertyValue NewProperty(VTPropertyType vType, PropertyContext ctx)
        {
            ITypedPropertyValue pr = null;

            switch (vType)
            {
                case VTPropertyType.VT_I2:
                    pr = new VT_I2_Property(vType);
                    break;
                case VTPropertyType.VT_I4:
                    pr = new VT_I4_Property(vType);
                    break;
                case VTPropertyType.VT_R4:
                    pr = new VT_R4_Property(vType);
                    break;
                case VTPropertyType.VT_LPSTR:
                    pr = new VT_LPSTR_Property(vType, ctx.CodePage);
                    break;
                case VTPropertyType.VT_FILETIME:
                    pr = new VT_FILETIME_Property(vType);
                    break;
                case VTPropertyType.VT_DECIMAL:
                    pr = new VT_DECIMAL_Property(vType);
                    break;
                case VTPropertyType.VT_BOOL:
                    pr = new VT_BOOL_Property(vType);
                    break;
                case VTPropertyType.VT_VECTOR_HEADER:
                    pr = new VT_VectorHeader(vType);
                    break;
                case VTPropertyType.VT_EMPTY:
                    pr = new VT_EMPTY_Property(vType);
                    break;
                default:
                    throw new Exception("Unrecognized property type");
            }

            return pr;
        }


        #region Property implementations
        private class VT_EMPTY_Property : TypedPropertyValue
        {
            public VT_EMPTY_Property(VTPropertyType vType) : base(vType)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                this.propertyValue = null;
            }

            public override void Write(System.IO.BinaryWriter bw)
            {

            }
        }

        private class VT_I2_Property : TypedPropertyValue
        {
            public VT_I2_Property(VTPropertyType vType) : base(vType)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                this.propertyValue = br.ReadInt16();
            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                bw.Write((short)propertyValue);
            }
        }

        private class VT_I4_Property : TypedPropertyValue
        {
            public VT_I4_Property(VTPropertyType vType) : base(vType)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                this.propertyValue = br.ReadInt32();
            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                bw.Write((int)propertyValue);
            }
        }

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

        private class VT_LPSTR_Property : TypedPropertyValue
        {
            private uint size = 0;
            private byte[] data;
            private int codePage;

            public VT_LPSTR_Property(VTPropertyType vType, int codePage) : base(vType)
            {
                this.codePage = codePage;

            }

            public override void Read(System.IO.BinaryReader br)
            {
                size = br.ReadUInt32();
                data = br.ReadBytes((int)size);
                this.propertyValue = Encoding.GetEncoding(codePage).GetString(data);
                int m = (int)size % 4;
                br.ReadBytes(m); // padding
            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                data = Encoding.GetEncoding(codePage).GetBytes((String)propertyValue);
                size = (uint)data.Length;
                int m = (int)size % 4;
                bw.Write(data);
                for (int i = 0; i < m; i++)  // padding
                    bw.Write(0);
            }
        }

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

        private class VT_VectorHeader : TypedPropertyValue
        {
            public VT_VectorHeader(VTPropertyType vType) : base(vType)
            {

            }

            public override void Read(BinaryReader br)
            {
                propertyValue = br.ReadUInt32();
            }
        }

        #endregion

    }
}
