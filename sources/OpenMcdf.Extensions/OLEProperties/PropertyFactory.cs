using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System;
using System.Text;
using System.IO;

namespace OpenMcdf.Extensions.OLEProperties
{
    internal abstract class PropertyFactory
    {
        static PropertyFactory()
        {
#if NETSTANDARD2_0_OR_GREATER
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        }

        public ITypedPropertyValue NewProperty(VTPropertyType vType, int codePage, uint propertyIdentifier, bool isVariant = false)
        {
            ITypedPropertyValue pr = null;

            switch ((VTPropertyType)((ushort)vType & 0x00FF))
            {
                case VTPropertyType.VT_I1:
                    pr = new VT_I1_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_I2:
                    pr = new VT_I2_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_I4:
                    pr = new VT_I4_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_R4:
                    pr = new VT_R4_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_R8:
                    pr = new VT_R8_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_CY:
                    pr = new VT_CY_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_DATE:
                    pr = new VT_DATE_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_INT:
                    pr = new VT_INT_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_UINT:
                    pr = new VT_UINT_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_UI1:
                    pr = new VT_UI1_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_UI2:
                    pr = new VT_UI2_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_UI4:
                    pr = new VT_UI4_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_UI8:
                    pr = new VT_UI8_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_BSTR:
                    pr = new VT_LPSTR_Property(vType, codePage, isVariant);
                    break;
                case VTPropertyType.VT_LPSTR:
                    pr = CreateLpstrProperty(vType, codePage, propertyIdentifier, isVariant);
                    break;
                case VTPropertyType.VT_LPWSTR:
                    pr = new VT_LPWSTR_Property(vType, codePage, isVariant);
                    break;
                case VTPropertyType.VT_FILETIME:
                    pr = new VT_FILETIME_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_DECIMAL:
                    pr = new VT_DECIMAL_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_BOOL:
                    pr = new VT_BOOL_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_EMPTY:
                    pr = new VT_EMPTY_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_VARIANT_VECTOR:
                    pr = new VT_VariantVector(vType, codePage, isVariant, this, propertyIdentifier);
                    break;
                case VTPropertyType.VT_CF:
                    pr = new VT_CF_Property(vType, isVariant);
                    break;
                case VTPropertyType.VT_BLOB_OBJECT:
                case VTPropertyType.VT_BLOB:
                    pr = new VT_BLOB_Property(vType, isVariant);
                    break;
                default:
                    throw new Exception("Unrecognized property type");
            }

            return pr;
        }

        protected virtual ITypedPropertyValue CreateLpstrProperty(VTPropertyType vType, int codePage, uint propertyIdentifier, bool isVariant)
        {
            return new VT_LPSTR_Property(vType, codePage, isVariant);
        }

        #region Property implementations

        private class VT_EMPTY_Property : TypedPropertyValue<object>
        {
            public VT_EMPTY_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override object ReadScalarValue(System.IO.BinaryReader br)
            {
                return null;
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, object pValue)
            {
            }
        }
        private class VT_I1_Property : TypedPropertyValue<sbyte>
        {
            public VT_I1_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override sbyte ReadScalarValue(System.IO.BinaryReader br)
            {
                var r = br.ReadSByte();
                return r;
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, sbyte pValue)
            {
                bw.Write(pValue);
            }
        }

        private class VT_UI1_Property : TypedPropertyValue<byte>
        {
            public VT_UI1_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override byte ReadScalarValue(System.IO.BinaryReader br)
            {
                var r = br.ReadByte();
                return r;
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, byte pValue)
            {
                bw.Write(pValue);
            }
        }

        private class VT_UI4_Property : TypedPropertyValue<uint>
        {
            public VT_UI4_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override uint ReadScalarValue(System.IO.BinaryReader br)
            {
                var r = br.ReadUInt32();
                return r;
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, uint pValue)
            {
                bw.Write(pValue);
            }
        }

        private class VT_UI8_Property : TypedPropertyValue<ulong>
        {
            public VT_UI8_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override ulong ReadScalarValue(System.IO.BinaryReader br)
            {
                var r = br.ReadUInt64();
                return r;
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, ulong pValue)
            {
                bw.Write(pValue);
            }
        }

        private class VT_I2_Property : TypedPropertyValue<short>
        {
            public VT_I2_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override short ReadScalarValue(System.IO.BinaryReader br)
            {
                var r = br.ReadInt16();
                return r;
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, short pValue)
            {
                bw.Write(pValue);
            }
        }



        private class VT_UI2_Property : TypedPropertyValue<ushort>
        {
            public VT_UI2_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override ushort ReadScalarValue(System.IO.BinaryReader br)
            {
                var r = br.ReadUInt16();
                return r;
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, ushort pValue)
            {
                bw.Write(pValue);
            }
        }

        private class VT_I4_Property : TypedPropertyValue<int>
        {
            public VT_I4_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {
            }

