using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        public ITypedPropertyValue NewProperty(VTPropertyType vType, PropertyContext ctx = null)
        {
            bool isVector = ((0x1000 & (ushort)vType) == 1);
            bool isArray = ((0x2000 & (ushort)vType) == 1);
            bool isVariant = (((ushort)vType & 0x00FF) == 0x000C);

            vType = (VTPropertyType)((ushort)vType & 0x00FF);

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
                    pr = new VT_LPSTR_Property(vType, ctx.CodePage, isVector);
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
                case VTPropertyType.VT_VARIANT_VECTOR_HEADER:
                    pr = new VT_VARIANT_VECTOR_HEADER_Property(vType, ctx);
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