            public override int ReadScalarValue(System.IO.BinaryReader br)
            {
                var r = br.ReadInt32();
                return r;
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, int pValue)
            {
                bw.Write(pValue);
            }
        }

        private class VT_I8_Property : TypedPropertyValue<long>
        {
            public VT_I8_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {
            }

            public override long ReadScalarValue(System.IO.BinaryReader br)
            {
                var r = br.ReadInt64();
                return r;
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, long pValue)
            {
                bw.Write(pValue);
            }
        }

        private class VT_INT_Property : TypedPropertyValue<int>
        {
            public VT_INT_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {
            }

            public override int ReadScalarValue(System.IO.BinaryReader br)
            {
                var r = br.ReadInt32();
                return r;
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, int pValue)
            {
                bw.Write(pValue);
            }
        }

        private class VT_UINT_Property : TypedPropertyValue<uint>
        {
            public VT_UINT_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {
            }

            public override uint ReadScalarValue(System.IO.BinaryReader br)
            {
                var r = br.ReadUInt32();
                return r;
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, uint pValue)
            {
                bw.Write(pValue);
            }
        }


        private class VT_R4_Property : TypedPropertyValue<float>
        {
            public VT_R4_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override float ReadScalarValue(System.IO.BinaryReader br)
            {
                var r = br.ReadSingle();
                return r;
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, float pValue)
            {
                bw.Write(pValue);
            }
        }

        private class VT_R8_Property : TypedPropertyValue<double>
        {
            public VT_R8_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override double ReadScalarValue(System.IO.BinaryReader br)
            {
                var r = br.ReadDouble();
                return r;
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, double pValue)
            {
                bw.Write(pValue);
            }
        }

        private class VT_CY_Property : TypedPropertyValue<long>
        {
            public VT_CY_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {
            }

            public override long ReadScalarValue(System.IO.BinaryReader br)
            {
                Int64 temp = br.ReadInt64();

                var tmp = (temp /= 10000);

                return (tmp);
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, long pValue)
            {
                bw.Write(pValue * 10000);
            }
        }

        private class VT_DATE_Property : TypedPropertyValue<DateTime>
        {
            public VT_DATE_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override DateTime ReadScalarValue(System.IO.BinaryReader br)
            {
                Double temp = br.ReadDouble();

                return DateTime.FromOADate(temp);
            }

            public override void WriteScalarValue(System.IO.BinaryWriter bw, DateTime pValue)
            {
                bw.Write(pValue.ToOADate());
            }
        }

        protected class VT_LPSTR_Property : TypedPropertyValue<string>
        {

            private byte[] data;
            private int codePage;

            public VT_LPSTR_Property(VTPropertyType vType, int codePage, bool isVariant) : base(vType, isVariant)
            {
                this.codePage = codePage;
            }

            public override string ReadScalarValue(System.IO.BinaryReader br)
            {
                uint size = br.ReadUInt32();
                data = br.ReadBytes((int)size);

                return Encoding.GetEncoding(codePage).GetString(data);
            }

            public override void WriteScalarValue(BinaryWriter bw, string pValue)
            {
                data = Encoding.GetEncoding(codePage).GetBytes(pValue);
                uint dataLength = (uint)data.Length;

                // The string data must be null terminated, so add a null byte if there isn't one already
                bool addNullTerminator =
                    dataLength == 0 || data[dataLength - 1] != '\0';

                if (addNullTerminator) 
                    dataLength += 1;

                bw.Write(dataLength);
                bw.Write(data);

                if (addNullTerminator)
                    bw.Write((byte)0);
            }
        }

        protected class VT_Unaligned_LPSTR_Property : VT_LPSTR_Property
        {
            public VT_Unaligned_LPSTR_Property(VTPropertyType vType, int codePage, bool isVariant) : base(vType, codePage, isVariant)
            {
                this.NeedsPadding = false;
            }
        }

        private class VT_LPWSTR_Property : TypedPropertyValue<string>
        {

            private byte[] data;
            private int codePage;

            public VT_LPWSTR_Property(VTPropertyType vType, int codePage, bool isVariant) : base(vType, isVariant)
            {
                this.codePage = codePage;
            }

            public override string ReadScalarValue(System.IO.BinaryReader br)
            {
                uint nChars = br.ReadUInt32();
                data = br.ReadBytes((int)(nChars * 2));  //WChar
                return Encoding.Unicode.GetString(data);
            }

            public override void WriteScalarValue(BinaryWriter bw, string pValue)
            {
                data = Encoding.Unicode.GetBytes(pValue);

                // The written data length field is the number of characters (not bytes) and must include a null terminator
                // add a null terminator if there isn't one already
                var byteLength = data.Length;
                bool addNullTerminator =
                    byteLength == 0 || data[byteLength - 1] != '\0' || data[byteLength - 2] != '\0';

                if (addNullTerminator)
                    byteLength += 2;

                bw.Write((uint)byteLength / 2);
                bw.Write(data);

                if (addNullTerminator)
                {
                    bw.Write((byte)0);
                    bw.Write((byte)0);
                }
            }
        }

        private class VT_FILETIME_Property : TypedPropertyValue<DateTime>
        {

            public VT_FILETIME_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override DateTime ReadScalarValue(System.IO.BinaryReader br)
            {
                Int64 tmp = br.ReadInt64();

                return DateTime.FromFileTime(tmp);
            }

            public override void WriteScalarValue(BinaryWriter bw, DateTime pValue)
            {
                bw.Write((pValue).ToFileTime());

            }
        }

        private class VT_DECIMAL_Property : TypedPropertyValue<Decimal>
        {

            public VT_DECIMAL_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override Decimal ReadScalarValue(System.IO.BinaryReader br)
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
                return d;
            }

            public override void WriteScalarValue(BinaryWriter bw, Decimal pValue)
            {
                int[] parts = Decimal.GetBits((Decimal)pValue);

                bool sign = (parts[3] & 0x80000000) != 0;
                byte scale = (byte)((parts[3] >> 16) & 0x7F);


                bw.Write((short)0);
                bw.Write(scale);
                bw.Write(sign ? (byte)0 : (byte)1);

                bw.Write(parts[2]);
                bw.Write(parts[1]);
                bw.Write(parts[0]);
            }
        }

        private class VT_BOOL_Property : TypedPropertyValue<bool>
        {
            public VT_BOOL_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override bool ReadScalarValue(System.IO.BinaryReader br)
            {

                this.propertyValue = br.ReadUInt16() == (ushort)0xFFFF ? true : false;
                return (bool)propertyValue;
                //br.ReadUInt16();//padding
            }

            public override void WriteScalarValue(BinaryWriter bw, bool pValue)
            {
                bw.Write((bool)pValue ? (ushort)0xFFFF : (ushort)0);

            }

        }

        private class VT_CF_Property : TypedPropertyValue<object>
        {
            public VT_CF_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override object ReadScalarValue(System.IO.BinaryReader br)
            {

                int size = br.ReadInt32();
                byte[] data = br.ReadBytes(size);
                return data;
                //br.ReadUInt16();//padding
            }

            public override void WriteScalarValue(BinaryWriter bw, object pValue)
            {
                byte[] r = pValue as byte[];
                if (r != null)
                    bw.Write(r);
            }

        }

        private class VT_BLOB_Property : TypedPropertyValue<object>
        {
            public VT_BLOB_Property(VTPropertyType vType, bool isVariant) : base(vType, isVariant)
            {

            }

            public override object ReadScalarValue(System.IO.BinaryReader br)
            {
                int size = br.ReadInt32();
                byte[] data = br.ReadBytes(size);
                return data;
            }

            public override void WriteScalarValue(BinaryWriter bw, object pValue)
            {
                byte[] r = pValue as byte[];
                if (r != null)
                    bw.Write(r);

            }

        }

        private class VT_VariantVector : TypedPropertyValue<object>
        {
            private readonly int codePage;
            private readonly PropertyFactory factory;
            private readonly uint propertyIdentifier;

            public VT_VariantVector(VTPropertyType vType, int codePage, bool isVariant, PropertyFactory factory, uint propertyIdentifier) : base(vType, isVariant)
            {
                this.codePage = codePage;
                this.factory = factory;
                this.propertyIdentifier = propertyIdentifier;
                this.NeedsPadding = false;
            }

            public override object ReadScalarValue(System.IO.BinaryReader br)
            {
                VTPropertyType vType = (VTPropertyType)br.ReadUInt16();
                br.ReadUInt16(); // Ushort Padding

                ITypedPropertyValue p = factory.NewProperty(vType, codePage, propertyIdentifier, true);
                p.Read(br);
                return p;
            }

            public override void WriteScalarValue(BinaryWriter bw, object pValue)
            {
                ITypedPropertyValue p = (ITypedPropertyValue)pValue;

                p.Write(bw);
            }
        }

#endregion

    }

    // The default property factory.
    internal sealed class DefaultPropertyFactory : PropertyFactory 
    {
        public static PropertyFactory Instance { get; } = new DefaultPropertyFactory();
    }

    // A separate factory for DocumentSummaryInformation properties, to handle special cases with unaligned strings.
    internal sealed class DocumentSummaryInfoPropertyFactory : PropertyFactory
    {
        public static PropertyFactory Instance { get; } = new DocumentSummaryInfoPropertyFactory();

        protected override ITypedPropertyValue CreateLpstrProperty(VTPropertyType vType, int codePage, uint propertyIdentifier, bool isVariant)
        {
            // PIDDSI_HEADINGPAIR and PIDDSI_DOCPARTS use unaligned (unpadded) strings - the others are padded
            if (propertyIdentifier == 0x0000000C || propertyIdentifier == 0x0000000D)
            {
                return new VT_Unaligned_LPSTR_Property(vType, codePage, isVariant);
            }

            return base.CreateLpstrProperty(vType, codePage, propertyIdentifier, isVariant);
        }
    }
}
